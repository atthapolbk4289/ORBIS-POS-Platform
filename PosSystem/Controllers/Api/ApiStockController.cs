using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosSystem.Helpers;
using PosSystem.Models.ViewModels.PosSystem.Models.Dtos;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace PosSystem.Controllers.Api
{
    /// <summary>
    /// API สำหรับจัดการสต็อกสินค้า (Ajax)
    /// </summary>
    [ApiController]
    [Route("api/stock")]
    [Authorize]
    public class ApiStockController : ControllerBase
    {
        private readonly ISqlHelper _sql;
        public ApiStockController(ISqlHelper sql) { _sql = sql; }

        private string? GetBranchId() => User.FindFirst("BranchId")?.Value;
        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;

        /// <summary>
        /// ปรับสต็อกสินค้า (SET, ADD, SUB)
        /// </summary>
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockDto req)
        {
            try
            {
                if (req.Quantity < 0) return Ok(new { success = false, message = "จำนวนต้องไม่ติดลบ" });

                var branchIdStr = GetBranchId();
                var userIdStr = GetUserId();

                if (!Guid.TryParse(branchIdStr, out var branchId)) return Ok(new { success = false, message = "ไม่พบสาขา: " + branchIdStr });
                if (!Guid.TryParse(userIdStr, out var userId)) return Ok(new { success = false, message = "ไม่พบผู้ใช้: " + userIdStr });

                var current = await _sql.QueryFirstOrDefaultAsync<StockRow>(
                    "SELECT Id, PhysicalQty FROM ProductStocks WHERE ProductId=@PId AND BranchId=@BId",
                    new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                        new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId)
                    });

                if (current == null) return Ok(new { success = false, message = "ไม่พบสินค้าในคลังสำหรับสาขานี้" });

                int newQty = req.AdjustType switch {
                    "ADD" => current.PhysicalQty + req.Quantity,
                    "SUB" => current.PhysicalQty - req.Quantity,
                    _     => req.Quantity
                };

                if (newQty < 0) return Ok(new { success = false, message = "สต็อกไม่เพียงพอที่จะหักออก" });

                int delta = newQty - current.PhysicalQty;

                await _sql.ExecuteAsync(
                    "UPDATE ProductStocks SET PhysicalQty=@Qty, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                    new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@Qty", newQty),
                        new Microsoft.Data.SqlClient.SqlParameter("@Id", current.Id)
                    });

                await _sql.ExecuteAsync(
                    @"INSERT INTO StockMovements (Id, BranchId, ProductId, UserId, MovementType, Qty, BalanceAfter, Note, CreatedAt)
                      VALUES (@Id, @BId, @PId, @By, 'ADJUST', @Qty, @After, @Note, GETUTCDATE())",
                    new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@Id", Guid.NewGuid()),
                        new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId),
                        new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                        new Microsoft.Data.SqlClient.SqlParameter("@By", userId),
                        new Microsoft.Data.SqlClient.SqlParameter("@Qty", delta),
                        new Microsoft.Data.SqlClient.SqlParameter("@After", newQty),
                        new Microsoft.Data.SqlClient.SqlParameter("@Note", req.Note ?? "ปรับปรุงสต็อกด้วยตนเอง")
                    });

                return Ok(new { success = true, message = "บันทึกการปรับสต็อกสำเร็จ", newQty });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Error: " + ex.Message + " | " + ex.StackTrace });
            }
        }

        /// <summary>
        /// รับสินค้าเข้าสต็อก (Stock In)
        /// </summary>
        [HttpPost("receive")]
        public async Task<IActionResult> Receive([FromBody] ReceiveStockDto req)
        {
            if (req.Quantity <= 0) return Ok(new { success = false, message = "จำนวนที่รับต้องมากกว่า 0" });

            var branchIdStr = GetBranchId();
            var userIdStr = GetUserId();

            if (!Guid.TryParse(branchIdStr, out var branchId)) return Ok(new { success = false, message = "ไม่พบสาขา" });
            if (!Guid.TryParse(userIdStr, out var userId)) return Ok(new { success = false, message = "ไม่พบผู้ใช้" });

            var stock = await _sql.QueryFirstOrDefaultAsync<StockRow>(
                "SELECT Id, PhysicalQty FROM ProductStocks WHERE ProductId=@PId AND BranchId=@BId",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId)
                });

            if (stock == null) return Ok(new { success = false, message = "ไม่พบสินค้าในคลัง" });

            int newQty = stock.PhysicalQty + req.Quantity;

            await _sql.ExecuteAsync(
                "UPDATE ProductStocks SET PhysicalQty=@Qty, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", newQty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", stock.Id)
                });

            await _sql.ExecuteAsync(
                @"INSERT INTO StockMovements (Id, BranchId, ProductId, UserId, MovementType, Qty, CostPrice, BalanceAfter, ReferenceId, Note, CreatedAt)
                  VALUES (@Id, @BId, @PId, @By, 'PURCHASE_IN', @Qty, @Cost, @After, @Ref, @Note, GETUTCDATE())",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", Guid.NewGuid()),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@By", userId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", req.Quantity),
                    new Microsoft.Data.SqlClient.SqlParameter("@Cost", req.CostPrice),
                    new Microsoft.Data.SqlClient.SqlParameter("@After", newQty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Ref", req.ReferenceNo ?? ""),
                    new Microsoft.Data.SqlClient.SqlParameter("@Note", req.Note ?? "รับสินค้าเข้าคลัง")
                });

            return Ok(new { success = true, message = "รับสินค้าเข้าสต็อกสำเร็จ", newQty });
        }

        /// <summary>
        /// แจ้งสินค้าชำรุดหรือเสียหาย (Damage Out)
        /// </summary>
        [HttpPost("damage")]
        public async Task<IActionResult> Damage([FromBody] DamageStockDto req)
        {
            if (req.Quantity <= 0) return Ok(new { success = false, message = "จำนวนต้องมากกว่า 0" });

            var branchIdStr = GetBranchId();
            var userIdStr = GetUserId();

            if (!Guid.TryParse(branchIdStr, out var branchId)) return Ok(new { success = false, message = "ไม่พบสาขา" });
            if (!Guid.TryParse(userIdStr, out var userId)) return Ok(new { success = false, message = "ไม่พบผู้ใช้" });

            var stock = await _sql.QueryFirstOrDefaultAsync<StockRow>(
                "SELECT Id, PhysicalQty FROM ProductStocks WHERE ProductId=@PId AND BranchId=@BId",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId)
                });

            if (stock == null || stock.PhysicalQty < req.Quantity)
                return Ok(new { success = false, message = "สินค้าไม่เพียงพอต่อการแจ้งทำลาย" });

            int newQty = stock.PhysicalQty - req.Quantity;

            await _sql.ExecuteAsync(
                "UPDATE ProductStocks SET PhysicalQty=@Qty, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", newQty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", stock.Id)
                });

            await _sql.ExecuteAsync(
                @"INSERT INTO StockMovements (Id, BranchId, ProductId, UserId, MovementType, Qty, BalanceAfter, Note, CreatedAt)
                  VALUES (@Id, @BId, @PId, @By, 'ADJUSTMENT_OUT', @Qty, @After, @Note, GETUTCDATE())",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", Guid.NewGuid()),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@By", userId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", -req.Quantity),
                    new Microsoft.Data.SqlClient.SqlParameter("@After", newQty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Note", "แจ้งทำลาย: " + (req.Reason ?? "ไม่ระบุสาเหตุ"))
                });

            return Ok(new { success = true, message = "บันทึกการแจ้งทำลายสินค้าสำเร็จ", newQty });
        }

        /// <summary>
        /// บันทึกผลการตรวจนับสต็อก (Stock Counting)
        /// </summary>
        [HttpPost("count")]
        public async Task<IActionResult> Count([FromBody] StockCountDto req)
        {
            var branchIdStr = GetBranchId();
            var userIdStr = GetUserId();

            if (!Guid.TryParse(branchIdStr, out var branchId)) return Ok(new { success = false, message = "ไม่พบสาขา" });
            if (!Guid.TryParse(userIdStr, out var userId)) return Ok(new { success = false, message = "ไม่พบผู้ใช้" });

            var stock = await _sql.QueryFirstOrDefaultAsync<StockRow>(
                "SELECT Id, PhysicalQty FROM ProductStocks WHERE ProductId=@PId AND BranchId=@BId",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId)
                });

            if (stock == null) return Ok(new { success = false, message = "ไม่พบสินค้าในคลัง" });

            int delta = req.ActualQuantity - stock.PhysicalQty;
            if (delta == 0) return Ok(new { success = true, message = "จำนวนตรงกัน ไม่มีการเปลี่ยนแปลง", newQty = stock.PhysicalQty });

            await _sql.ExecuteAsync(
                "UPDATE ProductStocks SET PhysicalQty=@Qty, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", req.ActualQuantity),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", stock.Id)
                });

            await _sql.ExecuteAsync(
                @"INSERT INTO StockMovements (Id, BranchId, ProductId, UserId, MovementType, Qty, BalanceAfter, Note, CreatedAt)
                  VALUES (@Id, @BId, @PId, @By, 'ADJUST', @Qty, @After, @Note, GETUTCDATE())",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", Guid.NewGuid()),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@By", userId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", delta),
                    new Microsoft.Data.SqlClient.SqlParameter("@After", req.ActualQuantity),
                    new Microsoft.Data.SqlClient.SqlParameter("@Note", "ผลการตรวจนับ: " + (req.Note ?? "ตรวจนับประจำงวด"))
                });

            return Ok(new { success = true, message = "บันทึกผลการตรวจนับสต็อกสำเร็จ", newQty = req.ActualQuantity });
        }

        public class StockRow
        {
            public Guid Id { get; set; }
            public int PhysicalQty { get; set; }
        }
    }
}

