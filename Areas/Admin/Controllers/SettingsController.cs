using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class SettingsController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await GetSettingsAsync();
            return View(new RestaurantSettingsViewModel
            {
                OpenTime = settings.OpenTime,
                CloseTime = settings.CloseTime,
                IsTemporarilyClosed = settings.IsTemporarilyClosed
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RestaurantSettingsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var settings = await GetSettingsAsync();
            settings.OpenTime = model.OpenTime;
            settings.CloseTime = model.CloseTime;
            settings.IsTemporarilyClosed = model.IsTemporarilyClosed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ مواعيد الشغل";
            return RedirectToAction(nameof(Index));
        }

        // The settings row is seeded once by DbInitializer, but FirstAsync guards against a
        // database that was created before this feature existed and never got that seed.
        private async Task<RestaurantSettings> GetSettingsAsync()
        {
            var settings = await _context.RestaurantSettings.FirstOrDefaultAsync();
            if (settings != null) return settings;

            settings = new RestaurantSettings();
            _context.RestaurantSettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }
    }
}
