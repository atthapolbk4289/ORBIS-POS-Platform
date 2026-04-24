USE PosDB;
GO

SET NOCOUNT ON;

DECLARE @BranchId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Branches);
DECLARE @CatFood UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Categories WHERE Type = 'FOOD');

-- Insert 1000 Products
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    DECLARE @ProductId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Sku NVARCHAR(50) = 'SKU' + RIGHT('000000' + CAST(@i AS NVARCHAR), 6);
    DECLARE @Name NVARCHAR(150) = N'สินค้าทดสอบ ' + CAST(@i AS NVARCHAR);
    DECLARE @Price DECIMAL(10,2) = 100.00 + (@i % 100);
    DECLARE @Cost DECIMAL(10,2) = @Price * 0.5;

    INSERT INTO Products (Id, BranchId, CategoryId, Sku, Name, Price, CostPrice, ProductType, IsActive)
    VALUES (@ProductId, @BranchId, @CatFood, @Sku, @Name, @Price, @Cost, 'TRACKABLE_STOCK', 1);

    INSERT INTO ProductStocks (Id, ProductId, BranchId, PhysicalQty, ReservedQty, CommittedQty, MinAlertQty)
    VALUES (NEWID(), @ProductId, @BranchId, 100, 0, 0, 10);

    SET @i = @i + 1;
END

PRINT '1000 products created successfully.';
GO
