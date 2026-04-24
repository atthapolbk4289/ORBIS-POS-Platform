using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using PosSystem.Models.ViewModels;
using PosSystem.Models.ViewModels.PosSystem.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์สำหรับจัดการสต็อกสินค้าและสินค้าคงคลัง
    /// </summary>
    [Authorize(Policy = "StockAccess")]
    public class StockController : Controller
    {
        private readonly ISqlHelper _sql;
        public StockController(ISqlHelper sql) { _sql = sql; }

        /// <summary>
        /// ดึง ID ของสาขาจาก Claim ของผู้ใช้งานปัจจุบัน
        /// </summary>
        private Guid GetBranchId() {
            var claim = User.FindFirst("BranchId")?.Value;
            return string.IsNullOrEmpty(claim) ? Guid.Empty : Guid.Parse(claim);
        }

        /// <summary>
        /// แสดงหน้าจัดการสินค้าคงคลัง พร้อมระบบค้นหาและกรองสินค้าสต็อกต่ำ
        /// </summary>
        [HttpGet]
        public IActionResult Index(Guid? branchIdParam, string? search, bool lowStockOnly = false, string? statusFilter = null, string? sortBy = null)
        {
            var branchId = branchIdParam ?? GetBranchId();
            ViewData["ActiveMenu"] = "stock-inventory";
            ViewData["PageTitle"] = "สินค้าคงคลัง";
            ViewData["TopIcon"] = "package";

            // คำสั่ง SQL สำหรับดึงข้อมูลสินค้าและสถานะสต็อก
            var sql = @"SELECT 
                    p.Id as ProductId, p.Name as ProductName, p.Sku,
                    ps.PhysicalQty, ps.ReservedQty, ps.PhysicalQty - ps.ReservedQty as AvailableQty,
                    p.CostPrice, (ps.PhysicalQty * p.CostPrice) as StockValue,
                    CAST(CASE WHEN ps.PhysicalQty - ps.ReservedQty <= ps.MinAlertQty THEN 1 ELSE 0 END AS BIT) as IsLowStock
                FROM ProductStocks ps
                JOIN Products p ON ps.ProductId = p.Id
                WHERE ps.BranchId = @BranchId";

            var parameters = new List<Microsoft.Data.SqlClient.SqlParameter> {
                new Microsoft.Data.SqlClient.SqlParameter("@BranchId", branchId)
            };

            // กรองตามคำค้นหา
            if (!string.IsNullOrEmpty(search)) {
                sql += " AND (p.Name LIKE '%' + @Search + '%' OR p.Sku LIKE '%' + @Search + '%')";
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Search", search));
            }

            // กรองเฉพาะสินค้าที่สต็อกต่ำ
            if (lowStockOnly)
                sql += " AND (ps.PhysicalQty - ps.ReservedQty <= ps.MinAlertQty)";

            if (!string.IsNullOrEmpty(statusFilter)) {
                if (statusFilter == "IN_STOCK") sql += " AND (ps.PhysicalQty - ps.ReservedQty > 0)";
                else if (statusFilter == "OUT_OF_STOCK") sql += " AND (ps.PhysicalQty - ps.ReservedQty <= 0)";
                // LOW_STOCK is already handled or can be handled as well:
                else if (statusFilter == "LOW_STOCK") sql += " AND (ps.PhysicalQty - ps.ReservedQty <= ps.MinAlertQty AND ps.PhysicalQty - ps.ReservedQty > 0)";
            }

            // จัดเรียง
            if (!string.IsNullOrEmpty(sortBy)) {
                sql += sortBy switch {
                    "COST_ASC" => " ORDER BY p.CostPrice ASC",
                    "COST_DESC" => " ORDER BY p.CostPrice DESC",
                    "QTY_ASC" => " ORDER BY (ps.PhysicalQty - ps.ReservedQty) ASC",
                    "QTY_DESC" => " ORDER BY (ps.PhysicalQty - ps.ReservedQty) DESC",
                    "VALUE_DESC" => " ORDER BY (ps.PhysicalQty * p.CostPrice) DESC",
                    _ => " ORDER BY p.Name ASC"
                };
            } else {
                sql += " ORDER BY p.Name ASC";
            }

            var sqlHelper = HttpContext.RequestServices.GetService(typeof(PosSystem.Helpers.ISqlHelper)) as PosSystem.Helpers.ISqlHelper;
            var items = sqlHelper != null ? sqlHelper.QueryAsync<StockItemData>(sql, parameters.ToArray()).Result : new List<StockItemData>();

            // เตรียมข้อมูลสำหรับ ViewModel
            var vm = new StockIndexViewModel {
                Search = search,
                LowStockOnly = lowStockOnly,
                StatusFilter = statusFilter,
                SortBy = sortBy,
                TotalStockValue = items.Sum(x => x.StockValue),
                LowStockCount = items.Count(x => x.IsLowStock),
                OutOfStockCount = items.Count(x => x.AvailableQty <= 0),
                StockItems = items
            };
            return View(vm);
        }

        /// <summary>
        /// แสดงรายการใบขอสั่งซื้อ (Purchase Orders)
        /// </summary>
        [HttpGet]
        public IActionResult PurchaseOrders(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "stock-po";
            ViewData["PageTitle"] = "ใบขอสั่งซื้อ";
            ViewData["TopIcon"] = "file-text";
            ViewData["OpenCreate"] = openCreate;
            var branchId = GetBranchId();

            // ดึงรายการใบสั่งซื้อ
            var items = _sql.QueryAsync<PurchaseOrderRow>(
                @"SELECT po.Id, po.PoNumber, po.Status, po.TotalAmount, po.CreatedAt,
                         ISNULL(s.Name, N'-') AS SupplierName
                  FROM PurchaseOrders po
                  LEFT JOIN Suppliers s ON s.Id = po.SupplierId
                  WHERE po.BranchId = @BranchId
                  ORDER BY po.CreatedAt DESC",
                new[] { new SqlParameter("@BranchId", branchId) }).Result;

            // ดึงรายชื่อ Supplier สำหรับเลือกตอนสร้างใบสั่งซื้อใหม่
            var suppliers = _sql.QueryAsync<SupplierOption>(
                @"SELECT Id, Name FROM Suppliers WHERE BranchId = @BranchId AND IsActive = 1 ORDER BY Name",
                new[] { new SqlParameter("@BranchId", branchId) }).Result;

            ViewData["Suppliers"] = suppliers;
            return View(items);
        }

        /// <summary>
        /// สร้างใบขอสั่งซื้อใหม่
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder(CreatePurchaseOrderInput input)
        {
            var branchId = GetBranchId();
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งานปัจจุบัน";
                return RedirectToAction(nameof(PurchaseOrders));
            }

            var poNumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";
            Guid? supplierId = Guid.TryParse(input.SupplierId, out var parsedSupplierId) ? parsedSupplierId : null;
            try
            {
                // บันทึกใบสั่งซื้อลงฐานข้อมูล
                await _sql.ExecuteAsync(
                    @"INSERT INTO PurchaseOrders (Id, BranchId, SupplierId, UserId, PoNumber, Status, TotalAmount, Note, OrderedAt, CreatedAt, UpdatedAt)
                      VALUES (@Id, @BranchId, @SupplierId, @UserId, @PoNumber, 'DRAFT', 0, @Note, NULL, GETUTCDATE(), GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@SupplierId", (object?)supplierId ?? DBNull.Value),
                        new SqlParameter("@UserId", userId),
                        new SqlParameter("@PoNumber", poNumber),
                        new SqlParameter("@Note", string.IsNullOrWhiteSpace(input.Note) ? DBNull.Value : input.Note.Trim())
                    });
                TempData["Success"] = $"สร้างใบสั่งซื้อสำเร็จ ({poNumber})";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"สร้างใบสั่งซื้อไม่สำเร็จ: {ex.Message}";
            }

            return RedirectToAction(nameof(PurchaseOrders));
        }

        /// <summary>
        /// แสดงหน้าการรับสินค้า (Stock In / Receive)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Receive()
        {
            var branchId = GetBranchId();
            ViewData["ActiveMenu"] = "stock-receive";
            ViewData["PageTitle"] = "รับสินค้าเข้าสต็อก";
            ViewData["TopIcon"] = "package-plus";

            // ดึงรายการสินค้าทั้งหมดที่เปิดใช้งานอยู่
            var products = await _sql.QueryAsync<ProductDropdownDto>(
                "SELECT Id, Name, Sku FROM Products WHERE BranchId=@BId AND IsActive=1 ORDER BY Name",
                new[] { new SqlParameter("@BId", branchId) });

            ViewData["Products"] = products;
            return View();
        }


        /// <summary>
        /// ประมวลผลการรับสินค้า
        /// </summary>
        [HttpPost]
        public IActionResult ReceiveStock(ReceiveStockDto dto) => Ok(new { success = true });

        /// <summary>
        /// แสดงหน้าแจ้งทำลายสินค้าหรือสินค้าชำรุด (Damage)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Damage()
        {
            var branchId = GetBranchId();
            ViewData["ActiveMenu"] = "stock-damage";
            ViewData["PageTitle"] = "แจ้งทำลายสินค้า";
            ViewData["TopIcon"] = "trash-2";

            var products = await _sql.QueryAsync<ProductDropdownDto>(
                @"SELECT p.Id, p.Name, p.Sku, ps.PhysicalQty 
                  FROM Products p 
                  JOIN ProductStocks ps ON p.Id = ps.ProductId
                  WHERE p.BranchId=@BId AND p.IsActive=1 AND ps.PhysicalQty > 0
                  ORDER BY p.Name",
                new[] { new SqlParameter("@BId", branchId) });

            ViewData["Products"] = products;
            return View();
        }


        /// <summary>
        /// แสดงหน้าตรวจนับสต็อกสินค้า (Stock Counting)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var branchId = GetBranchId();
            ViewData["ActiveMenu"] = "stock-count";
            ViewData["PageTitle"] = "ตรวจนับสต็อก";
            ViewData["TopIcon"] = "clipboard-check";

            var products = await _sql.QueryAsync<ProductDropdownDto>(
                @"SELECT p.Id, p.Name, p.Sku, ps.PhysicalQty 
                  FROM Products p 
                  JOIN ProductStocks ps ON p.Id = ps.ProductId
                  WHERE p.BranchId=@BId AND p.IsActive=1
                  ORDER BY p.Name",
                new[] { new SqlParameter("@BId", branchId) });

            ViewData["Products"] = products;
            return View();
        }


        /// <summary>
        /// รายการแจ้งเตือนสินค้าใกล้หมด
        /// </summary>
        [HttpGet]
        public IActionResult Alerts()
        {
            ViewData["PageTitle"] = "สินค้าใกล้หมด";
            return RedirectToAction("Index", new { lowStockOnly = true });
        }

        /// <summary>
        /// ปรับสต็อกสินค้าด้วยตนเอง (สำหรับแอดมินหรือผู้จัดการ)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "IT_ADMIN,MANAGER")]
        public IActionResult AdjustStock(AdjustStockDto dto) => Ok(new { success = true });

        /// <summary>
        /// ดูประวัติความเคลื่อนไหวของสต็อกสินค้า
        /// </summary>
        [HttpGet]
        public IActionResult Movements(Guid productId, DateTime? from, DateTime? to)
        {
            ViewData["PageTitle"] = "ความเคลื่อนไหวสต็อก";
            return View();
        }

        public class PurchaseOrderRow
        {
            public Guid Id { get; set; }
            public string PoNumber { get; set; } = "";
            public string SupplierName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = "";
        }

        public class SupplierOption
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class CreatePurchaseOrderInput
        {
            public string? SupplierId { get; set; }
            public string? Note { get; set; }
        }
    }
}

