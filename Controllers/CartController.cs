using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;

namespace RestaurantApp.Controllers
{
    // Browsing and building a cart never requires login — only Checkout does
    // (enforced in OrderController). This matches how customers actually shop.
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetCart();
            return View(new CartViewModel { Items = cart });
        }

        // sizeId is required for an item that has sizes, and must be omitted (null) for one
        // that doesn't — Menu/Index only ever submits the form that matches the item's shape.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int menuItemId, int quantity = 1, int? sizeId = null)
        {
            if (quantity < 1) quantity = 1;

            var menuItem = await _context.MenuItems
                .Include(m => m.Sizes)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
            {
                TempData["Error"] = "الصنف ده مش متاح دلوقتي";
                return RedirectToAction("Index", "Menu");
            }

            var isVip = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                isVip = user?.IsVip ?? false;
            }

            string displayName = menuItem.Name;
            decimal effectivePrice;
            string? sizeName = null;

            if (menuItem.HasSizes)
            {
                var size = menuItem.Sizes.FirstOrDefault(s => s.Id == sizeId);
                if (size == null)
                {
                    TempData["Error"] = "اختار المقاس الأول";
                    return RedirectToAction("Index", "Menu");
                }
                effectivePrice = size.EffectivePriceFor(isVip);
                sizeName = size.Name;
            }
            else
            {
                effectivePrice = menuItem.EffectivePriceFor(isVip);
                sizeId = null; // ignore any stray sizeId for a non-sized item
            }

            var cart = HttpContext.Session.GetCart();
            // Same item + same size (including "no size") merges quantities; a different size
            // of the same item is a separate line.
            var existing = cart.FirstOrDefault(i => i.MenuItemId == menuItemId && i.SizeId == sizeId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.Price = effectivePrice; // refresh in case price/VIP status changed
            }
            else
            {
                cart.Add(new CartItem
                {
                    MenuItemId = menuItem.Id,
                    Name = displayName,
                    Price = effectivePrice,
                    Quantity = quantity,
                    ImagePath = menuItem.ImagePath,
                    SizeId = sizeId,
                    SizeName = sizeName
                });
            }

            HttpContext.Session.SaveCart(cart);
            TempData["Success"] = $"تمت إضافة {displayName}{(sizeName != null ? " (" + sizeName + ")" : "")} للسلة";
            return RedirectToAction("Index", "Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int menuItemId, int quantity, int? sizeId = null)
        {
            var cart = HttpContext.Session.GetCart();
            var item = cart.FirstOrDefault(i => i.MenuItemId == menuItemId && i.SizeId == sizeId);
            if (item != null)
            {
                if (quantity < 1)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                HttpContext.Session.SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int menuItemId, int? sizeId = null)
        {
            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll(i => i.MenuItemId == menuItemId && i.SizeId == sizeId);
            HttpContext.Session.SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }
    }
}
