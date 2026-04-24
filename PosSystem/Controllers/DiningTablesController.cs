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
    public class DiningTablesController : Controller
    {
        private readonly ISqlHelper _sql;
        public DiningTablesController(ISqlHelper sql) { _sql = sql; }

        public async Task<IActionResult> Index(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "pos"; // Link to POS section
            ViewData["PageTitle"] = "จัดการโต๊ะอาหาร";
            ViewData["TopIcon"] = "grid";
            ViewData["OpenCreate"] = openCreate;

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId)) return View(new List<TableRow>());

            var tables = await _sql.QueryAsync<TableRow>(
                "SELECT Id, TableNumber, Zone, Capacity, Status, IsActive FROM DiningTables WHERE BranchId = @BranchId ORDER BY Zone, TableNumber",
                new[] { new SqlParameter("@BranchId", branchId) });

            return View(tables);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string tableNumber, string zone, int capacity)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId) || string.IsNullOrWhiteSpace(tableNumber))
            {
                TempData["Error"] = "ข้อมูลไม่ถูกต้อง";
                return RedirectToAction(nameof(Index));
            }

            try {
                await _sql.ExecuteAsync(
                    "INSERT INTO DiningTables (Id, BranchId, TableNumber, Zone, Capacity, Status, IsActive) VALUES (@Id, @BranchId, @No, @Zone, @Cap, 'AVAILABLE', 1)",
                    new[] {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@No", tableNumber.Trim()),
                        new SqlParameter("@Zone", string.IsNullOrWhiteSpace(zone) ? "Main" : zone.Trim()),
                        new SqlParameter("@Cap", capacity <= 0 ? 4 : capacity)
                    });
                TempData["Success"] = "เพิ่มโต๊ะสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("DELETE FROM DiningTables WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ลบโต๊ะสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ลบไม่สำเร็จ: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public class TableRow
        {
            public Guid Id { get; set; }
            public string TableNumber { get; set; } = "";
            public string Zone { get; set; } = "";
            public int Capacity { get; set; }
            public string Status { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
