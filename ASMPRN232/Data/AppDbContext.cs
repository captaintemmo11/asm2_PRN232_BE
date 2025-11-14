using ASMPRN232.Models;
using Microsoft.EntityFrameworkCore;

namespace ASMPRN232.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình đã có cho Price
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Cấu hình relationships

            // CartItem - User (Many-to-One: Nhiều CartItem thuộc một User)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)  // Navigation property trong CartItem: public User User { get; set; }
                .WithMany(u => u.CartItems)  // Navigation trong User: public ICollection<CartItem> CartItems { get; set; }
                .HasForeignKey(ci => ci.UserId)  // FK trong CartItem
                .OnDelete(DeleteBehavior.Cascade);  // Nếu xóa User, xóa luôn CartItem

            // CartItem - Product (Many-to-One) - SỬA Ở ĐÂY: Bỏ lambda vì không có navigation ngược
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()  // Bỏ (p => p.CartItems) để tránh lỗi, vì optional
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);  // Không xóa Product nếu xóa CartItem

            // Order - User (Many-to-One)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - OrderItem (One-to-Many: Một Order có nhiều OrderItem)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)  // Nav trong OrderItem: public Order Order { get; set; }
                .WithMany(o => o.Products)  // Nav trong Order: public List<OrderItem> Products { get; set; }
                .HasForeignKey(oi => oi.OrderId)  // Thêm OrderId vào model OrderItem nếu chưa có
                .OnDelete(DeleteBehavior.Cascade);  // Xóa Order thì xóa luôn OrderItem

            // OrderItem - Product (Many-to-One, optional nếu bạn muốn track sản phẩm trong chi tiết đơn)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()  // Không cần nav ngược nếu không dùng
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}