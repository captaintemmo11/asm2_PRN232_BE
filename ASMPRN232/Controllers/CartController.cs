using ASMPRN232.Data;
using ASMPRN232.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ASMPRN232.DTOs
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng của user (bao gồm items, quantity, total)
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<object>> GetCart()
        {
            int userId = GetCurrentUserId();

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)  // Include Product để tính price
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            decimal total = cartItems.Sum(ci => ci.Quantity * ci.Product.Price);

            return Ok(new
            {
                Items = cartItems.Select(ci => new
                {
                    ci.Id,
                    Product = new { ci.Product.Id, ci.Product.Name, ci.Product.Price, ci.Product.Image },
                    ci.Quantity
                }),
                Total = total
            });
        }

        // Thêm sản phẩm vào giỏ
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CartItem>> AddToCart([FromBody] AddToCartDto dto)
        {
            int userId = GetCurrentUserId();

            // Kiểm tra sản phẩm tồn tại
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Kiểm tra item đã tồn tại trong giỏ chưa
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                // Update quantity nếu đã có
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                // Thêm mới
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Item added to cart" });
        }

        // Cập nhật quantity của item
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateQuantityDto dto)
        {
            int userId = GetCurrentUserId();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            if (dto.Quantity <= 0)
            {
                // Nếu quantity <=0, xóa item
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = dto.Quantity;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item updated" });
        }

        // Xóa item khỏi giỏ
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveItem(int id)
        {
            int userId = GetCurrentUserId();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item removed" });
        }

        // Helper để lấy userId từ token
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}