namespace RestaurantApp.ViewModels
{
    // Stored as JSON in session — deliberately holds a price/name snapshot from when the
    // item was added, so a mid-session menu price change doesn't silently change the cart total.
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }

        // Null for an item with no sizes. When set, this line is that specific size — two
        // different sizes of the same MenuItem are two separate cart lines, never merged.
        public int? SizeId { get; set; }
        public string? SizeName { get; set; }

        public decimal LineTotal => Price * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
