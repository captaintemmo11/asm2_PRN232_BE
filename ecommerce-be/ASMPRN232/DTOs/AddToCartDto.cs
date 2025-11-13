namespace ASMPRN232.DTOs
{
    // DTOs cho Cart
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;  // Default 1 nếu không chỉ định
    }
}