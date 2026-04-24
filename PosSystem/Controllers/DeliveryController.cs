using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class DeliveryController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "delivery";
            ViewData["PageTitle"] = "ขนส่งสินค้า";
            ViewData["TopIcon"] = "truck";
            return View();
        }
    }
}
