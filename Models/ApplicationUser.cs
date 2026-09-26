using Microsoft.AspNetCore.Identity;

namespace RestaurantApp.Models
{
    // Extends the built-in Identity user. Used for Customers and the Admin.
    // (Delivery is handled by phone call, not an app account — see Order.DeliveryPersonName.)
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Used by Customers as their default delivery address (they can still type a different
        // one at checkout).
        public string? DefaultAddress { get; set; }

        // Admin-controlled flag (see Admin/CustomerController). VIP customers see and are
        // charged MenuItem.VipPrice on items that have one set, instead of the regular price.
        public bool IsVip { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
