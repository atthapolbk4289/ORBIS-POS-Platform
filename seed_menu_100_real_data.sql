USE PosDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
GO

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @TargetPerMenu INT = 100;

-- Ensure base branch exists
IF NOT EXISTS (SELECT 1 FROM Branches)
BEGIN
    INSERT INTO Branches (Id, Name, Address, Phone, TaxId, IsActive, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'Main Branch', '123 Sukhumvit Road, Bangkok', '02-000-0000', '1234567890123', 1, @Now, @Now);
END

DECLARE @PrimaryBranchId UNIQUEIDENTIFIER =
(
    SELECT TOP 1 Id FROM Branches ORDER BY CreatedAt
);

-- 1) Branches: top up to 100
DECLARE @BranchCount INT = (SELECT COUNT(*) FROM Branches);
WHILE @BranchCount < @TargetPerMenu
BEGIN
    DECLARE @BranchSeq INT = @BranchCount + 1;
    INSERT INTO Branches (Id, Name, Address, Phone, TaxId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (
        NEWID(),
        'Branch ' + RIGHT('000' + CAST(@BranchSeq AS NVARCHAR(3)), 3),
        'Commercial Building ' + CAST(@BranchSeq AS NVARCHAR(10)) + ', Business District, Bangkok',
        '02' + RIGHT('0000000' + CAST(7000000 + @BranchSeq AS NVARCHAR(10)), 7),
        RIGHT('0000000000000' + CAST(1000000000000 + @BranchSeq AS NVARCHAR(20)), 13),
        1,
        DATEADD(DAY, -@BranchSeq, @Now),
        @Now
    );
    SET @BranchCount += 1;
END

-- Ensure category in primary branch
IF NOT EXISTS (SELECT 1 FROM Categories WHERE BranchId = @PrimaryBranchId)
BEGIN
    INSERT INTO Categories (Id, BranchId, Name, Type, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES
      (NEWID(), @PrimaryBranchId, 'Food', 'FOOD', 1, 1, @Now, @Now),
      (NEWID(), @PrimaryBranchId, 'Beverage', 'BEVERAGE', 2, 1, @Now, @Now),
      (NEWID(), @PrimaryBranchId, 'Dessert', 'DESSERT', 3, 1, @Now, @Now),
      (NEWID(), @PrimaryBranchId, 'Retail', 'RETAIL', 4, 1, @Now, @Now);
END

-- 2) Users: top up to 100
DECLARE @UserCount INT = (SELECT COUNT(*) FROM Users);
WHILE @UserCount < @TargetPerMenu
BEGIN
    DECLARE @UserSeq INT = @UserCount + 1;
    DECLARE @Role NVARCHAR(20) =
        CASE WHEN @UserSeq % 10 = 0 THEN 'MANAGER'
             WHEN @UserSeq % 6 = 0 THEN 'STOCK_KEEPER'
             ELSE 'CASHIER' END;

    INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Phone, Email, Role, Status, Pin, CreatedAt, UpdatedAt)
    VALUES
    (
        NEWID(),
        @PrimaryBranchId,
        'user' + RIGHT('000' + CAST(@UserSeq AS NVARCHAR(3)), 3),
        'HASH_USER@1234',
        'Staff ' + CAST(@UserSeq AS NVARCHAR(10)),
        '08' + RIGHT('00000000' + CAST(10000000 + @UserSeq AS NVARCHAR(10)), 8),
        'user' + CAST(@UserSeq AS NVARCHAR(10)) + '@orbis.local',
        @Role,
        'ACTIVE',
        'HASH_123456',
        DATEADD(DAY, -@UserSeq, @Now),
        @Now
    );
    SET @UserCount += 1;
END

-- 3) Customers: top up to 100 (per primary branch)
DECLARE @CustomerCount INT = (SELECT COUNT(*) FROM Customers WHERE BranchId = @PrimaryBranchId);
WHILE @CustomerCount < @TargetPerMenu
BEGIN
    DECLARE @CustomerSeq INT = @CustomerCount + 1;
    INSERT INTO Customers (Id, BranchId, Name, Phone, Email, Points, TotalSpent, MemberLevel, IsActive, CreatedAt)
    VALUES
    (
        NEWID(),
        @PrimaryBranchId,
        'Customer ' + CAST(@CustomerSeq AS NVARCHAR(10)),
        '09' + RIGHT('00000000' + CAST(10000000 + @CustomerSeq AS NVARCHAR(10)), 8),
        'customer' + CAST(@CustomerSeq AS NVARCHAR(10)) + '@mail.com',
        @CustomerSeq * 10,
        CAST(@CustomerSeq * 250 AS DECIMAL(12,2)),
        CASE WHEN @CustomerSeq % 10 = 0 THEN 'PLATINUM'
             WHEN @CustomerSeq % 6 = 0 THEN 'GOLD'
             WHEN @CustomerSeq % 3 = 0 THEN 'SILVER'
             ELSE 'NORMAL' END,
        1,
        DATEADD(DAY, -@CustomerSeq, @Now)
    );
    SET @CustomerCount += 1;
END

-- 4) Suppliers: top up to 100 (per primary branch)
DECLARE @SupplierCount INT = (SELECT COUNT(*) FROM Suppliers WHERE BranchId = @PrimaryBranchId);
WHILE @SupplierCount < @TargetPerMenu
BEGIN
    DECLARE @SupplierSeq INT = @SupplierCount + 1;
    INSERT INTO Suppliers (Id, BranchId, Name, ContactName, Phone, Email, Address, TaxId, IsActive, CreatedAt)
    VALUES
    (
        NEWID(),
        @PrimaryBranchId,
        'Supplier Co. ' + CAST(@SupplierSeq AS NVARCHAR(10)),
        'Contact ' + CAST(@SupplierSeq AS NVARCHAR(10)),
        '02' + RIGHT('0000000' + CAST(8000000 + @SupplierSeq AS NVARCHAR(10)), 7),
        'supplier' + CAST(@SupplierSeq AS NVARCHAR(10)) + '@vendor.co.th',
        'Industrial Estate Zone ' + CAST((@SupplierSeq % 10) + 1 AS NVARCHAR(10)),
        RIGHT('0000000000000' + CAST(2000000000000 + @SupplierSeq AS NVARCHAR(20)), 13),
        1,
        DATEADD(DAY, -@SupplierSeq, @Now)
    );
    SET @SupplierCount += 1;
END

-- 5) Products: top up to 100 (per primary branch)
DECLARE @CategoryId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Categories WHERE BranchId = @PrimaryBranchId ORDER BY DisplayOrder, Name);
DECLARE @ProductCount INT = (SELECT COUNT(*) FROM Products WHERE BranchId = @PrimaryBranchId);
WHILE @ProductCount < @TargetPerMenu
BEGIN
    DECLARE @ProductSeq INT = @ProductCount + 1;
    DECLARE @ProductId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Sku NVARCHAR(50) = 'AUTO' + RIGHT('0000' + CAST(@ProductSeq AS NVARCHAR(4)), 4);
    DECLARE @Cost DECIMAL(10,2) = CAST(15 + (@ProductSeq % 20) * 3 AS DECIMAL(10,2));
    DECLARE @Price DECIMAL(10,2) = CAST(@Cost * 1.7 AS DECIMAL(10,2));

    INSERT INTO Products (Id, BranchId, CategoryId, Sku, Barcode, Name, Price, CostPrice, Unit, ProductType, IsActive, IsFeatured, Taxable, CreatedAt, UpdatedAt)
    VALUES
    (
        @ProductId,
        @PrimaryBranchId,
        @CategoryId,
        @Sku,
        '8859' + RIGHT('000000000' + CAST(@ProductSeq AS NVARCHAR(9)), 9),
        'POS Product ' + CAST(@ProductSeq AS NVARCHAR(10)),
        @Price,
        @Cost,
        'piece',
        'TRACKABLE_STOCK',
        1,
        CASE WHEN @ProductSeq % 8 = 0 THEN 1 ELSE 0 END,
        1,
        DATEADD(DAY, -@ProductSeq, @Now),
        @Now
    );

    INSERT INTO ProductStocks (Id, ProductId, BranchId, PhysicalQty, ReservedQty, CommittedQty, MinAlertQty, UpdatedAt)
    VALUES
    (
        NEWID(),
        @ProductId,
        @PrimaryBranchId,
        30 + (@ProductSeq % 70),
        0,
        0,
        10,
        @Now
    );

    SET @ProductCount += 1;
END

-- 6) Purchase Orders: top up to 100 (per primary branch)
DECLARE @PoCount INT = (SELECT COUNT(*) FROM PurchaseOrders WHERE BranchId = @PrimaryBranchId);
DECLARE @PoUserId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE BranchId = @PrimaryBranchId ORDER BY CreatedAt);
DECLARE @PoSupplierId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Suppliers WHERE BranchId = @PrimaryBranchId ORDER BY CreatedAt);

WHILE @PoCount < @TargetPerMenu
BEGIN
    DECLARE @PoSeq INT = @PoCount + 1;
    DECLARE @PoId UNIQUEIDENTIFIER = NEWID();
    DECLARE @PoNumber NVARCHAR(50) = 'PO' + CONVERT(NVARCHAR(8), GETUTCDATE(), 112) + RIGHT('0000' + CAST(@PoSeq AS NVARCHAR(4)), 4);
    DECLARE @Status NVARCHAR(30) =
        CASE WHEN @PoSeq % 5 = 0 THEN 'COMPLETED'
             WHEN @PoSeq % 3 = 0 THEN 'ORDERED'
             ELSE 'DRAFT' END;

    INSERT INTO PurchaseOrders (Id, BranchId, SupplierId, UserId, PoNumber, Status, TotalAmount, Note, OrderedAt, ReceivedAt, CreatedAt, UpdatedAt)
    VALUES
    (
        @PoId,
        @PrimaryBranchId,
        @PoSupplierId,
        @PoUserId,
        @PoNumber,
        @Status,
        0,
        'Auto generated for realistic test data',
        DATEADD(DAY, -@PoSeq, @Now),
        CASE WHEN @Status = 'COMPLETED' THEN DATEADD(DAY, -@PoSeq + 1, @Now) ELSE NULL END,
        DATEADD(DAY, -@PoSeq, @Now),
        @Now
    );

    DECLARE @LineCount INT = 1;
    DECLARE @TotalAmount DECIMAL(12,2) = 0;
    WHILE @LineCount <= 3
    BEGIN
        DECLARE @LineProductId UNIQUEIDENTIFIER;
        DECLARE @LineCost DECIMAL(10,2);
        SELECT TOP 1 @LineProductId = Id, @LineCost = CostPrice
        FROM Products
        WHERE BranchId = @PrimaryBranchId
        ORDER BY NEWID();

        DECLARE @LineQty INT = 5 + ABS(CHECKSUM(NEWID())) % 20;
        SET @TotalAmount += (@LineCost * @LineQty);

        INSERT INTO PurchaseOrderItems (Id, PurchaseOrderId, ProductId, OrderedQty, ReceivedQty, CostPrice)
        VALUES
        (
            NEWID(),
            @PoId,
            @LineProductId,
            @LineQty,
            CASE WHEN @Status = 'COMPLETED' THEN @LineQty ELSE 0 END,
            @LineCost
        );

        SET @LineCount += 1;
    END

    UPDATE PurchaseOrders
    SET TotalAmount = @TotalAmount
    WHERE Id = @PoId;

    SET @PoCount += 1;
END

PRINT 'SEED COMPLETE: 100 records per menu inserted';
