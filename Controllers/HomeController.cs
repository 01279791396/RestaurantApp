using Microsoft.AspNetCore.Mvc;

namespace RestaurantApp.Controllers
{
    public class HomeController : Controller
    {
        // The menu IS the home page for a single-restaurant app — no separate landing page.
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Menu");
        }

        [Route("/Home/Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
