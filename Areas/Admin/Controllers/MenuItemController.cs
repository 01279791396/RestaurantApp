using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class MenuItemController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/pjpeg"] = ".jpg",
            ["image/jpg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

        private static readonly string[] AllowedFallbackExtensions = { ".jpg", ".jpeg", ".jfif", ".png", ".webp" };

        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        public MenuItemController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Sizes)
                .OrderBy(m => m.Category!.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            return View(new MenuItemFormViewModel { CategoryOptions = await GetCategoryOptionsAsync() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemFormViewModel model)
        {
            string? savedImagePath = null;

            if (model.Image != null)
            {
                var (ok, error, fileName) = await TrySaveImageAsync(model.Image);
                if (!ok)
                {
                    ModelState.AddModelError(nameof(model.Image), error!);
                }
                else
                {
                    savedImagePath = fileName;
                }
            }

            if (model.DiscountedPrice.HasValue && model.DiscountedPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.DiscountedPrice), "السعر المخفض لازم يكون أقل من السعر الأصلي");
            }
            if (model.VipPrice.HasValue && model.VipPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.VipPrice), "سعر العميل المميز لازم يكون أقل من السعر الأصلي");
            }

            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                return View(model);
            }

            _context.MenuItems.Add(new MenuItem
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Price = model.Price,
                DiscountedPrice = model.DiscountedPrice,
                VipPrice = model.VipPrice,
                CategoryId = model.CategoryId,
                IsAvailable = model.IsAvailable,
                ImagePath = savedImagePath
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الصنف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems.Include(m => m.Sizes).FirstOrDefaultAsync(m => m.Id == id);
            if (item == null) return NotFound();

            return View(new MenuItemFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                DiscountedPrice = item.DiscountedPrice,
                VipPrice = item.VipPrice,
                CategoryId = item.CategoryId,
                IsAvailable = item.IsAvailable,
                ExistingImagePath = item.ImagePath,
                HasSizes = item.HasSizes,
                CategoryOptions = await GetCategoryOptionsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            string? newImagePath = null;
            if (model.Image != null)
            {
                var (ok, error, fileName) = await TrySaveImageAsync(model.Image);
                if (!ok)
                {
                    ModelState.AddModelError(nameof(model.Image), error!);
                }
                else
                {
                    newImagePath = fileName;
                }
            }

            if (model.DiscountedPrice.HasValue && model.DiscountedPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.DiscountedPrice), "السعر المخفض لازم يكون أقل من السعر الأصلي");
            }
            if (model.VipPrice.HasValue && model.VipPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.VipPrice), "سعر العميل المميز لازم يكون أقل من السعر الأصلي");
            }

            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                model.ExistingImagePath = item.ImagePath;
                return View(model);
            }

            var oldImagePath = item.ImagePath;

            item.Name = model.Name.Trim();
            item.Description = model.Description?.Trim();
            item.Price = model.Price;
            item.DiscountedPrice = model.DiscountedPrice;
            item.VipPrice = model.VipPrice;
            item.CategoryId = model.CategoryId;
            item.IsAvailable = model.IsAvailable;
            if (newImagePath != null)
            {
                item.ImagePath = newImagePath;
            }

            await _context.SaveChangesAsync();

            // Delete the old file only after the DB update succeeds, and only if we actually
            // replaced it — never delete on a validation failure or an unrelated field edit.
            if (newImagePath != null && !string.IsNullOrEmpty(oldImagePath))
            {
                DeleteImageFile(oldImagePath);
            }

            TempData["Success"] = "تم تعديل الصنف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            item.IsAvailable = !item.IsAvailable;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null) return NotFound();

            ViewBag.HasOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
            if (hasOrders)
            {
                // Preserve order history integrity — hide it instead of deleting.
                TempData["Error"] = "الصنف ده ليه أوردرات سابقة، فمينفعش يتمسح. تقدر تخليه (غير متاح) بدل ما تمسحه.";
                return RedirectToAction(nameof(Index));
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(item.ImagePath))
            {
                DeleteImageFile(item.ImagePath);
            }

            TempData["Success"] = "تم حذف الصنف";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCategoryOptionsAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        private async Task<(bool ok, string? error, string? fileName)> TrySaveImageAsync(IFormFile image)
        {
            string extension;

            if (AllowedContentTypes.TryGetValue(image.ContentType ?? "", out var mappedExtension))
            {
                extension = mappedExtension;
            }
            else
            {
                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedFallbackExtensions.Contains(fileExtension))
                {
                    return (false, "الصورة لازم تكون jpg أو jpeg أو png أو webp", null);
                }
                extension = fileExtension is ".jpeg" or ".jfif" ? ".jpg" : fileExtension;
            }

            if (image.Length > MaxImageBytes)
            {
                return (false, "حجم الصورة أكبر من 5 ميجا", null);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folder = Path.Combine(_env.WebRootPath, "images", "menu");
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return (true, null, fileName);
        }
        private void DeleteImageFile(string fileName)
        {
            try
            {
                var path = Path.Combine(_env.WebRootPath, "images", "menu", fileName);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup — an orphaned file on disk is harmless; failing the
                // whole request over it would not be.
            }
        }
    }
}
