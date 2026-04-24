using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class CostController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "cost";
            ViewData["PageTitle"] = "จัดการสินค้าต้นทุน";
            ViewData["TopIcon"] = "dollar-sign";
            return View();
        }
    }
}
