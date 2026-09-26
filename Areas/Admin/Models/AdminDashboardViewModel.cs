namespace RestaurantApp.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int PendingOrdersCount { get; set; }
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int MenuItemsCount { get; set; }
        public int CategoriesCount { get; set; }
        public int CustomersCount { get; set; }
        public int VipCustomersCount { get; set; }
        public int ActiveCouponsCount { get; set; }
    }
}
