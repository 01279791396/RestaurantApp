using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class HomeController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var todayStart = DateTime.UtcNow.Date;
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            var vm = new AdminDashboardViewModel
            {
                PendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                TodayOrdersCount = await _context.Orders.CountAsync(o => o.CreatedAt >= todayStart),
                TodayRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= todayStart && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0,
                MenuItemsCount = await _context.MenuItems.CountAsync(),
                CategoriesCount = await _context.Categories.CountAsync(),
                CustomersCount = customers.Count,
                VipCustomersCount = customers.Count(c => c.IsVip),
                ActiveCouponsCount = await _context.Coupons.CountAsync(c => c.IsActive)
            };

            return View(vm);
        }
    }
}
