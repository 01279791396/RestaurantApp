using System.ComponentModel.DataAnnotations;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Models
{
    public class CouponFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "كود الخصم مطلوب")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "الكود لازم يكون من 3 لـ 30 حرف")]
        [Display(Name = "كود الخصم")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "نوع الخصم")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required(ErrorMessage = "قيمة الخصم مطلوبة")]
        [Range(0.01, 100000, ErrorMessage = "قيمة الخصم لازم تكون أكبر من صفر")]
        [Display(Name = "القيمة")]
        public decimal Value { get; set; }

        [Range(0, 100000)]
        [Display(Name = "الحد الأدنى للأوردر (اختياري)")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "تاريخ الانتهاء (اختياري)")]
        [DataType(DataType.Date)]
        public DateTime? ExpiresAt { get; set; }

        [Display(Name = "فعّال")]
        public bool IsActive { get; set; } = true;
    }
}
