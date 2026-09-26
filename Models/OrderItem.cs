using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApp.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem? MenuItem { get; set; }

        // Snapshot of the item's name at order time, so the order history still reads correctly
        // even if the admin later renames or deletes the menu item.
        [Required, StringLength(150)]
        public string ItemNameAtOrderTime { get; set; } = string.Empty;

        // Snapshot of the chosen size's name (e.g. "كبير"), if the item had sizes. Null for an
        // item ordered without sizes. Not a FK to MenuItemSize — sizes can be edited/removed
        // by the admin later without touching past order history, same reasoning as the name.
        [StringLength(50)]
        public string? SizeNameAtOrderTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPriceAtOrderTime { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [NotMapped]
        public decimal LineTotal => UnitPriceAtOrderTime * Quantity;
    }
}
