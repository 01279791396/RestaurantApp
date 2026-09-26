using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class MenuItemSizeController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MenuItemSizeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int menuItemId)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Sizes)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);
            if (menuItem == null) return NotFound();

            ViewBag.MenuItem = menuItem;
            return View(menuItem.Sizes.OrderBy(s => s.DisplayOrder).ToList());
        }

        public async Task<IActionResult> Create(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null) return NotFound();

            return View(new MenuItemSizeFormViewModel
            {
                MenuItemId = menuItemId,
                MenuItemName = menuItem.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemSizeFormViewModel model)
        {
            if (await NameIsDuplicate(model.MenuItemId, model.Name, excludeId: null))
            {
                ModelState.AddModelError(nameof(model.Name), "فيه مقاس بنفس الاسم للصنف ده بالفعل");
            }
            ValidateDiscounts(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.MenuItemSizes.Add(new MenuItemSize
            {
                MenuItemId = model.MenuItemId,
                Name = model.Name.Trim(),
                Price = model.Price,
                DiscountedPrice = model.DiscountedPrice,
                VipPrice = model.VipPrice,
                DisplayOrder = model.DisplayOrder
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة المقاس بنجاح";
            return RedirectToAction(nameof(Index), new { menuItemId = model.MenuItemId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var size = await _context.MenuItemSizes
                .Include(s => s.MenuItem)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (size == null) return NotFound();

            return View(new MenuItemSizeFormViewModel
            {
                Id = size.Id,
                MenuItemId = size.MenuItemId,
                MenuItemName = size.MenuItem?.Name ?? string.Empty,
                Name = size.Name,
                Price = size.Price,
                DiscountedPrice = size.DiscountedPrice,
                VipPrice = size.VipPrice,
                DisplayOrder = size.DisplayOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemSizeFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (await NameIsDuplicate(model.MenuItemId, model.Name, excludeId: id))
            {
                ModelState.AddModelError(nameof(model.Name), "فيه مقاس بنفس الاسم للصنف ده بالفعل");
            }
            ValidateDiscounts(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var size = await _context.MenuItemSizes.FindAsync(id);
            if (size == null) return NotFound();

            size.Name = model.Name.Trim();
            size.Price = model.Price;
            size.DiscountedPrice = model.DiscountedPrice;
            size.VipPrice = model.VipPrice;
            size.DisplayOrder = model.DisplayOrder;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل المقاس بنجاح";
            return RedirectToAction(nameof(Index), new { menuItemId = size.MenuItemId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var size = await _context.MenuItemSizes
                .Include(s => s.MenuItem)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (size == null) return NotFound();

            return View(size);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var size = await _context.MenuItemSizes.FindAsync(id);
            if (size == null) return NotFound();

            var menuItemId = size.MenuItemId;

            // Safe to hard-delete unconditionally: past orders keep SizeNameAtOrderTime as a
            // plain text snapshot, never a foreign key to this row (see OrderItem).
            _context.MenuItemSizes.Remove(size);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المقاس";
            return RedirectToAction(nameof(Index), new { menuItemId });
        }

        private async Task<bool> NameIsDuplicate(int menuItemId, string name, int? excludeId)
        {
            var normalized = name.Trim();
            return await _context.MenuItemSizes.AnyAsync(s =>
                s.MenuItemId == menuItemId && s.Name == normalized && (excludeId == null || s.Id != excludeId));
        }

        private void ValidateDiscounts(MenuItemSizeFormViewModel model)
        {
            if (model.DiscountedPrice.HasValue && model.DiscountedPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.DiscountedPrice), "سعر الخصم لازم يكون أقل من السعر الأساسي");
            }
            if (model.VipPrice.HasValue && model.VipPrice.Value >= model.Price)
            {
                ModelState.AddModelError(nameof(model.VipPrice), "سعر العميل المميز لازم يكون أقل من السعر الأساسي");
            }
        }
    }
}
