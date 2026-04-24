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
    public class ProductsController : Controller
    {
        private readonly ISqlHelper _sql;
        public ProductsController(ISqlHelper sql) { _sql = sql; }

        [HttpGet]
        public async Task<IActionResult> Index(bool openCreate = false)
        {
            ViewData["ActiveMenu"] = "products";
            ViewData["PageTitle"] = "จัดการสินค้าหน้าร้าน";
            ViewData["TopIcon"] = "shopping-cart";
            ViewData["OpenCreate"] = openCreate;

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId))
            {
                return View(new List<ProductRow>());
            }

            var products = await _sql.QueryAsync<ProductRow>(
                @"SELECT p.Id, p.Name, p.Sku, p.Price, p.CostPrice, p.Unit, p.ProductType, p.IsActive, c.Name AS CategoryName
                  FROM Products p
                  LEFT JOIN Categories c ON p.CategoryId = c.Id
                  WHERE p.BranchId = @BranchId
                  ORDER BY p.Name",
                new[] { new SqlParameter("@BranchId", branchId) });

            var categories = await _sql.QueryAsync<CategoryOption>(
                @"SELECT Id, Name FROM Categories WHERE BranchId = @BranchId AND IsActive = 1 ORDER BY DisplayOrder, Name",
                new[] { new SqlParameter("@BranchId", branchId) });

            ViewData["Categories"] = categories;
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            ViewData["ActiveMenu"] = "products";
            ViewData["PageTitle"] = "จัดการหมวดหมู่สินค้า";
            ViewData["TopIcon"] = "layers";

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId)) return View(new List<CategoryRow>());

            var categories = await _sql.QueryAsync<CategoryRow>(
                "SELECT Id, Name, NameEn, DisplayOrder, IsActive FROM Categories WHERE BranchId = @BranchId ORDER BY DisplayOrder, Name",
                new[] { new SqlParameter("@BranchId", branchId) });

            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(string name, int displayOrder)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "ข้อมูลไม่ถูกต้อง";
                return RedirectToAction(nameof(Categories));
            }

            try {
                await _sql.ExecuteAsync(
                    "INSERT INTO Categories (Id, BranchId, Name, DisplayOrder, IsActive, CreatedAt, UpdatedAt) VALUES (@Id, @BranchId, @Name, @Order, 1, GETUTCDATE(), GETUTCDATE())",
                    new[] {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@Name", name.Trim()),
                        new SqlParameter("@Order", displayOrder)
                    });
                TempData["Success"] = "เพิ่มหมวดหมู่สำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try {
                await _sql.ExecuteAsync("DELETE FROM Categories WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ลบหมวดหมู่สำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ลบไม่สำเร็จ: " + ex.Message;
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try {
                await _sql.ExecuteAsync("UPDATE Products SET IsActive = 0 WHERE Id = @Id", new[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "ปิดการใช้งานสินค้าสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductInput input)
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId))
            {
                TempData["Error"] = "ไม่พบข้อมูลสาขาของผู้ใช้ปัจจุบัน";
                return RedirectToAction(nameof(Index));
            }
            if (!Guid.TryParse(input.CategoryId, out var categoryId) || string.IsNullOrWhiteSpace(input.Name))
            {
                TempData["Error"] = "กรุณากรอกข้อมูลสินค้าให้ครบ";
                return RedirectToAction(nameof(Index));
            }

            var newProductId = Guid.NewGuid();
            var sku = string.IsNullOrWhiteSpace(input.Sku) ? $"SKU-{DateTime.UtcNow:yyyyMMddHHmmssfff}" : input.Sku.Trim();
            try
            {
                await _sql.ExecuteAsync(
                    @"INSERT INTO Products (Id, BranchId, CategoryId, Sku, Barcode, Name, Price, CostPrice, Unit, ProductType, IsActive, Taxable, CreatedAt, UpdatedAt)
                      VALUES (@Id, @BranchId, @CategoryId, @Sku, @Barcode, @Name, @Price, @CostPrice, @Unit, @ProductType, @IsActive, 1, GETUTCDATE(), GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", newProductId),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@CategoryId", categoryId),
                        new SqlParameter("@Sku", sku),
                        new SqlParameter("@Barcode", string.IsNullOrWhiteSpace(input.Barcode) ? DBNull.Value : input.Barcode.Trim()),
                        new SqlParameter("@Name", input.Name.Trim()),
                        new SqlParameter("@Price", input.Price),
                        new SqlParameter("@CostPrice", input.CostPrice),
                        new SqlParameter("@Unit", string.IsNullOrWhiteSpace(input.Unit) ? "ชิ้น" : input.Unit.Trim()),
                        new SqlParameter("@ProductType", string.IsNullOrWhiteSpace(input.ProductType) ? "NON_TRACKABLE" : input.ProductType.Trim()),
                        new SqlParameter("@IsActive", input.IsActive)
                    });

                await _sql.ExecuteAsync(
                    @"INSERT INTO ProductStocks (Id, ProductId, BranchId, PhysicalQty, ReservedQty, CommittedQty, MinAlertQty, UpdatedAt)
                      VALUES (@Id, @ProductId, @BranchId, @PhysicalQty, 0, 0, @MinAlertQty, GETUTCDATE())",
                    new[]
                    {
                        new SqlParameter("@Id", Guid.NewGuid()),
                        new SqlParameter("@ProductId", newProductId),
                        new SqlParameter("@BranchId", branchId),
                        new SqlParameter("@PhysicalQty", Math.Max(0, input.InitialQty)),
                        new SqlParameter("@MinAlertQty", Math.Max(0, input.MinAlertQty))
                    });
                TempData["Success"] = "เพิ่มสินค้าเรียบร้อยแล้ว";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"เพิ่มสินค้าไม่สำเร็จ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["ActiveMenu"] = "products";
            ViewData["PageTitle"] = "แก้ไขข้อมูลสินค้า";
            ViewData["TopIcon"] = "edit";

            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!Guid.TryParse(branchClaim, out var branchId)) return RedirectToAction(nameof(Index));

            var product = await _sql.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM Products WHERE Id = @Id AND BranchId = @BranchId",
                new[] { new SqlParameter("@Id", id), new SqlParameter("@BranchId", branchId) });

            if (product == null) return NotFound();

            var categories = await _sql.QueryAsync<CategoryOption>(
                @"SELECT Id, Name FROM Categories WHERE BranchId = @BranchId AND IsActive = 1 ORDER BY DisplayOrder, Name",
                new[] { new SqlParameter("@BranchId", branchId) });

            ViewData["Categories"] = categories;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, CreateProductInput input)
        {
            try {
                await _sql.ExecuteAsync(
                    @"UPDATE Products SET 
                        CategoryId=@CatId, Name=@Name, Sku=@Sku, Barcode=@Barcode, 
                        Price=@Price, CostPrice=@Cost, Unit=@Unit, IsActive=@Active, 
                        UpdatedAt=GETUTCDATE() 
                      WHERE Id=@Id",
                    new[] {
                        new SqlParameter("@Id", id),
                        new SqlParameter("@CatId", Guid.Parse(input.CategoryId)),
                        new SqlParameter("@Name", input.Name.Trim()),
                        new SqlParameter("@Sku", input.Sku?.Trim() ?? ""),
                        new SqlParameter("@Barcode", input.Barcode?.Trim() ?? ""),
                        new SqlParameter("@Price", input.Price),
                        new SqlParameter("@Cost", input.CostPrice),
                        new SqlParameter("@Unit", input.Unit?.Trim() ?? "ชิ้น"),
                        new SqlParameter("@Active", input.IsActive)
                    });
                TempData["Success"] = "แก้ไขข้อมูลสินค้าสำเร็จ";
            } catch (Exception ex) {
                TempData["Error"] = "ผิดพลาด: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
        public class ProductRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public decimal Price { get; set; }
            public decimal CostPrice { get; set; }
            public string Unit { get; set; } = "";
            public string ProductType { get; set; } = "";
            public bool IsActive { get; set; }
            public string? CategoryName { get; set; }
        }


        public class CategoryRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public string? NameEn { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsActive { get; set; }
        }

        public class CategoryOption
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class CreateProductInput
        {
            public string CategoryId { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Sku { get; set; }
            public string? Barcode { get; set; }
            public decimal Price { get; set; }
            public decimal CostPrice { get; set; }
            public string? Unit { get; set; }
            public string? ProductType { get; set; }
            public bool IsActive { get; set; } = true;
            public int InitialQty { get; set; }
            public int MinAlertQty { get; set; } = 5;
        }
    }
}
