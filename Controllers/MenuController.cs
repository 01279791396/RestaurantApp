using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.ViewModels;

namespace RestaurantApp.Controllers
{
    // Public storefront page. No login required — anyone can browse the menu;
    // login is only needed at checkout (enforced in OrderController later).
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MenuController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Only show categories that currently have at least one available item —
            // an empty "مقبلات" section with nothing orderable just confuses customers.
            // Loaded via Include (not a Select projection) because we also need each item's
            // Sizes, which doesn't compose cleanly with a nested collection projection.
            var allCategories = await _context.Categories
                .Include(c => c.MenuItems).ThenInclude(m => m.Sizes)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var categories = allCategories
                .Select(c => new MenuCategoryViewModel
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    Items = c.MenuItems
                        .Where(m => m.IsAvailable)
                        .OrderBy(m => m.Name)
                        .ToList()
                })
                .Where(c => c.Items.Count > 0)
                .ToList();

            var isVip = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                isVip = user?.IsVip ?? false;
            }

            var itemIds = categories.SelectMany(c => c.Items).Select(i => i.Id).ToList();
            var ratings = await _context.Reviews
                .Where(r => itemIds.Contains(r.MenuItemId))
                .GroupBy(r => r.MenuItemId)
                .Select(g => new { MenuItemId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
                .ToListAsync();

            var settings = await _context.RestaurantSettings.FirstOrDefaultAsync();

            return View(new MenuIndexViewModel
            {
                Categories = categories,
                IsVip = isVip,
                Ratings = ratings.ToDictionary(r => r.MenuItemId, r => (r.Average, r.Count)),
                IsOpen = settings == null || settings.IsOpenAt(DateTime.Now),
                OpenTime = settings?.OpenTime ?? TimeSpan.Zero,
                CloseTime = settings?.CloseTime ?? TimeSpan.Zero
            });
        }
    }
}
