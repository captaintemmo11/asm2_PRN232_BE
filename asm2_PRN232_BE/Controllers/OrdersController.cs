using asm2_PRN232_BE.Data;
using asm2_PRN232_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace asm2_PRN232_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy lịch sử orders của user (Sau khi checkout thành công, API này sẽ trả về)
        // API Endpoint: GET /api/orders
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            int userId = GetCurrentUserId();

            var orders = await _context.Orders
                .Include(o => o.Products)  // Include OrderItems
                .ThenInclude(oi => oi.Product)  // Include Product trong OrderItem
                .Where(o => o.UserId == userId)
                // ĐÃ SỬA: Sắp xếp theo OrderDate giảm dần (mới nhất trước)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // Xử lý logic checkout và ghi lại đơn hàng
        // API Endpoint: POST /api/orders
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                int userId = GetCurrentUserId();

                // 1️⃣ Lấy tất cả sản phẩm trong giỏ hàng
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync();

                if (cartItems == null || !cartItems.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng của bạn đang trống." });
                }

                // 2️⃣ Tính tổng tiền
                decimal totalAmount = cartItems.Sum(ci => ci.Quantity * ci.Product.Price);

                // 3️⃣ Tạo đối tượng Order mới
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = "pending",
                    // OrderDate được thiết lập tự động trong Order model
                    Products = cartItems.Select(ci => new OrderItem
                    {
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity,
                        Price = ci.Product.Price
                    }).ToList()
                };

                // 4️⃣ Lưu order và xóa giỏ hàng
                await _context.Orders.AddAsync(order);
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                // 5️⃣ Trả về kết quả gọn gàng
                var response = new
                {
                    message = "Đặt hàng thành công!",
                    order = new
                    {
                        id = order.Id,
                        totalAmount = order.TotalAmount,
                        status = order.Status,
                        items = order.Products.Select(p => new
                        {
                            productId = p.ProductId,
                            quantity = p.Quantity,
                            price = p.Price
                        })
                    }
                };

                return Ok(response);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu khi lưu đơn hàng", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi không xác định", error = ex.Message });
            }
        }


        // Lấy chi tiết một order (optional, để xem detail)
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            int userId = GetCurrentUserId();

            var order = await _context.Orders
                .Include(o => o.Products)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }

        // Helper để lấy userId từ token
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}