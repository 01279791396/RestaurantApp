using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApp.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        // Always stored/compared uppercase (see Admin/CouponController) so entry is
        // case-insensitive for the customer without needing a case-insensitive DB collation.
        [Required, StringLength(30)]
        public string Code { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        // Percentage (0-100) or a flat ج.م amount, depending on DiscountType.
        [Column(TypeName = "decimal(10,2)")]
        public decimal Value { get; set; }

        // Order subtotal (items only, before delivery fee) must reach this for the coupon
        // to apply. Null = no minimum.
        [Column(TypeName = "decimal(10,2)")]
        public decimal? MinOrderAmount { get; set; }

        // Null = never expires.
        public DateTime? ExpiresAt { get; set; }

        // Lets the admin turn a coupon off without deleting it (deleting would break the
        // record on any past order that used it).
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsValidNow => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value >= DateTime.UtcNow);

        // Discount amount for a given items-subtotal, respecting MinOrderAmount and never
        // exceeding the subtotal itself (a coupon can't make an order go negative).
        public decimal CalculateDiscount(decimal subtotal)
        {
            if (!IsValidNow) return 0;
            if (MinOrderAmount.HasValue && subtotal < MinOrderAmount.Value) return 0;

            var discount = DiscountType == DiscountType.Percentage
                ? subtotal * (Value / 100m)
                : Value;

            return Math.Min(discount, subtotal);
        }
    }
}
