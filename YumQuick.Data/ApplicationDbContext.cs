using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using YumQuick.Core.Entities;

namespace YumQuick.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<CartItemVariant> CartItemVariants { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemVariant> OrderItemVariants { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<CancelReason> CancelReasons { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Composite Key for Favorites (Many-to-Many)
            builder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.ProductId });

            // 2. Self-Referencing Category (Sub-categories)
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Order Relationships
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting orders if user is deleted

            builder.Entity<Order>()
                .HasOne(o => o.Driver)
                .WithMany()
                .HasForeignKey(o => o.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Decimal Precision Configuration (SQL Server Warning Fix)
            var decimals = builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimals)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // 5. Unique Indexes
            builder.Entity<Coupon>()
                .HasIndex(c => c.Code).IsUnique();

            // 6. preventMultiple Cascade Paths
            builder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItemVariant>()
                .HasOne(cv => cv.Variant)
                .WithMany()
                .HasForeignKey(cv => cv.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItemVariant>()
                .HasOne(ov => ov.Variant)
                .WithMany()
                .HasForeignKey(ov => ov.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}