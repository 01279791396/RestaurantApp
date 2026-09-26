using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.ViewModels;

namespace RestaurantApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Rate(int menuItemId)
        {
            var userId = _userManager.GetUserId(User)!;

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null) return NotFound();

            if (!await HasReceivedItemAsync(userId, menuItemId))
            {
                TempData["Error"] = "تقدر تقيّم بس الأصناف اللي طلبتها واستلمتها";
                return RedirectToAction("MyOrders", "Order");
            }

            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MenuItemId == menuItemId && r.CustomerId == userId);

            return View(new RateItemViewModel
            {
                MenuItemId = menuItem.Id,
                MenuItemName = menuItem.Name,
                Rating = existing?.Rating ?? 5,
                Comment = existing?.Comment
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(RateItemViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;

            var menuItem = await _context.MenuItems.FindAsync(model.MenuItemId);
            if (menuItem == null) return NotFound();
            model.MenuItemName = menuItem.Name;

            if (!await HasReceivedItemAsync(userId, model.MenuItemId))
            {
                TempData["Error"] = "تقدر تقيّم بس الأصناف اللي طلبتها واستلمتها";
                return RedirectToAction("MyOrders", "Order");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MenuItemId == model.MenuItemId && r.CustomerId == userId);

            if (existing != null)
            {
                existing.Rating = model.Rating;
                existing.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Reviews.Add(new Review
                {
                    MenuItemId = model.MenuItemId,
                    CustomerId = userId,
                    Rating = model.Rating,
                    Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim()
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"شكرًا لتقييمك لـ {menuItem.Name}!";
            return RedirectToAction("MyOrders", "Order");
        }

        // Verified-purchase check: the customer must have at least one Delivered order that
        // included this item — prevents drive-by ratings from people who never actually ordered.
        private async Task<bool> HasReceivedItemAsync(string customerId, int menuItemId)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.MenuItemId == menuItemId
                    && oi.Order!.CustomerId == customerId
                    && oi.Order.Status == OrderStatus.Delivered);
        }
    }
}
