using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PosSystem.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    [Authorize]
    /// <summary>
    /// API สำหรับจัดการคำสั่งซื้อ (Orders)
    /// </summary>
    public class ApiOrdersController : ControllerBase
    {
        private readonly ISqlHelper _sql;
        public ApiOrdersController(ISqlHelper sql) { _sql = sql; }

        public class PosProductDto
        {
            public Guid Id { get; set; }
            public Guid CategoryId { get; set; }
            public string Sku { get; set; } = "";
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public int AvailableQty { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class CreateOrderRequest
        {
            public string OrderType { get; set; } = "TAKEAWAY";
            public Guid? TableId { get; set; }
            public string PaymentMethod { get; set; } = "CASH";
            public decimal? CashReceived { get; set; }
            public string? ReferenceNo { get; set; }
            public decimal TaxRate { get; set; } = 7m;
            public List<CreateOrderItem> Cart { get; set; } = new();
        }

        public class CreateOrderItem
        {
            public Guid ProductId { get; set; }
            public int Qty { get; set; }
            public decimal Price { get; set; }
            public string? Note { get; set; }
        }

        /// <summary>
        /// ดึงรายการสินค้าสำหรับหน้า POS พร้อมจำนวนสต็อกที่เหลืออยู่
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProductsForPOS(Guid branchId, Guid? categoryId, string? search)
        {
            var sql = @"SELECT p.Id, p.CategoryId, p.Sku, p.Name, p.Price, p.ImageUrl,
                               (ps.PhysicalQty - ps.ReservedQty - ps.CommittedQty) as AvailableQty
                        FROM Products p
                        JOIN ProductStocks ps ON p.Id = ps.ProductId
                        WHERE p.BranchId = @BranchId AND p.IsActive = 1";
            var parameters = new List<SqlParameter> { new("@BranchId", branchId) };
            if (categoryId.HasValue)
            {
                sql += " AND p.CategoryId = @CatId";
                parameters.Add(new SqlParameter("@CatId", categoryId.Value));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (p.Name LIKE '%' + @Search + '%' OR p.Sku LIKE '%' + @Search + '%' OR p.Barcode LIKE '%' + @Search + '%')";
                parameters.Add(new SqlParameter("@Search", search.Trim()));
            }

            sql += " ORDER BY p.Name";
            var items = await _sql.QueryAsync<PosProductDto>(sql, parameters.ToArray());
            return Ok(new { success = true, data = items, message = "สำเร็จ" });
        }

        /// <summary>
        /// สร้างคำสั่งซื้อใหม่ (แบบชำระเงินทันที)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest dto)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            var userClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId) || !Guid.TryParse(userClaim, out var userId))
            {
                return BadRequest(new { success = false, message = "ไม่พบข้อมูลผู้ใช้งานหรือสาขา" });
            }
            if (dto.Cart == null || dto.Cart.Count == 0)
            {
                return BadRequest(new { success = false, message = "ตะกร้าสินค้าว่าง" });
            }

            var normalizedItems = dto.Cart
                .Where(x => x.ProductId != Guid.Empty && x.Qty > 0)
                .GroupBy(x => x.ProductId)
                .Select(g => new CreateOrderItem
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Price = g.Last().Price,
                    Note = g.Last().Note
                })
                .ToList();
            if (normalizedItems.Count == 0)
            {
                return BadRequest(new { success = false, message = "ข้อมูลสินค้าไม่ถูกต้อง" });
            }

            var orderId = Guid.NewGuid();
            var orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString("N")[..6]}";
            var taxRate = dto.TaxRate <= 0 ? 7m : dto.TaxRate;

            using var conn = await _sql.OpenConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                // ตรวจสอบสต็อกสินค้าแต่ละรายการก่อนทำรายการ
                decimal subtotal = 0m;
                foreach (var item in normalizedItems)
                {
                    using var stockCmd = new SqlCommand(
                        @"SELECT p.Price, p.CostPrice, (ps.PhysicalQty - ps.ReservedQty - ps.CommittedQty) AS AvailableQty
                          FROM Products p
                          JOIN ProductStocks ps ON p.Id = ps.ProductId
                          WHERE p.Id = @ProductId AND p.BranchId = @BranchId", conn, tx);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    stockCmd.Parameters.AddWithValue("@BranchId", branchId);
                    using var reader = await stockCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        throw new InvalidOperationException("ไม่พบสินค้าในสาขาปัจจุบัน");
                    }
                    var dbPrice = reader.GetDecimal(reader.GetOrdinal("Price"));
                    var availableQty = reader.GetInt32(reader.GetOrdinal("AvailableQty"));
                    await reader.CloseAsync();

                    if (availableQty < item.Qty)
                    {
                        throw new InvalidOperationException("สต็อกสินค้าไม่เพียงพอ");
                    }

                    item.Price = item.Price > 0 ? item.Price : dbPrice;
                    subtotal += item.Price * item.Qty;
                }

                // คำนวณภาษีและยอดรวม
                var taxAmount = Math.Round(subtotal * (taxRate / 100m), 2);
                var totalAmount = subtotal + taxAmount;
                var paidAt = DateTime.UtcNow;
                var orderType = string.IsNullOrWhiteSpace(dto.OrderType) ? "TAKEAWAY" : dto.OrderType.Trim().ToUpperInvariant();
                var paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "CASH" : dto.PaymentMethod.Trim().ToUpperInvariant();
                var changeAmount = paymentMethod == "CASH" && dto.CashReceived.HasValue
                    ? Math.Max(0, dto.CashReceived.Value - totalAmount)
                    : 0m;

                // บันทึกข้อมูลคำสั่งซื้อหลัก (Orders)
                using var orderCmd = new SqlCommand(
                    @"INSERT INTO Orders(Id, BranchId, UserId, OrderNumber, OrderType, TableId, Status, Note, Subtotal, DiscountAmount, TaxAmount, TaxRate, TotalAmount, PaymentStatus, CreatedAt, UpdatedAt, CompletedAt)
                      VALUES(@Id, @BranchId, @UserId, @OrderNumber, @OrderType, @TableId, 'COMPLETED', NULL, @Subtotal, 0, @TaxAmount, @TaxRate, @TotalAmount, 'PAID', @Now, @Now, @Now)", conn, tx);
                orderCmd.Parameters.AddWithValue("@Id", orderId);
                orderCmd.Parameters.AddWithValue("@BranchId", branchId);
                orderCmd.Parameters.AddWithValue("@UserId", userId);
                orderCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                orderCmd.Parameters.AddWithValue("@OrderType", orderType);
                orderCmd.Parameters.AddWithValue("@TableId", (object?)dto.TableId ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                orderCmd.Parameters.AddWithValue("@TaxAmount", taxAmount);
                orderCmd.Parameters.AddWithValue("@TaxRate", taxRate);
                orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                orderCmd.Parameters.AddWithValue("@Now", paidAt);
                await orderCmd.ExecuteNonQueryAsync();

                foreach (var item in normalizedItems)
                {
                    using var costCmd = new SqlCommand("SELECT CostPrice FROM Products WHERE Id=@ProductId", conn, tx);
                    costCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    var costObj = await costCmd.ExecuteScalarAsync();
                    var costPrice = costObj == null ? 0m : Convert.ToDecimal(costObj);

                    using var itemCmd = new SqlCommand(
                        @"INSERT INTO OrderItems(Id, OrderId, ProductId, VariantId, Qty, UnitPrice, CostPrice, DiscountAmount, LineTotal, Status, Note, SentToKitchenAt)
                          VALUES(@Id, @OrderId, @ProductId, NULL, @Qty, @UnitPrice, @CostPrice, 0, @LineTotal, 'SERVED', @Note, @Now)", conn, tx);
                    itemCmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@Qty", item.Qty);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.Price);
                    itemCmd.Parameters.AddWithValue("@CostPrice", costPrice);
                    itemCmd.Parameters.AddWithValue("@LineTotal", item.Price * item.Qty);
                    itemCmd.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(item.Note) ? DBNull.Value : item.Note.Trim());
                    itemCmd.Parameters.AddWithValue("@Now", paidAt);
                    await itemCmd.ExecuteNonQueryAsync();

                    using var stockUpdateCmd = new SqlCommand(
                        @"UPDATE ProductStocks
                          SET PhysicalQty = PhysicalQty - @Qty,
                              UpdatedAt = @Now
                          WHERE ProductId = @ProductId AND BranchId = @BranchId", conn, tx);
                    stockUpdateCmd.Parameters.AddWithValue("@Qty", item.Qty);
                    stockUpdateCmd.Parameters.AddWithValue("@Now", paidAt);
                    stockUpdateCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    stockUpdateCmd.Parameters.AddWithValue("@BranchId", branchId);
                    await stockUpdateCmd.ExecuteNonQueryAsync();

                    using var balanceCmd = new SqlCommand(
                        @"SELECT PhysicalQty FROM ProductStocks WHERE ProductId = @ProductId AND BranchId = @BranchId", conn, tx);
                    balanceCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    balanceCmd.Parameters.AddWithValue("@BranchId", branchId);
                    var balanceObj = await balanceCmd.ExecuteScalarAsync();
                    var balanceAfter = balanceObj == null ? 0 : Convert.ToInt32(balanceObj);

                    using var movementCmd = new SqlCommand(
                        @"INSERT INTO StockMovements(Id, BranchId, ProductId, UserId, MovementType, Qty, CostPrice, BalanceAfter, ReferenceId, ReferenceType, Note, CreatedAt)
                          VALUES(@Id, @BranchId, @ProductId, @UserId, 'SALE_OUT', @Qty, @CostPrice, @BalanceAfter, @ReferenceId, 'ORDER', NULL, @CreatedAt)", conn, tx);
                    movementCmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    movementCmd.Parameters.AddWithValue("@BranchId", branchId);
                    movementCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    movementCmd.Parameters.AddWithValue("@UserId", userId);
                    movementCmd.Parameters.AddWithValue("@Qty", item.Qty);
                    movementCmd.Parameters.AddWithValue("@CostPrice", costPrice);
                    movementCmd.Parameters.AddWithValue("@BalanceAfter", balanceAfter);
                    movementCmd.Parameters.AddWithValue("@ReferenceId", orderId.ToString());
                    movementCmd.Parameters.AddWithValue("@CreatedAt", paidAt);
                    await movementCmd.ExecuteNonQueryAsync();
                }

                // บันทึกข้อมูลการชำระเงิน (Payments)
                using var paymentCmd = new SqlCommand(
                    @"INSERT INTO Payments(Id, OrderId, UserId, PaymentMethod, Amount, ChangeAmount, ReferenceNo, Status, PaidAt)
                      VALUES(@Id, @OrderId, @UserId, @PaymentMethod, @Amount, @ChangeAmount, @ReferenceNo, 'SUCCESS', @PaidAt)", conn, tx);
                paymentCmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                paymentCmd.Parameters.AddWithValue("@OrderId", orderId);
                paymentCmd.Parameters.AddWithValue("@UserId", userId);
                paymentCmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                paymentCmd.Parameters.AddWithValue("@Amount", totalAmount);
                paymentCmd.Parameters.AddWithValue("@ChangeAmount", changeAmount);
                paymentCmd.Parameters.AddWithValue("@ReferenceNo", string.IsNullOrWhiteSpace(dto.ReferenceNo) ? DBNull.Value : dto.ReferenceNo.Trim());
                paymentCmd.Parameters.AddWithValue("@PaidAt", paidAt);
                await paymentCmd.ExecuteNonQueryAsync();

                tx.Commit();
                return Ok(new { success = true, data = new { orderId, orderNumber, totalAmount }, message = "บันทึกคำสั่งซื้อสำเร็จ" });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/add-item")]
        public IActionResult AddItem(Guid id, [FromBody] object dto)
        {
            return Ok(new { success = true, data = true, message = "สำเร็จ" });
        }

        [HttpDelete("{id}/items/{itemId}")]
        public IActionResult RemoveItem(Guid id, Guid itemId)
        {
            return Ok(new { success = true, data = true, message = "สำเร็จ" });
        }

        /// <summary>
        /// ส่งรายการอาหารเข้าห้องครัว (แบบยังไม่ชำระเงิน - Dine-in)
        /// </summary>
        [HttpPost("kitchen")]
        public async Task<IActionResult> SendToKitchen([FromBody] CreateOrderRequest dto)
        {
            // Similar to CreateOrder but Status = 'PENDING', PaymentStatus = 'UNPAID'
            var branchClaim = User.FindFirst("BranchId")?.Value;
            var userClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId) || !Guid.TryParse(userClaim, out var userId))
                return BadRequest(new { success = false, message = "ไม่พบข้อมูลผู้ใช้งานหรือสาขา" });

            if (dto.Cart == null || dto.Cart.Count == 0)
                return BadRequest(new { success = false, message = "ตะกร้าสินค้าว่าง" });

            var orderId = Guid.NewGuid();
            var orderNumber = $"KITCHEN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4]}";
            
            using var conn = await _sql.OpenConnectionAsync();
            using var tx = conn.BeginTransaction();
            try {
                decimal subtotal = 0m;
                foreach (var item in dto.Cart) subtotal += item.Price * item.Qty;
                var taxAmount = Math.Round(subtotal * (dto.TaxRate / 100m), 2);
                var totalAmount = subtotal + taxAmount;

                using var cmd = new SqlCommand(
                    @"INSERT INTO Orders(Id, BranchId, UserId, OrderNumber, OrderType, TableId, Status, Subtotal, TaxAmount, TaxRate, TotalAmount, PaymentStatus, CreatedAt, UpdatedAt)
                      VALUES(@Id, @BranchId, @UserId, @OrderNumber, @Type, @TableId, 'PENDING', @Subtotal, @Tax, @Rate, @Total, 'UNPAID', GETUTCDATE(), GETUTCDATE())", conn, tx);
                cmd.Parameters.AddWithValue("@Id", orderId);
                cmd.Parameters.AddWithValue("@BranchId", branchId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                cmd.Parameters.AddWithValue("@Type", dto.OrderType ?? "DINE_IN");
                cmd.Parameters.AddWithValue("@TableId", (object?)dto.TableId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                cmd.Parameters.AddWithValue("@Tax", taxAmount);
                cmd.Parameters.AddWithValue("@Rate", dto.TaxRate);
                cmd.Parameters.AddWithValue("@Total", totalAmount);
                await cmd.ExecuteNonQueryAsync();

                foreach (var item in dto.Cart) {
                    using var iCmd = new SqlCommand(
                        @"INSERT INTO OrderItems(Id, OrderId, ProductId, Qty, UnitPrice, LineTotal, Status, CreatedAt)
                          VALUES(NEWID(), @OrderId, @ProductId, @Qty, @Price, @Total, 'PENDING', GETUTCDATE())", conn, tx);
                    iCmd.Parameters.AddWithValue("@OrderId", orderId);
                    iCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    iCmd.Parameters.AddWithValue("@Qty", item.Qty);
                    iCmd.Parameters.AddWithValue("@Price", item.Price);
                    iCmd.Parameters.AddWithValue("@Total", item.Price * item.Qty);
                    await iCmd.ExecuteNonQueryAsync();
                }

                if (dto.TableId.HasValue) {
                    using var tCmd = new SqlCommand("UPDATE DiningTables SET Status = 'OCCUPIED' WHERE Id = @Id", conn, tx);
                    tCmd.Parameters.AddWithValue("@Id", dto.TableId.Value);
                    await tCmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return Ok(new { success = true, data = orderId, message = "ส่งรายการเข้าห้องครัวเรียบร้อย" });
            } catch (Exception ex) {
                tx.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Orders SET Status = 'CANCELLED' WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                return Ok(new { success = true, message = "ยกเลิกออเดอร์เรียบร้อย" });
            } catch (Exception ex) {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/promotion")]
        public IActionResult ApplyPromotion(Guid id, [FromBody] object dto)
        {
            return Ok(new { success = true, data = true, message = "สำเร็จ" });
        }

        [HttpGet("tables")]
        public IActionResult GetTables(Guid branchId)
        {
            return Ok(new { success = true, data = new object[] { }, message = "สำเร็จ" });
        }

        [HttpPatch("tables/{id}/clear")]
        public IActionResult ClearTable(Guid id)
        {
            return Ok(new { success = true, data = true, message = "สำเร็จ" });
        }
    }
}
