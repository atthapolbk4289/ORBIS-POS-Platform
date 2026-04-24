using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์สำหรับจัดการข้อมูลผู้จำหน่าย (Suppliers)
    /// </summary>
    [Authorize(Roles = "IT_ADMIN,MANAGER,STOCK_KEEPER")]
    public class SuppliersController : Controller
    {
        private readonly ISqlHelper _sql;
        public SuppliersController(ISqlHelper sql) { _sql = sql; }

        /// <summary>
        /// แสดงรายการผู้จำหน่ายทั้งหมดในสาขา
        /// </summary>
        /// <param name="openCreate">กำหนดว่าจะให้เปิด Modal สร้างผู้จำหน่ายทันทีหรือไม่</param>
        public async Task<IActionResult> Index(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "stock";
            ViewData["PageTitle"] = "จัดการผู้จำหน่าย (Suppliers)";
            ViewData["TopIcon"] = "truck";
            ViewData["OpenCreate"] = openCreate;

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId)) return View(new List<SupplierRow>());

            // ดึงข้อมูลผู้จำหน่ายจากฐานข้อมูล
            var items = await _sql.QueryAsync<SupplierRow>(
                "SELECT Id, Name, ContactName, Phone, Email, IsActive FROM Suppliers WHERE BranchId = @BranchId ORDER BY Name",
                new[] { new SqlParameter("@BranchId", branchId) });

            return View(items);
        }

        /// <summary>
        /// บันทึกข้อมูลผู้จำหน่ายใหม่
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateSupplierInput input)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId) || string.IsNullOrWhiteSpace(input.Name))
            {
                TempData["Error"] = "กรุณากรอกชื่อผู้จำหน่าย";
                return RedirectToAction(nameof(Index));
            }

            try {
                // บันทึกข้อมูลลงในตาราง Suppliers
                await _sql.ExecuteAsync(
                    @"INSERT INTO Suppliers (Id, BranchId, Name, ContactName, Phone, Email, Address, TaxId, IsActive, CreatedAt)
                      VALUES (@Id, @BranchId, @Name, @Contact, @Phone, @Email, @Address, @TaxId, 1, GETUTCDATE())",
                    new[] {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@Name", input.Name.Trim()),
                        new SqlParameter("@Contact", (object?)input.ContactName ?? DBNull.Value),
                        new SqlParameter("@Phone", (object?)input.Phone ?? DBNull.Value),
                        new SqlParameter("@Email", (object?)input.Email ?? DBNull.Value),
                        new SqlParameter("@Address", (object?)input.Address ?? DBNull.Value),
                        new SqlParameter("@TaxId", (object?)input.TaxId ?? DBNull.Value)
                    });
                TempData["Success"] = "เพิ่มผู้จำหน่ายสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ระงับการใช้งานผู้จำหน่าย (Soft Delete)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Suspend(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Suppliers SET IsActive = 0 WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ระงับการใช้งานผู้จำหน่ายสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ปลดระงับการใช้งานผู้จำหน่าย
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Unsuspend(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Suppliers SET IsActive = 1 WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ปลดระงับผู้จำหน่ายสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ลบข้อมูลผู้จำหน่าย (Hard Delete)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("DELETE FROM Suppliers WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ลบข้อมูลผู้จำหน่ายสำเร็จ";
            } catch (SqlException ex) when (ex.Number == 547) {
                TempData["Error"] = "ไม่สามารถลบได้ เนื่องจากมีข้อมูลใบสั่งซื้อที่เชื่อมโยงกับผู้จำหน่ายนี้ (แนะนำให้ใช้การ 'ระงับ' แทน)";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public class SupplierRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string? ContactName { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateSupplierInput
        {
            public string Name { get; set; } = "";
            public string? ContactName { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? TaxId { get; set; }
        }
    }
}

