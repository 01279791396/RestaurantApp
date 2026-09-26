using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Areas.Admin.Models
{
    public class RestaurantSettingsViewModel
    {
        [Required(ErrorMessage = "ميعاد الفتح مطلوب")]
        [Display(Name = "ميعاد الفتح")]
        [DataType(DataType.Time)]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "ميعاد القفل مطلوب")]
        [Display(Name = "ميعاد القفل")]
        [DataType(DataType.Time)]
        public TimeSpan CloseTime { get; set; }

        [Display(Name = "المطعم مقفول مؤقتًا (حتى في مواعيد الشغل)")]
        public bool IsTemporarilyClosed { get; set; }
    }
}
