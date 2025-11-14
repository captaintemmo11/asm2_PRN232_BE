namespace ASMPRN232.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public List<OrderItem> Products { get; set; } 
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public Order()
        {
            OrderDate = DateTime.UtcNow;
        }
    }
}
