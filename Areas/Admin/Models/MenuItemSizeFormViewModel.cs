using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Areas.Admin.Models
{
    public class MenuItemSizeFormViewModel
    {
        public int Id { get; set; }

        public int MenuItemId { get; set; }

        // Shown above the form so the admin sees which item they're editing sizes for,
        // without needing another round trip.
        public string MenuItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المقاس مطلوب")]
        [StringLength(50)]
        [Display(Name = "اسم المقاس")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, 100000, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }

        [Range(0.01, 100000, ErrorMessage = "سعر الخصم لازم يكون أكبر من صفر")]
        [Display(Name = "سعر الخصم (اختياري)")]
        public decimal? DiscountedPrice { get; set; }

        [Range(0.01, 100000, ErrorMessage = "سعر العميل المميز لازم يكون أكبر من صفر")]
        [Display(Name = "سعر العميل المميز (اختياري)")]
        public decimal? VipPrice { get; set; }

        [Display(Name = "ترتيب العرض")]
        [Range(0, 1000)]
        public int DisplayOrder { get; set; }
    }
}
