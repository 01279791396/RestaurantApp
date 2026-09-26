using RestaurantApp.Models;

namespace RestaurantApp.ViewModels
{
    public class MenuCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<MenuItem> Items { get; set; } = new();
    }

    public class MenuIndexViewModel
    {
        public List<MenuCategoryViewModel> Categories { get; set; } = new();
        public bool IsVip { get; set; }
        public Dictionary<int, (double Average, int Count)> Ratings { get; set; } = new();
        public bool IsOpen { get; set; } = true;
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
    }
}
