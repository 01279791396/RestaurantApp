using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Models
{
    // A menu section, e.g. "مقبلات", "أطباق رئيسية", "مشروبات".
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Controls display order on the menu page (lower shows first).
        public int DisplayOrder { get; set; }

        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
