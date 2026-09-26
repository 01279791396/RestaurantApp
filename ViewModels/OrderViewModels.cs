using System.ComponentModel.DataAnnotations;
using RestaurantApp.Models;

namespace RestaurantApp.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "عنوان التوصيل مطلوب")]
        [StringLength(300)]
        [Display(Name = "عنوان التوصيل")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الموبايل مطلوب")]
        [Phone(ErrorMessage = "رقم الموبايل غير صحيح")]
        [Display(Name = "رقم الموبايل")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "ملاحظات (اختياري)")]
        public string? Notes { get; set; }

        [StringLength(30)]
        [Display(Name = "كود الخصم (اختياري)")]
        public string? CouponCode { get; set; }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal => CartItems.Sum(i => i.LineTotal);
        public decimal Total => Subtotal;
    }

    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;
    }

    public class RateItemViewModel
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "اختار تقييم من 1 لـ 5")]
        [Display(Name = "التقييم")]
        public int Rating { get; set; } = 5;

        [StringLength(500)]
        [Display(Name = "تعليق (اختياري)")]
        public string? Comment { get; set; }
    }
}
