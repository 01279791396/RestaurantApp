using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantApp.Areas.Admin.Models
{
    public class MenuItemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [StringLength(150)]
        [Display(Name = "اسم الصنف")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, 100000, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }

        [Range(0.01, 100000, ErrorMessage = "سعر الخصم لازم يكون أكبر من صفر")]
        [Display(Name = "سعر مخفض (عرض) — اختياري")]
        public decimal? DiscountedPrice { get; set; }

        [Range(0.01, 100000, ErrorMessage = "سعر العميل المميز لازم يكون أكبر من صفر")]
        [Display(Name = "سعر العميل المميز — اختياري")]
        public decimal? VipPrice { get; set; }

        [Required(ErrorMessage = "اختار القسم")]
        [Display(Name = "القسم")]
        public int CategoryId { get; set; }

        [Display(Name = "متاح للطلب")]
        public bool IsAvailable { get; set; } = true;

        // Display-only (not bound from the form) — tells the Edit view whether to show a note
        // that Price/DiscountedPrice/VipPrice above are ignored because sizes take over pricing.
        public bool HasSizes { get; set; }

        [Display(Name = "صورة الصنف")]
        public IFormFile? Image { get; set; }

        // Existing image path, shown as a preview when editing; untouched if no new Image is uploaded.
        public string? ExistingImagePath { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new();
    }
}
