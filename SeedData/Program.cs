using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

// ตั้งค่า Connection String สำหรับเชื่อมต่อฐานข้อมูล SQL Server
var connStr = "Server=localhost;Database=PosDB;Integrated Security=True;TrustServerCertificate=True";

using var conn = new SqlConnection(connStr);
conn.Open();

// 1. ทำความสะอาดข้อมูลเดิมในฐานข้อมูล (ลบข้อมูลออกจากตารางต่างๆ)
Console.WriteLine("Cleaning up database...");
string[] tables = {
    "AuditLogs", "UserSessions", "Payments", "OrderItems", "Orders", "PurchaseOrderItems", "PurchaseOrders", 
    "StockMovements", "ProductStocks", "Products", "Categories", "Suppliers", 
    "DiningTables", "Customers", "Users", "Branches"
};

foreach (var table in tables)
{
    try {
        using var cmd = new SqlCommand($"DELETE FROM {table}", conn);
        cmd.ExecuteNonQuery();
        Console.WriteLine($"Deleted {table}");
    } catch (Exception ex) {
        Console.WriteLine($"Warning: Could not delete from {table}: {ex.Message}");
    }
}

var rand = new Random();

// 2. สร้างข้อมูลสาขา (Branches) จำนวน 20 สาขา
string[] branchNames = {
    "สาขาสยามพารากอน", "สาขาเซ็นทรัลเวิลด์", "สาขาเอ็มควอเทียร์", "สาขาไอคอนสยาม",
    "สาขาเมกาบางนา", "สาขาเซ็นทรัลลาดพร้าว", "สาขาเดอะมอลล์บางกะปิ", "สาขาแฟชั่นไอส์แลนด์",
    "สาขาเซ็นทรัลปิ่นเกล้า", "สาขาเซ็นทรัลพระราม 9", "สาขาฟิวเจอร์พาร์ครังสิต", "สาขาเซ็นทรัลแจ้งวัฒนะ",
    "สาขาเซ็นทรัลรัตนาธิเบศร์", "สาขาเซ็นทรัลเวสต์เกต", "สาขาเซ็นทรัลอีสต์วิลล์", "สาขาเซ็นทรัลพระราม 2",
    "สาขาเซ็นทรัลพระราม 3", "สาขาซีคอนสแควร์", "สาขาพาราไดซ์พาร์ค", "สาขาเทอร์มินอล 21"
};

var branchIds = new List<Guid>();
foreach (var name in branchNames)
{
    var id = Guid.NewGuid();
    using var cmd = new SqlCommand("INSERT INTO Branches (Id, Name, IsActive, CreatedAt) VALUES (@Id, @Name, 1, GETUTCDATE())", conn);
    cmd.Parameters.AddWithValue("@Id", id);
    cmd.Parameters.AddWithValue("@Name", name);
    cmd.ExecuteNonQuery();
    branchIds.Add(id);
}
Console.WriteLine($"Seeded {branchIds.Count} Branches.");

// 3. สร้างข้อมูลผู้ใช้งาน (Users) จำนวน 20 คน (1 คนต่อ 1 สาขา)
string[] firstNames = { "สมชาย", "สมหญิง", "สมศักดิ์", "สมศรี", "วิชัย", "วิภา", "มานะ", "มานี", "ปิติ", "ชูใจ", "อดิศร", "นงลักษณ์", "เกียรติ", "สุวรรณ", "ประเสริฐ", "วรัญญา", "พิชิต", "เบญจมาศ", "ธนพล", "อัญชลี" };
string[] lastNames = { "ใจดี", "มีสุข", "รักชาติ", "มั่นคง", "เพิ่มพูน", "รุ่งเรือง", "แสงทอง", "งามวิไล", "ยอดเยี่ยม", "เพียรพยายาม", "สุวรรณมณี", "ภักดี", "ศรีสุข", "รักษ์ไทย", "วงศ์กำแหง", "บุญส่ง", "วัฒนา", "อริยวงศ์", "โชคดี", "มณีรัตน์" };

var userIds = new List<Guid>();
for (int i = 0; i < 20; i++)
{
    var id = Guid.NewGuid();
    var fullName = $"{firstNames[i]} {lastNames[i]}";
    var username = $"user{i + 1:D2}";
    var role = i == 0 ? "IT_ADMIN" : (i < 5 ? "MANAGER" : "CASHIER");
    
    using var cmd = new SqlCommand(@"INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Role, Status, CreatedAt, UpdatedAt) 
                                     VALUES (@Id, @BranchId, @User, @Pass, @Full, @Role, 'ACTIVE', GETUTCDATE(), GETUTCDATE())", conn);
    cmd.Parameters.AddWithValue("@Id", id);
    cmd.Parameters.AddWithValue("@BranchId", branchIds[i]);
    cmd.Parameters.AddWithValue("@User", username);
    cmd.Parameters.AddWithValue("@Pass", BCrypt.Net.BCrypt.HashPassword("password123"));
    cmd.Parameters.AddWithValue("@Full", fullName);
    cmd.Parameters.AddWithValue("@Role", role);
    cmd.ExecuteNonQuery();
    userIds.Add(id);
}
Console.WriteLine($"Seeded {userIds.Count} Users.");

// 4. สร้างข้อมูลหมวดหมู่สินค้า (Categories) สำหรับแต่ละสาขา
string[] catNames = { "อาหารคาว", "ของหวาน", "เครื่องดื่มร้อน", "เครื่องดื่มเย็น", "ของกินเล่น" };
var catIdsByBranch = new Dictionary<Guid, List<Guid>>();

foreach (var bId in branchIds)
{
    var branchCats = new List<Guid>();
    for (int i = 0; i < catNames.Length; i++)
    {
        var id = Guid.NewGuid();
        using var cmd = new SqlCommand("INSERT INTO Categories (Id, BranchId, Name, DisplayOrder, IsActive, CreatedAt) VALUES (@Id, @BId, @Name, @Order, 1, GETUTCDATE())", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@BId", bId);
        cmd.Parameters.AddWithValue("@Name", catNames[i]);
        cmd.Parameters.AddWithValue("@Order", i);
        cmd.ExecuteNonQuery();
        branchCats.Add(id);
    }
    catIdsByBranch[bId] = branchCats;
}
Console.WriteLine("Seeded Categories for all branches.");

// 5. สร้างข้อมูลสินค้า (Products) สำหรับแต่ละสาขา
var productData = new[] {
    (Name: "ข้าวผัดกะเพราไข่ดาว", Price: 65m, Cost: 25m, CatIdx: 0),
    (Name: "ข้าวผัดปู", Price: 80m, Cost: 35m, CatIdx: 0),
    (Name: "ต้มยำกุ้ง", Price: 150m, Cost: 70m, CatIdx: 0),
    (Name: "ผัดไทยกุ้งสด", Price: 90m, Cost: 40m, CatIdx: 0),
    (Name: "บัวลอยไข่หวาน", Price: 45m, Cost: 15m, CatIdx: 1),
    (Name: "ข้าวเหนียวมะม่วง", Price: 120m, Cost: 50m, CatIdx: 1),
    (Name: "เอสเพรสโซ่ร้อน", Price: 55m, Cost: 12m, CatIdx: 2),
    (Name: "ชาไทยเย็น", Price: 45m, Cost: 10m, CatIdx: 3),
    (Name: "กาแฟลาเต้เย็น", Price: 65m, Cost: 15m, CatIdx: 3),
    (Name: "ลูกชิ้นปิ้ง", Price: 40m, Cost: 15m, CatIdx: 4)
};

foreach (var bId in branchIds)
{
    var cats = catIdsByBranch[bId];
    foreach (var p in productData)
    {
        var pId = Guid.NewGuid();
        using var cmd = new SqlCommand(@"INSERT INTO Products (Id, BranchId, CategoryId, Sku, Name, Price, CostPrice, IsActive, CreatedAt, UpdatedAt) 
                                         VALUES (@Id, @BId, @CId, @Sku, @Name, @Price, @Cost, 1, GETUTCDATE(), GETUTCDATE())", conn);
        cmd.Parameters.AddWithValue("@Id", pId);
        cmd.Parameters.AddWithValue("@BId", bId);
        cmd.Parameters.AddWithValue("@CId", cats[p.CatIdx]);
        cmd.Parameters.AddWithValue("@Sku", $"SKU-{bId.ToString()[..4]}-{p.Name.GetHashCode() % 10000:D4}-{rand.Next(10,99)}");
        cmd.Parameters.AddWithValue("@Name", p.Name);
        cmd.Parameters.AddWithValue("@Price", p.Price);
        cmd.Parameters.AddWithValue("@Cost", p.Cost);
        cmd.ExecuteNonQuery();

        // สร้างข้อมูลสต็อกสินค้า (ProductStocks)
        using var scmd = new SqlCommand(@"INSERT INTO ProductStocks (Id, ProductId, BranchId, PhysicalQty, ReservedQty, CommittedQty, MinAlertQty, UpdatedAt) 
                                          VALUES (NEWID(), @PId, @BId, @Qty, 0, 0, 5, GETUTCDATE())", conn);
        scmd.Parameters.AddWithValue("@PId", pId);
        scmd.Parameters.AddWithValue("@BId", bId);
        scmd.Parameters.AddWithValue("@Qty", rand.Next(20, 100));
        scmd.ExecuteNonQuery();
    }
}
Console.WriteLine("Seeded Products for all branches.");

// 6. สร้างข้อมูลผู้จำหน่าย (Suppliers) จำนวน 5 ราย ต่อสาขา
string[] supplierNames = { "ซีพี ออลล์", "เบทาโกร", "ไทยเบฟ", "สหพัฒนพิบูล", "เนสท์เล่ ไทย" };
var supplierIdsByBranch = new Dictionary<Guid, List<Guid>>();
foreach (var bId in branchIds)
{
    var sList = new List<Guid>();
    foreach (var sName in supplierNames)
    {
        var sId = Guid.NewGuid();
        using var cmd = new SqlCommand("INSERT INTO Suppliers (Id, BranchId, Name, IsActive, CreatedAt) VALUES (@Id, @BId, @Name, 1, GETUTCDATE())", conn);
        cmd.Parameters.AddWithValue("@Id", sId);
        cmd.Parameters.AddWithValue("@BId", bId);
        cmd.Parameters.AddWithValue("@Name", sName);
        cmd.ExecuteNonQuery();
        sList.Add(sId);
    }
    supplierIdsByBranch[bId] = sList;
}
Console.WriteLine("Seeded Suppliers.");

// 7. สร้างข้อมูลคำสั่งซื้อ (Orders) จำนวน 10 รายการ ต่อสาขา
foreach (var bId in branchIds)
{
    var uId = userIds[branchIds.IndexOf(bId)];
    var products = new List<(Guid Id, decimal Price, decimal Cost)>();
    using (var pc = new SqlCommand("SELECT Id, Price, CostPrice FROM Products WHERE BranchId = @BId", conn)) {
        pc.Parameters.AddWithValue("@BId", bId);
        using var reader = pc.ExecuteReader();
        while(reader.Read()) products.Add((reader.GetGuid(0), reader.GetDecimal(1), reader.GetDecimal(2)));
    }

    for (int i = 0; i < 10; i++)
    {
        var oId = Guid.NewGuid();
        var oNo = $"ORD-{bId.ToString()[..4]}-{DateTime.UtcNow:yyyyMMdd}-{i:D3}";
        var at = DateTime.UtcNow.AddDays(-rand.Next(0, 7)).AddHours(-rand.Next(0, 24));
        
        using var cmd = new SqlCommand(@"INSERT INTO Orders (Id, BranchId, UserId, OrderNumber, OrderType, Status, Subtotal, TaxAmount, TaxRate, TotalAmount, PaymentStatus, CreatedAt, UpdatedAt) 
                                         VALUES (@Id, @BId, @UId, @No, 'TAKEAWAY', 'COMPLETED', 0, 0, 7, 0, 'PAID', @At, @At)", conn);
        cmd.Parameters.AddWithValue("@Id", oId);
        cmd.Parameters.AddWithValue("@BId", bId);
        cmd.Parameters.AddWithValue("@UId", uId);
        cmd.Parameters.AddWithValue("@No", oNo);
        cmd.Parameters.AddWithValue("@At", at);
        cmd.ExecuteNonQuery();

        decimal subtotal = 0;
        int items = rand.Next(1, 4);
        for (int j = 0; j < items; j++)
        {
            var p = products[rand.Next(products.Count)];
            var qty = rand.Next(1, 3);
            var lineTotal = p.Price * qty;
            subtotal += lineTotal;
            using var icmd = new SqlCommand(@"INSERT INTO OrderItems (Id, OrderId, ProductId, Qty, UnitPrice, CostPrice, LineTotal, Status, SentToKitchenAt) 
                                              VALUES (NEWID(), @OId, @PId, @Qty, @Price, @Cost, @Total, 'SERVED', @At)", conn);
            icmd.Parameters.AddWithValue("@OId", oId);
            icmd.Parameters.AddWithValue("@PId", p.Id);
            icmd.Parameters.AddWithValue("@Qty", qty);
            icmd.Parameters.AddWithValue("@Price", p.Price);
            icmd.Parameters.AddWithValue("@Cost", p.Cost);
            icmd.Parameters.AddWithValue("@Total", lineTotal);
            icmd.Parameters.AddWithValue("@At", at);
            icmd.ExecuteNonQuery();
        }
        var tax = Math.Round(subtotal * 0.07m, 2);
        var total = subtotal + tax;
        using var ucmd = new SqlCommand("UPDATE Orders SET Subtotal=@S, TaxAmount=@T, TotalAmount=@Tot WHERE Id=@Id", conn);
        ucmd.Parameters.AddWithValue("@S", subtotal);
        ucmd.Parameters.AddWithValue("@T", tax);
        ucmd.Parameters.AddWithValue("@Tot", total);
        ucmd.Parameters.AddWithValue("@Id", oId);
        ucmd.ExecuteNonQuery();

        // สร้างข้อมูลการชำระเงิน (Payments)
        using var pcmd = new SqlCommand("INSERT INTO Payments (Id, OrderId, UserId, PaymentMethod, Amount, Status, PaidAt) VALUES (NEWID(), @OId, @UId, 'CASH', @Amt, 'SUCCESS', @At)", conn);
        pcmd.Parameters.AddWithValue("@OId", oId);
        pcmd.Parameters.AddWithValue("@UId", uId);
        pcmd.Parameters.AddWithValue("@Amt", total);
        pcmd.Parameters.AddWithValue("@At", at);
        pcmd.ExecuteNonQuery();
    }
}
Console.WriteLine("Seeded Orders for all branches.");

Console.WriteLine("DATABASE SEEDING COMPLETED SUCCESSFULLY.");
