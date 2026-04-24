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
    public class BranchesController : Controller
    {
        private readonly ISqlHelper _sql;
        public BranchesController(ISqlHelper sql) { _sql = sql; }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBranchInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                TempData["Error"] = "กรุณากรอกชื่อสาขา";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _sql.ExecuteAsync(
                    @"INSERT INTO Branches (Id, Name, Address, Phone, TaxId, IsActive, CreatedAt, UpdatedAt)
                      VALUES (@Id, @Name, @Address, @Phone, @TaxId, @IsActive, GETUTCDATE(), GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@Name", input.Name.Trim()),
                        new SqlParameter("@Address", string.IsNullOrWhiteSpace(input.Address) ? DBNull.Value : input.Address.Trim()),
                        new SqlParameter("@Phone", string.IsNullOrWhiteSpace(input.Phone) ? DBNull.Value : input.Phone.Trim()),
                        new SqlParameter("@TaxId", string.IsNullOrWhiteSpace(input.TaxId) ? DBNull.Value : input.TaxId.Trim()),
                        new SqlParameter("@IsActive", input.IsActive)
                    });
                TempData["Success"] = "เพิ่มสาขาเรียบร้อยแล้ว";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เพิ่มสาขาไม่สำเร็จ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Branches SET IsActive = 0 WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ปิดการใช้งานสาขาสัมเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "branches";
            ViewData["PageTitle"] = "จัดการสาขา";
            ViewData["TopIcon"] = "building";
            ViewData["OpenCreate"] = openCreate;
            // Branches table: Id, Name, Address, Phone, TaxId, IsActive, CreatedAt, UpdatedAt
            var items = await _sql.QueryAsync<BranchRow>(
                "SELECT Id, Name, Phone, Address, TaxId, IsActive FROM Branches ORDER BY Name");
            return View(items);
        }

        public class BranchRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? TaxId { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateBranchInput
        {
            public string Name { get; set; } = "";
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public string? TaxId { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
}
