using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class HRController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "hr";
            ViewData["PageTitle"] = "จัดการฝ่ายบุคคล";
            ViewData["TopIcon"] = "users";
            return View();
        }
    }
}
