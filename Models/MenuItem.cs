using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApp.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 100000)]
        public decimal Price { get; set; }

        // Sale price shown/charged to every customer when set (e.g. "عرض خاص"). Must be lower
        // than Price — enforced in Admin/MenuItemController, not here, so validation messages
        // can be in Arabic and compare against the submitted Price in the same request.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountedPrice { get; set; }

        // Special price for VIP ("عميل مميز") customers only — takes priority over
        // DiscountedPrice when the buyer is VIP. Null = this item has no VIP price.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? VipPrice { get; set; }

        // Relative path under wwwroot/images/menu, e.g. "koshary.jpg". Null = placeholder image.
        public string? ImagePath { get; set; }

        // Lets the admin temporarily hide an item (out of stock) without deleting it,
        // which would also delete its order history via FK.
        public bool IsAvailable { get; set; } = true;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // When an item has sizes (e.g. كبير/وسط/صغير, or L/M), the customer picks one of
        // these instead of the base Price/DiscountedPrice/VipPrice above, which are then
        // ignored for pricing purposes but stay set (harmless) rather than becoming nullable —
        // keeps every other query/validation that reads MenuItem.Price unchanged.
        public ICollection<MenuItemSize> Sizes { get; set; } = new List<MenuItemSize>();

        [NotMapped]
        public bool HasSizes => Sizes.Count > 0;

        // The price a given customer actually pays: VIP price (if they're VIP and one is set),
        // else the sale price (if set), else the regular price. Used both when adding to the
        // cart and when re-validating prices server-side at checkout, so the two always agree.
        // Only meaningful for an item with no sizes — see MenuItemSize.EffectivePriceFor for
        // sized items.
        public decimal EffectivePriceFor(bool isVip)
        {
            if (isVip && VipPrice.HasValue) return VipPrice.Value;
            return DiscountedPrice ?? Price;
        }
    }
}
