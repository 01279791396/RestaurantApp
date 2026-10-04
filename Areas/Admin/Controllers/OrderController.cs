using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Areas.Admin.Models;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Areas.Admin.Controllers
{
    public class OrderController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            ViewBag.SelectedStatus = status;
            return View(await query.ToListAsync());
        }

        // Polled from Index.cshtml every few seconds to sound an alert when a new pending
        // Two-step destructive action (confirm page, then the actual delete on POST) for
        // wiping test orders before going live — never a single click. Cascades to
        // OrderItems automatically (see ApplicationDbContext); never touches MenuItems,
        // Categories, Customers, or Coupons.
        public async Task<IActionResult> ClearAllConfirm()
        {
            ViewBag.OrderCount = await _context.Orders.CountAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var count = await _context.Orders.ExecuteDeleteAsync();
            TempData["Success"] = $"تم حذف {count} أوردر نهائيًا";
            return RedirectToAction(nameof(Index));
        }

        // Same idea, but only the orders that never actually completed — Cancelled, or still
        // stuck somewhere in Pending/Accepted/Preparing/OutForDelivery. Delivered orders (the
        // real sales history) are always kept.
        public async Task<IActionResult> ClearIncompleteConfirm()
        {
            ViewBag.OrderCount = await _context.Orders.CountAsync(o => o.Status != OrderStatus.Delivered);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearIncomplete()
        {
            var count = await _context.Orders
                .Where(o => o.Status != OrderStatus.Delivered)
                .ExecuteDeleteAsync();
            TempData["Success"] = $"تم حذف {count} أوردر (الملغي وغير المكتمل)، واتسيبت الأوردرات المُسلَّمة";
            return RedirectToAction(nameof(Index));
        }
        // order arrives. Returns the highest Pending order Id (not just a count) so the client
        // can tell "a new order arrived" apart from "an old one just got accepted" — a raw
        // count can't distinguish those two cases.
        [HttpGet]
        public async Task<IActionResult> PendingCount()
        {
            var pending = await _context.Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .ToListAsync();

            return Json(new
            {
                count = pending.Count,
                latestId = pending.Any() ? pending.Max(o => o.Id) : 0
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            return View(new AdminOrderDetailsViewModel { Order = order });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            return await TransitionAsync(id, from: OrderStatus.Pending, to: OrderStatus.Accepted);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPreparing(int id)
        {
            return await TransitionAsync(id, from: OrderStatus.Accepted, to: OrderStatus.Preparing);
        }

        // Delivery is arranged by phone call, not a staff account — the admin picks up the
        // phone, agrees a delivery person, and records their name/phone + the delivery fee
        // that person will collect from the customer on top of the order total.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDelivery(int id, string deliveryPersonName, string deliveryPersonPhone, decimal deliveryFee)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.Preparing)
            {
                TempData["Error"] = "الأوردر لازم يكون في حالة (جاري التحضير) الأول قبل ما يتسند لمندوب توصيل";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(deliveryPersonName) || string.IsNullOrWhiteSpace(deliveryPersonPhone))
            {
                TempData["Error"] = "اكتب اسم ورقم موبايل مندوب التوصيل";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (deliveryFee < 0)
            {
                TempData["Error"] = "مصاريف التوصيل مينفعش تكون بالسالب";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.DeliveryPersonName = deliveryPersonName.Trim();
            order.DeliveryPersonPhone = deliveryPersonPhone.Trim();
            order.DeliveryFee = deliveryFee;
            order.Status = OrderStatus.OutForDelivery;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إسناد الأوردر لـ {order.DeliveryPersonName}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // The delivery person hands back the DeliveryFee in cash once they return —
        // confirmed here once that money is back with the restaurant.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.OutForDelivery)
            {
                TempData["Error"] = "الأوردر لازم يكون (مع مندوب التوصيل) الأول";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;
            order.IsPaid = true; // cash on delivery — collected at hand-off
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تسجيل الأوردر كمُسلَّم";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Accepted)
            {
                TempData["Error"] = "مينفعش تلغي أوردر وصل للمرحلة دي. اتصل بالعميل لو فيه مشكلة.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء الأوردر";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Printable receipt meant to be printed and kept with the order (or read out over the
        // phone to the delivery person), so the right amount gets collected from the customer.
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            return View(order);
        }

        private async Task<IActionResult> TransitionAsync(int id, OrderStatus from, OrderStatus to)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != from)
            {
                TempData["Error"] = "حالة الأوردر اتغيرت بالفعل";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = to;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث حالة الأوردر";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
