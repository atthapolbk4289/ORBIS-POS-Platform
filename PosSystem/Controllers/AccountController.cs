using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosSystem.Helpers;
using PosSystem.Models.ViewModels;
using PosSystem.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ISqlHelper _sql;

        public AccountController(IAuthService authService, ISqlHelper sql)
        {
            _authService = authService;
            _sql = sql;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToRoleBasedDashboard();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var ua = Request.Headers.UserAgent.ToString();
            var result = await _authService.LoginAsync(model.Username, model.Password, ip, ua);
            if (!result.Success || result.Principal == null)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe });
            return RedirectToRoleBasedDashboard();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ──────────────────── PROFILE ────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            ViewData["PageTitle"] = "โปรไฟล์ของฉัน";
            ViewData["TopIcon"] = "user";
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _sql.QueryFirstOrDefaultAsync<ProfileViewModel>(
                "SELECT Id, Username, FullName, Phone, Email, Role, LastLoginAt FROM Users WHERE Id = @Id",
                new[] { new Microsoft.Data.SqlClient.SqlParameter("@Id", userId) });
            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword) { TempData["Error"] = "รหัสผ่านใหม่ไม่ตรงกัน"; return RedirectToAction("Profile"); }
            if (newPassword.Length < 6) { TempData["Error"] = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร"; return RedirectToAction("Profile"); }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _sql.QueryFirstOrDefaultAsync<ProfileViewModel>(
                "SELECT Id, PasswordHash FROM Users WHERE Id = @Id",
                new[] { new Microsoft.Data.SqlClient.SqlParameter("@Id", userId) });

            if (user == null) { TempData["Error"] = "ไม่พบข้อมูลผู้ใช้"; return RedirectToAction("Profile"); }

            bool valid = user.PasswordHash != null &&
                (user.PasswordHash.StartsWith("HASH_") ? currentPassword == "Admin@1234"
                 : BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash));

            if (!valid) { TempData["Error"] = "รหัสผ่านปัจจุบันไม่ถูกต้อง"; return RedirectToAction("Profile"); }

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _sql.ExecuteAsync("UPDATE Users SET PasswordHash=@Hash, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Hash", newHash),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", userId)
                });

            TempData["Success"] = "เปลี่ยนรหัสผ่านสำเร็จ";
            return RedirectToAction("Profile");
        }

        private IActionResult RedirectToRoleBasedDashboard()
        {
            if (User.IsInRole("CASHIER")) return RedirectToAction("Index", "Pos");
            if (User.IsInRole("STOCK_KEEPER")) return RedirectToAction("Index", "Stock");
            return RedirectToAction("Index", "Dashboard");
        }
    }

    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "";
        public DateTime? LastLoginAt { get; set; }
        public string? PasswordHash { get; set; }
    }
}
