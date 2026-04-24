using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    [Authorize(Roles = "IT_ADMIN,MANAGER")]
    public class UsersController : Controller
    {
        private readonly ISqlHelper _sql;
        public UsersController(ISqlHelper sql) { _sql = sql; }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Password))
            {
                TempData["Error"] = "กรุณากรอกชื่อผู้ใช้ ชื่อ-นามสกุล และรหัสผ่านให้ครบ";
                return RedirectToAction(nameof(Index));
            }

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var currentBranchId))
            {
                TempData["Error"] = "ไม่พบข้อมูลสาขาของผู้ใช้ปัจจุบัน";
                return RedirectToAction(nameof(Index));
            }

            var targetBranchId = User.IsInRole("IT_ADMIN") && Guid.TryParse(input.BranchId, out var parsedBranchId)
                ? parsedBranchId
                : currentBranchId;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
            object pinHash = string.IsNullOrWhiteSpace(input.Pin) ? DBNull.Value : BCrypt.Net.BCrypt.HashPassword(input.Pin);
            try
            {
                await _sql.ExecuteAsync(
                    @"INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Phone, Email, Role, Status, Pin, CreatedAt, UpdatedAt)
                      VALUES (@Id, @BranchId, @Username, @PasswordHash, @FullName, @Phone, @Email, @Role, @Status, @Pin, GETUTCDATE(), GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", targetBranchId),
                        new SqlParameter("@Username", input.Username.Trim()),
                        new SqlParameter("@PasswordHash", passwordHash),
                        new SqlParameter("@FullName", input.FullName.Trim()),
                        new SqlParameter("@Phone", string.IsNullOrWhiteSpace(input.Phone) ? DBNull.Value : input.Phone.Trim()),
                        new SqlParameter("@Email", string.IsNullOrWhiteSpace(input.Email) ? DBNull.Value : input.Email.Trim()),
                        new SqlParameter("@Role", string.IsNullOrWhiteSpace(input.Role) ? "CASHIER" : input.Role.Trim()),
                        new SqlParameter("@Status", string.IsNullOrWhiteSpace(input.Status) ? "ACTIVE" : input.Status.Trim()),
                        new SqlParameter("@Pin", pinHash)
                    });
                TempData["Success"] = "เพิ่มผู้ใช้งานเรียบร้อยแล้ว";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เพิ่มผู้ใช้งานไม่สำเร็จ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Users SET Status = 'INACTIVE' WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ปิดการใช้งานผู้ใช้สำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var defaultPassword = "Admin@1234";
            try
            {
                var user = await _sql.QueryFirstOrDefaultAsync<UserRow>(
                    "SELECT Id, Username, FullName, Role, Status, LastLoginAt, '' AS BranchName FROM Users WHERE Id = @Id",
                    new[] { new SqlParameter("@Id", id) });
                if (user == null)
                {
                    TempData["Error"] = "ไม่พบผู้ใช้งานที่ต้องการรีเซ็ตรหัสผ่าน";
                    return RedirectToAction(nameof(Index));
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
                await _sql.ExecuteAsync(
                    "UPDATE Users SET PasswordHash = @PasswordHash, Status = 'ACTIVE', UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new[]
                    {
                        new SqlParameter("@PasswordHash", passwordHash),
                        new SqlParameter("@Id", id)
                    });

                TempData["Success"] = $"รีเซ็ตรหัสผ่านของ {user.Username} สำเร็จ (รหัสชั่วคราว: {defaultPassword})";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "รีเซ็ตรหัสผ่านไม่สำเร็จ: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "users";
            ViewData["PageTitle"] = "จัดการผู้ใช้งาน";
            ViewData["TopIcon"] = "shield";
            ViewData["OpenCreate"] = openCreate;

            // Users columns: Id, BranchId, Username, PasswordHash, FullName, Phone, Email, Role, Status, Pin, LastLoginAt, CreatedAt, UpdatedAt
            var branchId = User.FindFirst("BranchId")?.Value;
            var sql = User.IsInRole("IT_ADMIN")
                ? "SELECT u.Id, u.Username, u.FullName, u.Role, u.Status, u.LastLoginAt, b.Name AS BranchName FROM Users u JOIN Branches b ON u.BranchId = b.Id ORDER BY u.FullName"
                : "SELECT u.Id, u.Username, u.FullName, u.Role, u.Status, u.LastLoginAt, b.Name AS BranchName FROM Users u JOIN Branches b ON u.BranchId = b.Id WHERE u.BranchId = @BranchId ORDER BY u.FullName";

            var parameters = User.IsInRole("IT_ADMIN") ? null
                : new[] { new Microsoft.Data.SqlClient.SqlParameter("@BranchId", (object?)branchId ?? DBNull.Value) };

            var items = await _sql.QueryAsync<UserRow>(sql, parameters);
            var branches = await _sql.QueryAsync<BranchOption>("SELECT Id, Name FROM Branches WHERE IsActive = 1 ORDER BY Name");
            ViewData["Branches"] = branches;
            return View(items);
        }

        public IActionResult Roles()
        {
            ViewData["ActiveMenu"] = "users";
            ViewData["PageTitle"] = "กำหนดสิทธิ์";
            ViewData["TopIcon"] = "shield";
            return View();
        }

        public class UserRow
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Role { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime? LastLoginAt { get; set; }
            public string BranchName { get; set; } = "";
        }

        public class BranchOption
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class CreateUserInput
        {
            public string Username { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Password { get; set; } = "";
            public string? Role { get; set; }
            public string? Status { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Pin { get; set; }
            public string? BranchId { get; set; }
        }
    }
}
