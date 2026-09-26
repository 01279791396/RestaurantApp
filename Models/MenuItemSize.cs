using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApp.Models
{
    // One priced size option for a MenuItem (e.g. "كبير" 170ج / "وسط" 140ج / "صغير" 125ج,
    // or "L" / "M" for pizzas). An item with no sizes just uses its own Price directly
    // (see MenuItem.HasSizes) — sizes are opt-in per item, not required.
    public class MenuItemSize
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem? MenuItem { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 100000, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        public decimal Price { get; set; }

        // Same override rules as MenuItem's own pricing fields, just scoped to this size.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountedPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? VipPrice { get; set; }

        // Controls display order (e.g. كبير then وسط then صغير) — lower shows first.
        public int DisplayOrder { get; set; }

        public decimal EffectivePriceFor(bool isVip)
        {
            if (isVip && VipPrice.HasValue) return VipPrice.Value;
            return DiscountedPrice ?? Price;
        }
    }
}
