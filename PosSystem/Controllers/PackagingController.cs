using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class PackagingController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "packaging";
            ViewData["PageTitle"] = "แบ่งบรรจุสินค้า";
            ViewData["TopIcon"] = "boxes";
            return View();
        }
    }
}
