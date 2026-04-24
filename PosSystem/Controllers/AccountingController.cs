using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PosSystem.Controllers
{
    [Authorize]
    public class AccountingController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "accounting";
            ViewData["PageTitle"] = "บัญชี";
            ViewData["TopIcon"] = "book-open";
            return View();
        }
    }
}
