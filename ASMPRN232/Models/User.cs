namespace ASMPRN232.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public ICollection<CartItem> CartItems { get; set; } 
        public ICollection<Order> Orders { get; set; }
    }
}
