using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class CategoryController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View(new CategoryFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (await NameIsDuplicate(model.Name, excludeId: null))
            {
                ModelState.AddModelError(nameof(model.Name), "فيه قسم بنفس الاسم بالفعل");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.Categories.Add(new Category
            {
                Name = model.Name.Trim(),
                DisplayOrder = model.DisplayOrder
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة القسم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                DisplayOrder = category.DisplayOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (await NameIsDuplicate(model.Name, excludeId: id))
            {
                ModelState.AddModelError(nameof(model.Name), "فيه قسم بنفس الاسم بالفعل");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = model.Name.Trim();
            category.DisplayOrder = model.DisplayOrder;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل القسم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            if (category.MenuItems.Any())
            {
                // The FK is also set to Restrict at the DB level, but checking here first
                // lets us show a clear Arabic message instead of a raw SQL exception.
                TempData["Error"] = "مينفعش تمسح القسم ده لأنه لسه فيه أصناف بداخله. امسح أو انقل الأصناف الأول.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف القسم";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> NameIsDuplicate(string name, int? excludeId)
        {
            var normalized = name.Trim();
            return await _context.Categories.AnyAsync(c =>
                c.Name == normalized && (excludeId == null || c.Id != excludeId));
        }
    }
}
