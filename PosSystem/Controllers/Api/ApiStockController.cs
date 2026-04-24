using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosSystem.Helpers;
using System;
using System.Threading.Tasks;

namespace PosSystem.Controllers.Api
{
    [ApiController]
    [Route("api/stock")]
    [Authorize]
    public class ApiStockController : ControllerBase
    {
        private readonly ISqlHelper _sql;
        public ApiStockController(ISqlHelper sql) { _sql = sql; }

        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustRequest req)
        {
            if (req.Quantity < 0)
                return Ok(new { success = false, message = "จำนวนต้องไม่ติดลบ" });

            var branchId = User.FindFirst("BranchId")?.Value;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Get current stock
            var current = await _sql.QueryFirstOrDefaultAsync<StockRow>(
                "SELECT ps.Id, ps.PhysicalQty FROM ProductStocks ps " +
                "WHERE ps.ProductId=@PId AND ps.BranchId=@BId",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId)
                });

            if (current == null)
                return Ok(new { success = false, message = "ไม่พบสินค้าในคลัง" });

            int newQty = req.AdjustType switch {
                "ADD" => current.PhysicalQty + req.Quantity,
                "SUB" => current.PhysicalQty - req.Quantity,
                _     => req.Quantity  // SET
            };

            if (newQty < 0)
                return Ok(new { success = false, message = "จำนวนไม่เพียงพอ ปัจจุบัน: " + current.PhysicalQty });

            int delta = newQty - current.PhysicalQty;

            // Update stock
            await _sql.ExecuteAsync(
                "UPDATE ProductStocks SET PhysicalQty=@Qty, UpdatedAt=GETUTCDATE() WHERE Id=@Id",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", newQty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", current.Id)
                });

            // Record movement
            await _sql.ExecuteAsync(
                "INSERT INTO StockMovements(Id,BranchId,ProductId,MovementType,Quantity,Note,CreatedBy,CreatedAt) " +
                "VALUES(@Id,@BId,@PId,@Type,@Qty,@Note,@By,GETUTCDATE())",
                new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", Guid.NewGuid()),
                    new Microsoft.Data.SqlClient.SqlParameter("@BId", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@PId", req.ProductId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Type", "ADJUST"),
                    new Microsoft.Data.SqlClient.SqlParameter("@Qty", delta),
                    new Microsoft.Data.SqlClient.SqlParameter("@Note", (object?)req.Note ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@By", userId)
                });

            return Ok(new { success = true, message = "ปรับสต็อกสำเร็จ", newQty });
        }

        public class AdjustRequest
        {
            public Guid ProductId { get; set; }
            public string AdjustType { get; set; } = "SET"; // SET, ADD, SUB
            public int Quantity { get; set; }
            public string? Note { get; set; }
        }

        public class StockRow
        {
            public Guid Id { get; set; }
            public int PhysicalQty { get; set; }
        }
    }
}
