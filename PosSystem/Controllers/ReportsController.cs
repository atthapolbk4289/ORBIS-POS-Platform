using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "reports";
            ViewData["PageTitle"] = "รายงาน";
            ViewData["TopIcon"] = "bar-chart-3";
            return View();
        }
    }
}
