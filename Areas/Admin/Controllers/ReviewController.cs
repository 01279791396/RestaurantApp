using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class ReviewController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? rating)
        {
            var query = _context.Reviews
                .Include(r => r.MenuItem)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            ViewBag.SelectedRating = rating;
            ViewBag.AverageRating = await _context.Reviews.AnyAsync()
                ? await _context.Reviews.AverageAsync(r => r.Rating)
                : 0;
            ViewBag.TotalCount = await _context.Reviews.CountAsync();

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound();

            return View(review);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف التقييم";
            return RedirectToAction(nameof(Index));
        }
    }
}