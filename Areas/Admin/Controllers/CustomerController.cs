using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class CustomerController : AdminControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            return View(customers.OrderByDescending(c => c.IsVip).ThenBy(c => c.FullName).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVip(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsVip = !user.IsVip;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsVip
                ? $"{user.FullName} بقى عميل مميز"
                : $"تم إلغاء تميز {user.FullName}";
            return RedirectToAction(nameof(Index));
        }
    }
}
