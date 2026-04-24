using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ISqlHelper _sql;
        public CustomersController(ISqlHelper sql) { _sql = sql; }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCustomerInput input)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId))
            {
                TempData["Error"] = "ไม่พบข้อมูลสาขาของผู้ใช้ปัจจุบัน";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Phone))
            {
                TempData["Error"] = "กรุณากรอกชื่อและเบอร์โทรให้ครบ";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _sql.ExecuteAsync(
                    @"INSERT INTO Customers (Id, BranchId, Name, Phone, Email, Points, TotalSpent, MemberLevel, IsActive, CreatedAt)
                      VALUES (@Id, @BranchId, @Name, @Phone, @Email, 0, 0, @MemberLevel, @IsActive, GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@Name", input.Name.Trim()),
                        new SqlParameter("@Phone", input.Phone.Trim()),
                        new SqlParameter("@Email", string.IsNullOrWhiteSpace(input.Email) ? DBNull.Value : input.Email.Trim()),
                        new SqlParameter("@MemberLevel", string.IsNullOrWhiteSpace(input.MemberLevel) ? "NORMAL" : input.MemberLevel.Trim()),
                        new SqlParameter("@IsActive", input.IsActive)
                    });
                TempData["Success"] = "เพิ่มข้อมูลลูกค้าเรียบร้อยแล้ว";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เพิ่มข้อมูลลูกค้าไม่สำเร็จ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Customers SET IsActive = 0 WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ระงับการใช้งานลูกค้าสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index(string? search, bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "customers";
            ViewData["PageTitle"] = "ลูกค้า";
            ViewData["TopIcon"] = "user-check";
            ViewData["OpenCreate"] = openCreate;

            // Customers columns: Id, BranchId, Name, Phone, Email, Points, TotalSpent, MemberLevel, IsActive, CreatedAt
            var branchId = User.FindFirst("BranchId")?.Value;
            var items = await _sql.QueryAsync<CustomerRow>(
                "SELECT Id, Name, Phone, Email, Points, TotalSpent, MemberLevel, IsActive, CreatedAt " +
                "FROM Customers WHERE BranchId = @BranchId " +
                "AND (@Search IS NULL OR Name LIKE '%'+@Search+'%' OR Phone LIKE '%'+@Search+'%') " +
                "ORDER BY Name",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@BranchId", (object?)branchId ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Search", (object?)search ?? DBNull.Value)
                });
            ViewData["Search"] = search;
            return View(items);
        }

        public class CustomerRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public int Points { get; set; }
            public decimal TotalSpent { get; set; }
            public string? MemberLevel { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CreateCustomerInput
        {
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
            public string? Email { get; set; }
            public string? MemberLevel { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
}
