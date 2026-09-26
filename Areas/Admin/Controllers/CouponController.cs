using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class CouponController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CouponController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(coupons);
        }

        public IActionResult Create()
        {
            return View(new CouponFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponFormViewModel model)
        {
            await ValidateAsync(model, excludeId: null);
            if (!ModelState.IsValid) return View(model);

            _context.Coupons.Add(new Coupon
            {
                Code = model.Code.Trim().ToUpperInvariant(),
                DiscountType = model.DiscountType,
                Value = model.Value,
                MinOrderAmount = model.MinOrderAmount,
                ExpiresAt = model.ExpiresAt,
                IsActive = model.IsActive
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة كود الخصم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View(new CouponFormViewModel
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountType = coupon.DiscountType,
                Value = coupon.Value,
                MinOrderAmount = coupon.MinOrderAmount,
                ExpiresAt = coupon.ExpiresAt,
                IsActive = coupon.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CouponFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            await ValidateAsync(model, excludeId: id);
            if (!ModelState.IsValid) return View(model);

            coupon.Code = model.Code.Trim().ToUpperInvariant();
            coupon.DiscountType = model.DiscountType;
            coupon.Value = model.Value;
            coupon.MinOrderAmount = model.MinOrderAmount;
            coupon.ExpiresAt = model.ExpiresAt;
            coupon.IsActive = model.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تعديل كود الخصم";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            coupon.IsActive = !coupon.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            var usedOnOrder = await _context.Orders.AnyAsync(o => o.CouponCode == coupon.Code);
            if (usedOnOrder)
            {
                // Keep past orders' recorded coupon code meaningful — deactivate instead.
                TempData["Error"] = "الكود ده استُخدم في أوردرات سابقة، فمينفعش يتمسح. تقدر توقفه (غير فعّال) بدل ما تمسحه.";
                return RedirectToAction(nameof(Index));
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف كود الخصم";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateAsync(CouponFormViewModel model, int? excludeId)
        {
            if (model.DiscountType == DiscountType.Percentage && model.Value > 100)
            {
                ModelState.AddModelError(nameof(model.Value), "النسبة المئوية مينفعش تزيد عن 100");
            }

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            var isDuplicate = await _context.Coupons.AnyAsync(c =>
                c.Code == normalizedCode && (excludeId == null || c.Id != excludeId));
            if (isDuplicate)
            {
                ModelState.AddModelError(nameof(model.Code), "فيه كود خصم بنفس الاسم بالفعل");
            }
        }
    }
}
