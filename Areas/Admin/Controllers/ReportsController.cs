using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class ReportsController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Defaults to "today" — the most common thing an admin checks — but accepts any
        // from/to range (e.g. a full week) via the date pickers on the page.
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var fromDate = (from ?? DateTime.Today).Date;
            var toDate = (to ?? DateTime.Today).Date;
            // Inclusive of the whole "to" day.
            var toExclusive = toDate.AddDays(1);

            var ordersInRange = _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toExclusive);

            var nonCancelled = ordersInRange.Where(o => o.Status != OrderStatus.Cancelled);

            var vm = new SalesReportViewModel
            {
                From = fromDate,
                To = toDate,
                TotalOrders = await nonCancelled.CountAsync(),
                CancelledOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = await nonCancelled.SumAsync(o => (decimal?)(o.TotalPrice + o.DeliveryFee - o.DiscountAmount)) ?? 0,
                TotalDeliveryFees = await nonCancelled.SumAsync(o => (decimal?)o.DeliveryFee) ?? 0,
                TotalDiscounts = await nonCancelled.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0
            };

            vm.TopItems = await _context.OrderItems
                .Where(oi => oi.Order!.CreatedAt >= fromDate
                    && oi.Order.CreatedAt < toExclusive
                    && oi.Order.Status != OrderStatus.Cancelled)
                .GroupBy(oi => oi.SizeNameAtOrderTime == null
                    ? oi.ItemNameAtOrderTime
                    : oi.ItemNameAtOrderTime + " (" + oi.SizeNameAtOrderTime + ")")
                .Select(g => new BestSellingItemViewModel
                {
                    Name = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPriceAtOrderTime * oi.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToListAsync();

            return View(vm);
        }
    }
}
