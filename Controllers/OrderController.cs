using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;

namespace RestaurantApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWhatsAppNotifier _whatsAppNotifier;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWhatsAppNotifier whatsAppNotifier)
        {
            _context = context;
            _userManager = userManager;
            _whatsAppNotifier = whatsAppNotifier;
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "السلة فاضية";
                return RedirectToAction("Index", "Cart");
            }

            var settings = await _context.RestaurantSettings.FirstOrDefaultAsync();
            if (settings != null && !settings.IsOpenAt(DateTime.Now))
            {
                TempData["Error"] = $"المطعم قافل دلوقتي. مواعيد الشغل: من {settings.OpenTime:hh\\:mm} لحد {settings.CloseTime:hh\\:mm}.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            return View(new CheckoutViewModel
            {
                CartItems = cart,
                DeliveryAddress = user?.DefaultAddress ?? string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "السلة فاضية";
                return RedirectToAction("Index", "Cart");
            }
            model.CartItems = cart;

            var settings = await _context.RestaurantSettings.FirstOrDefaultAsync();
            if (settings != null && !settings.IsOpenAt(DateTime.Now))
            {
                TempData["Error"] = $"المطعم قافل دلوقتي. مواعيد الشغل: من {settings.OpenTime:hh\\:mm} لحد {settings.CloseTime:hh\\:mm}.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isVip = currentUser?.IsVip ?? false;

            // Re-fetch current price/availability from the DB rather than trusting the cart
            // snapshot — a menu item could have been disabled or repriced (or the customer's
            // VIP status could have changed) since it was added.
            var menuItemIds = cart.Select(i => i.MenuItemId).ToList();
            var menuItems = await _context.MenuItems
                .Include(m => m.Sizes)
                .Where(m => menuItemIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            var unavailableLines = new HashSet<(int MenuItemId, int? SizeId)>();
            var unavailableNames = new List<string>();
            var orderItems = new List<OrderItem>();
            decimal subtotal = 0;

            foreach (var cartItem in cart)
            {
                if (!menuItems.TryGetValue(cartItem.MenuItemId, out var menuItem) || !menuItem.IsAvailable)
                {
                    unavailableLines.Add((cartItem.MenuItemId, cartItem.SizeId));
                    unavailableNames.Add(cartItem.Name);
                    continue;
                }

                decimal unitPrice;
                string? sizeName = null;

                if (menuItem.HasSizes)
                {
                    var size = menuItem.Sizes.FirstOrDefault(s => s.Id == cartItem.SizeId);
                    if (size == null)
                    {
                        // The size was removed (or this item switched to using sizes) since
                        // it was added to the cart.
                        unavailableLines.Add((cartItem.MenuItemId, cartItem.SizeId));
                        unavailableNames.Add($"{cartItem.Name} ({cartItem.SizeName})");
                        continue;
                    }
                    unitPrice = size.EffectivePriceFor(isVip);
                    sizeName = size.Name;
                }
                else
                {
                    unitPrice = menuItem.EffectivePriceFor(isVip);
                }

                var lineItem = new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    ItemNameAtOrderTime = menuItem.Name,
                    SizeNameAtOrderTime = sizeName,
                    UnitPriceAtOrderTime = unitPrice,
                    Quantity = cartItem.Quantity
                };
                orderItems.Add(lineItem);
                subtotal += unitPrice * lineItem.Quantity;
            }

            if (unavailableLines.Any())
            {
                TempData["Error"] = $"للأسف الأصناف دي بقت مش متاحة: {string.Join("، ", unavailableNames)}. اتشالوا من السلة، راجع طلبك.";
                // Drop just the unavailable lines from the session cart so the customer sees
                // an accurate cart instead of hitting the same error again.
                var remaining = cart.Where(i => !unavailableLines.Contains((i.MenuItemId, i.SizeId))).ToList();
                HttpContext.Session.SaveCart(remaining);
                return RedirectToAction("Index", "Cart");
            }

            // Validate the coupon (if any) against the current items subtotal.
            string? appliedCouponCode = null;
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(model.CouponCode))
            {
                var normalizedCode = model.CouponCode.Trim().ToUpperInvariant();
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == normalizedCode);

                if (coupon == null || !coupon.IsValidNow)
                {
                    ModelState.AddModelError(nameof(model.CouponCode), "كود الخصم غير صحيح أو منتهي");
                    return View(model);
                }
                if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                {
                    ModelState.AddModelError(nameof(model.CouponCode),
                        $"الكود ده محتاج أوردر بحد أدنى {coupon.MinOrderAmount.Value:0.00} ج.م");
                    return View(model);
                }

                discountAmount = coupon.CalculateDiscount(subtotal);
                appliedCouponCode = coupon.Code;
            }

            var userId = _userManager.GetUserId(User)!;

            var order = new Order
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                CustomerId = userId,
                DeliveryAddress = model.DeliveryAddress.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                Status = OrderStatus.Pending,
                TotalPrice = subtotal,
                CouponCode = appliedCouponCode,
                DiscountAmount = discountAmount,
                IsPaid = false, // cash on delivery — marked paid by the admin once the delivery fee is handed back
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.ClearCart();

            // Best-effort — NotifyNewOrderAsync never throws, so a WhatsApp hiccup can never
            // stop the customer from seeing their order confirmation.
            order.Customer = currentUser;
            await _whatsAppNotifier.NotifyNewOrderAsync(order);

            TempData["Success"] = $"تم إنشاء طلبك بنجاح! رقم الفاتورة: {order.InvoiceNumber}";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isOwner = order.CustomerId == userId;
            var isStaff = User.IsInRole("Admin");

            if (!isOwner && !isStaff) return Forbid();

            return View(new OrderDetailsViewModel { Order = order });
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"INV-{datePart}-";

            // Not perfectly race-proof under heavy concurrent load, but the unique index on
            // InvoiceNumber (see ApplicationDbContext) guarantees no two orders ever collide —
            // worst case a retry here just picks the next number up.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var countToday = await _context.Orders
                    .CountAsync(o => o.InvoiceNumber.StartsWith(prefix));
                var candidate = $"{prefix}{(countToday + 1 + attempt):0000}";

                var exists = await _context.Orders.AnyAsync(o => o.InvoiceNumber == candidate);
                if (!exists) return candidate;
            }

            // Extremely unlikely fallback: fall back to a value that's unique by construction.
            return $"{prefix}{Guid.NewGuid().ToString("N")[..6]}";
        }
    }
}
