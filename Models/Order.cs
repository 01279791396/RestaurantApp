using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Human-friendly invoice number printed on the receipt, e.g. "INV-20260920-0007".
        // Filled in when the order is created (see OrderController) — never reused, even if
        // an order is later cancelled, so a delivery person's printed slip always maps to
        // exactly one order.
        [Required, StringLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey(nameof(CustomerId))]
        public ApplicationUser? Customer { get; set; }

        // Snapshot of delivery details at order time (not read from the user's profile later,
        // so later profile edits never change a past order's delivery info).
        [Required, StringLength(300)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Notes { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Sum of OrderItems (Quantity * UnitPriceAtOrderTime). Stored, not computed, so the
        // order total stays fixed even if menu prices change afterwards.
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Cash on delivery only for now — no payment gateway integration.
        public bool IsPaid { get; set; } = false;

        // Delivery is handled by phone call, not a delivery-staff account/login — the admin
        // calls someone and jots their name/phone here for the record. Null until the order
        // reaches OutForDelivery.
        [StringLength(100)]
        public string? DeliveryPersonName { get; set; }

        [StringLength(20)]
        public string? DeliveryPersonPhone { get; set; }

        // Set when assigning the delivery person. The delivery person collects
        // (TotalPrice + DeliveryFee - DiscountAmount) in cash from the customer and hands the
        // DeliveryFee portion back to the restaurant.
        [Column(TypeName = "decimal(10,2)")]
        public decimal DeliveryFee { get; set; }

        // Coupon applied at checkout, if any — stored as a snapshot (code + resulting amount)
        // rather than a Coupon FK, so the order stays accurate even if the coupon is edited or
        // deactivated afterwards.
        [StringLength(30)]
        public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        [NotMapped]
        public decimal GrandTotal => TotalPrice + DeliveryFee - DiscountAmount;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
