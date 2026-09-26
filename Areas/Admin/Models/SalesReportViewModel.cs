namespace RestaurantApp.Areas.Admin.Models
{
    public class BestSellingItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int TotalOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDeliveryFees { get; set; }
        public decimal TotalDiscounts { get; set; }

        public List<BestSellingItemViewModel> TopItems { get; set; } = new();
    }
}
