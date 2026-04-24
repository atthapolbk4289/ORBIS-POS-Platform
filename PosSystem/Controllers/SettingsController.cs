using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize(Roles = "IT_ADMIN")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["PageTitle"] = "ตั้งค่าระบบ";
            ViewData["TopIcon"] = "settings";
            return View();
        }
    }
}
