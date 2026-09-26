using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Areas.Admin.Models
{
    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم القسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "ترتيب العرض")]
        [Range(0, 1000)]
        public int DisplayOrder { get; set; }
    }
}
