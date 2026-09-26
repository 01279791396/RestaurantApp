using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Models;

namespace RestaurantApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Coupon> Coupons { get; set; } = null!;
        public DbSet<RestaurantSettings> RestaurantSettings { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<MenuItemSize> MenuItemSizes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // MenuItem -> Category: restrict delete so an admin can't delete a category
            // that still has items in it (avoids orphaned/cascaded menu data by accident).
            builder.Entity<MenuItem>()
                .HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> Customer: keep order history even if a user is ever removed.
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> DeliveryPerson removed: delivery is arranged by phone call, not a
            // staff account, so the order just stores a name/phone snapshot (see Order.cs).

            // OrderItem -> Order: cascade, an order item makes no sense without its order.
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> MenuItem: restrict, so a menu item with past orders can't be
            // hard-deleted (use IsAvailable = false instead).
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany()
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.InvoiceNumber)
                .IsUnique();

            builder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // Review -> MenuItem / Customer: restrict delete so a review can't be orphaned by
            // deleting the item or the account it belongs to (menu items with history already
            // can't be hard-deleted anyway — see MenuItemController).
            builder.Entity<Review>()
                .HasOne(r => r.MenuItem)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One review per customer per item — a second "rating" for the same item just
            // edits their existing one (see ReviewController).
            builder.Entity<Review>()
                .HasIndex(r => new { r.MenuItemId, r.CustomerId })
                .IsUnique();

            // MenuItemSize -> MenuItem: cascade — a size option is meaningless without its
            // item, and orders never FK to a size (OrderItem stores SizeNameAtOrderTime as a
            // plain snapshot, same pattern as ItemNameAtOrderTime), so cascading here can never
            // touch order history.
            builder.Entity<MenuItemSize>()
                .HasOne(s => s.MenuItem)
                .WithMany(m => m.Sizes)
                .HasForeignKey(s => s.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MenuItemSize>()
                .HasIndex(s => new { s.MenuItemId, s.Name })
                .IsUnique();
        }
    }
}
