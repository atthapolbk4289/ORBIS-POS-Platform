

CREATE DATABASE PosDB;
GO
USE PosDB;
GO

-- ════════════════════════════════════════
-- TABLE 1: Branches (สาขา)
-- ════════════════════════════════════════
CREATE TABLE Branches (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name        NVARCHAR(100)    NOT NULL,
    Address     NVARCHAR(500)    NULL,
    Phone       NVARCHAR(20)     NULL,
    TaxId       NVARCHAR(13)     NULL,    -- เลขประจำตัวผู้เสียภาษี 13 หลัก
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);

-- ════════════════════════════════════════
-- TABLE 2: Users (ผู้ใช้งาน)
-- Role: IT_ADMIN | EXECUTIVE | MANAGER | CASHIER | STOCK_KEEPER
-- Status: ACTIVE | INACTIVE | SUSPENDED
-- ════════════════════════════════════════
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId        UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    Username        NVARCHAR(50)     NOT NULL,
    PasswordHash    NVARCHAR(255)    NOT NULL,
    FullName        NVARCHAR(100)    NOT NULL,
    Phone           NVARCHAR(20)     NULL,
    Email           NVARCHAR(150)    NULL,
    Role            NVARCHAR(20)     NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'ACTIVE',
    Pin             NVARCHAR(255)    NULL,   -- Hashed 6-digit PIN
    LastLoginAt     DATETIME2        NULL,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);
CREATE INDEX IX_Users_BranchId ON Users(BranchId);
CREATE INDEX IX_Users_Role     ON Users(Role);

-- ════════════════════════════════════════
-- TABLE 3: UserSessions
-- ════════════════════════════════════════
CREATE TABLE UserSessions (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    SessionKey  NVARCHAR(500)    NOT NULL,   -- encrypted session key
    IpAddress   NVARCHAR(50)     NULL,
    DeviceInfo  NVARCHAR(300)    NULL,
    IsActive    BIT              NOT NULL DEFAULT 1,
    ExpiresAt   DATETIME2        NOT NULL,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    RevokedAt   DATETIME2        NULL,
    CONSTRAINT UQ_Sessions_Key UNIQUE (SessionKey)
);
CREATE INDEX IX_Sessions_UserId ON UserSessions(UserId);

-- ════════════════════════════════════════
-- TABLE 4: AuditLogs
-- ════════════════════════════════════════
CREATE TABLE AuditLogs (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId      UNIQUEIDENTIFIER NULL REFERENCES Users(Id),
    BranchId    UNIQUEIDENTIFIER NULL,
    Action      NVARCHAR(100)    NOT NULL,
    EntityType  NVARCHAR(50)     NULL,
    EntityId    NVARCHAR(100)    NULL,
    OldValue    NVARCHAR(MAX)    NULL,
    NewValue    NVARCHAR(MAX)    NULL,
    IpAddress   NVARCHAR(50)     NULL,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_AuditLogs_UserId    ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Action    ON AuditLogs(Action);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);

-- ════════════════════════════════════════
-- TABLE 5: Categories
-- Type: FOOD | BEVERAGE | DESSERT | RETAIL | SERVICE
-- ════════════════════════════════════════
CREATE TABLE Categories (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId     UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    Name         NVARCHAR(100)    NOT NULL,
    NameEn       NVARCHAR(100)    NULL,
    Type         NVARCHAR(20)     NOT NULL DEFAULT 'RETAIL',
    DisplayOrder INT              NOT NULL DEFAULT 0,
    ImageUrl     NVARCHAR(500)    NULL,
    IsActive     BIT              NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Categories_BranchId ON Categories(BranchId);

-- ════════════════════════════════════════
-- TABLE 6: Products
-- ProductType: TRACKABLE_STOCK | NON_TRACKABLE | SERVICE
-- ════════════════════════════════════════
CREATE TABLE Products (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    CategoryId  UNIQUEIDENTIFIER NOT NULL REFERENCES Categories(Id),
    Sku         NVARCHAR(50)     NOT NULL,
    Barcode     NVARCHAR(50)     NULL,
    Name        NVARCHAR(150)    NOT NULL,
    NameEn      NVARCHAR(150)    NULL,
    Description NVARCHAR(500)    NULL,
    Price       DECIMAL(10,2)    NOT NULL,
    CostPrice   DECIMAL(10,2)    NOT NULL DEFAULT 0,
    Unit        NVARCHAR(20)     NOT NULL DEFAULT N'ชิ้น',
    ProductType NVARCHAR(30)     NOT NULL DEFAULT 'NON_TRACKABLE',
    IsActive    BIT              NOT NULL DEFAULT 1,
    IsFeatured  BIT              NOT NULL DEFAULT 0,
    ImageUrl    NVARCHAR(500)    NULL,
    Taxable     BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Products_Sku UNIQUE (Sku)
);
CREATE INDEX IX_Products_BranchId   ON Products(BranchId);
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_Barcode    ON Products(Barcode) WHERE Barcode IS NOT NULL;
CREATE INDEX IX_Products_IsActive   ON Products(IsActive);

-- ════════════════════════════════════════
-- TABLE 7: ProductVariants (ตัวเลือกสินค้า)
-- ════════════════════════════════════════
CREATE TABLE ProductVariants (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    Name            NVARCHAR(100)    NOT NULL,   -- "ไม่เผ็ด", "Size L"
    AdditionalPrice DECIMAL(10,2)    NOT NULL DEFAULT 0,
    Sku             NVARCHAR(50)     NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    DisplayOrder    INT              NOT NULL DEFAULT 0
);
CREATE INDEX IX_ProductVariants_ProductId ON ProductVariants(ProductId);

-- ════════════════════════════════════════
-- TABLE 8: ProductStocks (3-Layer Stock)
-- AvailableQty = PhysicalQty - ReservedQty - CommittedQty
-- ════════════════════════════════════════
CREATE TABLE ProductStocks (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ProductId    UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    BranchId     UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    PhysicalQty  INT              NOT NULL DEFAULT 0,
    ReservedQty  INT              NOT NULL DEFAULT 0,
    CommittedQty INT              NOT NULL DEFAULT 0,
    MinAlertQty  INT              NOT NULL DEFAULT 5,
    UpdatedAt    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_ProductStocks_Product UNIQUE (ProductId)
);

-- VIEW: AvailableQty computed
GO
CREATE VIEW vw_ProductAvailability AS
SELECT
    ps.ProductId,
    ps.BranchId,
    ps.PhysicalQty,
    ps.ReservedQty,
    ps.CommittedQty,
    (ps.PhysicalQty - ps.ReservedQty - ps.CommittedQty) AS AvailableQty,
    ps.MinAlertQty,
    CASE WHEN (ps.PhysicalQty - ps.ReservedQty - ps.CommittedQty) <= ps.MinAlertQty
         THEN 1 ELSE 0 END AS IsLowStock
FROM ProductStocks ps;
GO

-- ════════════════════════════════════════
-- TABLE 9: StockMovements
-- MovementType: PURCHASE_IN|SALE_OUT|RETURN_IN|ADJUSTMENT_IN|ADJUSTMENT_OUT|TRANSFER_IN|TRANSFER_OUT
-- ════════════════════════════════════════
CREATE TABLE StockMovements (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId      UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    ProductId     UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    UserId        UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    MovementType  NVARCHAR(30)     NOT NULL,
    Qty           INT              NOT NULL,
    CostPrice     DECIMAL(10,2)    NOT NULL DEFAULT 0,
    BalanceAfter  INT              NOT NULL,
    ReferenceId   NVARCHAR(100)    NULL,
    ReferenceType NVARCHAR(50)     NULL,
    Note          NVARCHAR(500)    NULL,
    CreatedAt     DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_StockMovements_ProductId ON StockMovements(ProductId);
CREATE INDEX IX_StockMovements_BranchId  ON StockMovements(BranchId, CreatedAt);

-- ════════════════════════════════════════
-- TABLE 10: StockReservations
-- Status: PENDING | CONFIRMED | FULFILLED | RELEASED
-- ════════════════════════════════════════
CREATE TABLE StockReservations (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OrderId     UNIQUEIDENTIFIER NOT NULL,
    ProductId   UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    ReservedQty INT              NOT NULL,
    Status      NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
    ReservedAt  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt   DATETIME2        NOT NULL,
    ConfirmedAt DATETIME2        NULL,
    FulfilledAt DATETIME2        NULL,
    ReleasedAt  DATETIME2        NULL
);
CREATE INDEX IX_StockRes_OrderId ON StockReservations(OrderId);
CREATE INDEX IX_StockRes_Status  ON StockReservations(Status, ExpiresAt);

-- ════════════════════════════════════════
-- TABLE 11: Suppliers
-- ════════════════════════════════════════
CREATE TABLE Suppliers (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    Name        NVARCHAR(150)    NOT NULL,
    ContactName NVARCHAR(100)    NULL,
    Phone       NVARCHAR(20)     NULL,
    Email       NVARCHAR(150)    NULL,
    Address     NVARCHAR(500)    NULL,
    TaxId       NVARCHAR(13)     NULL,
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);

-- ════════════════════════════════════════
-- TABLE 12: PurchaseOrders (ใบสั่งซื้อ)
-- Status: DRAFT | ORDERED | PARTIAL_RECEIVED | COMPLETED | CANCELLED
-- ════════════════════════════════════════
CREATE TABLE PurchaseOrders (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    SupplierId  UNIQUEIDENTIFIER NULL REFERENCES Suppliers(Id),
    UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    PoNumber    NVARCHAR(50)     NOT NULL,
    Status      NVARCHAR(30)     NOT NULL DEFAULT 'DRAFT',
    TotalAmount DECIMAL(12,2)    NOT NULL DEFAULT 0,
    Note        NVARCHAR(500)    NULL,
    OrderedAt   DATETIME2        NULL,
    ReceivedAt  DATETIME2        NULL,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_PO_Number UNIQUE (PoNumber)
);

CREATE TABLE PurchaseOrderItems (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    PurchaseOrderId UNIQUEIDENTIFIER NOT NULL REFERENCES PurchaseOrders(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    OrderedQty      INT              NOT NULL,
    ReceivedQty     INT              NOT NULL DEFAULT 0,
    CostPrice       DECIMAL(10,2)    NOT NULL
);

-- ════════════════════════════════════════
-- TABLE 13: DiningTables (โต๊ะ)
-- Status: AVAILABLE | OCCUPIED | RESERVED | CLEANING
-- ════════════════════════════════════════
CREATE TABLE DiningTables (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    TableNumber NVARCHAR(10)     NOT NULL,
    Zone        NVARCHAR(50)     NULL,
    Capacity    INT              NOT NULL DEFAULT 4,
    Status      NVARCHAR(20)     NOT NULL DEFAULT 'AVAILABLE',
    IsActive    BIT              NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Tables_BranchTable UNIQUE (BranchId, TableNumber)
);
CREATE INDEX IX_Tables_BranchId ON DiningTables(BranchId, Status);

-- ════════════════════════════════════════
-- TABLE 14: Customers
-- MemberLevel: NORMAL | SILVER | GOLD | PLATINUM
-- ════════════════════════════════════════
CREATE TABLE Customers (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    Name        NVARCHAR(100)    NOT NULL,
    Phone       NVARCHAR(20)     NOT NULL,
    Email       NVARCHAR(150)    NULL,
    Points      INT              NOT NULL DEFAULT 0,
    TotalSpent  DECIMAL(12,2)    NOT NULL DEFAULT 0,
    MemberLevel NVARCHAR(20)     NOT NULL DEFAULT 'NORMAL',
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Customers_Phone UNIQUE (Phone)
);
CREATE INDEX IX_Customers_BranchId ON Customers(BranchId);

-- ════════════════════════════════════════
-- TABLE 15: Orders
-- OrderType: DINE_IN | TAKEAWAY | DELIVERY | RETAIL
-- Status: DRAFT|PENDING|CONFIRMED|PREPARING|READY|COMPLETED|CANCELLED|REFUNDED
-- PaymentStatus: UNPAID | PARTIAL | PAID | REFUNDED
-- ════════════════════════════════════════
CREATE TABLE Orders (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId       UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    UserId         UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    OrderNumber    NVARCHAR(30)     NOT NULL,
    OrderType      NVARCHAR(20)     NOT NULL,
    TableId        UNIQUEIDENTIFIER NULL REFERENCES DiningTables(Id),
    CustomerId     UNIQUEIDENTIFIER NULL REFERENCES Customers(Id),
    CustomerName   NVARCHAR(100)    NULL,
    Status         NVARCHAR(20)     NOT NULL DEFAULT 'DRAFT',
    Note           NVARCHAR(500)    NULL,
    Subtotal       DECIMAL(10,2)    NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(10,2)    NOT NULL DEFAULT 0,
    PromotionId    UNIQUEIDENTIFIER NULL,
    TaxAmount      DECIMAL(10,2)    NOT NULL DEFAULT 0,
    TaxRate        DECIMAL(5,2)     NOT NULL DEFAULT 7,
    TotalAmount    DECIMAL(10,2)    NOT NULL DEFAULT 0,
    PaymentStatus  NVARCHAR(20)     NOT NULL DEFAULT 'UNPAID',
    IsOffline      BIT              NOT NULL DEFAULT 0,
    OfflineId      NVARCHAR(100)    NULL,
    CreatedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt    DATETIME2        NULL,
    CONSTRAINT UQ_Orders_Number UNIQUE (OrderNumber)
);
CREATE INDEX IX_Orders_BranchStatus  ON Orders(BranchId, Status);
CREATE INDEX IX_Orders_BranchDate    ON Orders(BranchId, CreatedAt);
CREATE INDEX IX_Orders_TableId       ON Orders(TableId);

-- ════════════════════════════════════════
-- TABLE 16: OrderItems
-- Status: PENDING | PREPARING | SERVED | CANCELLED
-- ════════════════════════════════════════
CREATE TABLE OrderItems (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OrderId         UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    VariantId       UNIQUEIDENTIFIER NULL REFERENCES ProductVariants(Id),
    Qty             INT              NOT NULL,
    UnitPrice       DECIMAL(10,2)    NOT NULL,
    CostPrice       DECIMAL(10,2)    NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(10,2)    NOT NULL DEFAULT 0,
    LineTotal       DECIMAL(10,2)    NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
    Note            NVARCHAR(200)    NULL,
    SentToKitchenAt DATETIME2        NULL
);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);

-- ════════════════════════════════════════
-- TABLE 17: Payments
-- PaymentMethod: CASH|CREDIT_CARD|DEBIT_CARD|PROMPTPAY|BANK_TRANSFER|MIXED
-- Status: SUCCESS | FAILED | REFUNDED
-- ════════════════════════════════════════
CREATE TABLE Payments (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OrderId       UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(Id),
    UserId        UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    PaymentMethod NVARCHAR(20)     NOT NULL,
    Amount        DECIMAL(10,2)    NOT NULL,
    ChangeAmount  DECIMAL(10,2)    NOT NULL DEFAULT 0,
    ReferenceNo   NVARCHAR(100)    NULL,
    Status        NVARCHAR(20)     NOT NULL DEFAULT 'SUCCESS',
    PaidAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);

-- ════════════════════════════════════════
-- TABLE 18: TaxInvoices (ใบกำกับภาษี e-Tax)
-- Status: DRAFT|PENDING_SUBMIT|SUBMITTED|ACCEPTED|REJECTED|CANCELLED
-- ════════════════════════════════════════
CREATE TABLE TaxInvoices (
    Id               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OrderId          UNIQUEIDENTIFIER NOT NULL REFERENCES Orders(Id),
    BranchId         UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    InvoiceNumber    NVARCHAR(50)     NOT NULL,
    InvoiceType      NVARCHAR(20)     NOT NULL DEFAULT 'FULL_TAX',
    SellerTaxId      NVARCHAR(13)     NOT NULL,
    SellerName       NVARCHAR(200)    NOT NULL,
    SellerAddress    NVARCHAR(500)    NOT NULL,
    BuyerTaxId       NVARCHAR(13)     NULL,
    BuyerName        NVARCHAR(200)    NULL,
    BuyerAddress     NVARCHAR(500)    NULL,
    Subtotal         DECIMAL(10,2)    NOT NULL,
    TaxRate          DECIMAL(5,2)     NOT NULL DEFAULT 7,
    TaxAmount        DECIMAL(10,2)    NOT NULL,
    TotalAmount      DECIMAL(10,2)    NOT NULL,
    Status           NVARCHAR(20)     NOT NULL DEFAULT 'DRAFT',
    XmlPayload       NVARCHAR(MAX)    NULL,
    ProviderResponse NVARCHAR(MAX)    NULL,
    PdfUrl           NVARCHAR(500)    NULL,
    IssuedAt         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    SubmittedAt      DATETIME2        NULL,
    AcceptedAt       DATETIME2        NULL,
    CONSTRAINT UQ_TaxInvoice_Order   UNIQUE (OrderId),
    CONSTRAINT UQ_TaxInvoice_Number  UNIQUE (InvoiceNumber)
);
CREATE INDEX IX_TaxInvoices_BranchId ON TaxInvoices(BranchId);

-- ════════════════════════════════════════
-- TABLE 19: Promotions
-- Type: PERCENT_DISCOUNT|FIXED_DISCOUNT|BUY_X_GET_Y|FREE_ITEM
-- ApplicableTo: ALL|CATEGORY|SPECIFIC_PRODUCT|MEMBER_ONLY
-- ════════════════════════════════════════
CREATE TABLE Promotions (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId       UNIQUEIDENTIFIER NOT NULL REFERENCES Branches(Id),
    Name           NVARCHAR(150)    NOT NULL,
    Description    NVARCHAR(500)    NULL,
    Type           NVARCHAR(30)     NOT NULL,
    DiscountValue  DECIMAL(10,2)    NOT NULL,
    MinOrderAmount DECIMAL(10,2)    NULL,
    ApplicableTo   NVARCHAR(30)     NOT NULL DEFAULT 'ALL',
    UsageLimit     INT              NULL,
    UsageCount     INT              NOT NULL DEFAULT 0,
    StartDate      DATETIME2        NOT NULL,
    EndDate        DATETIME2        NOT NULL,
    IsActive       BIT              NOT NULL DEFAULT 1,
    CreatedBy      UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    CreatedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE PromotionProducts (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    PromotionId UNIQUEIDENTIFIER NOT NULL REFERENCES Promotions(Id) ON DELETE CASCADE,
    ProductId   UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id)
);

-- ════════════════════════════════════════
-- TABLE 20: OfflineSyncLogs
-- SyncStatus: PENDING|SYNCED|CONFLICT|FAILED
-- ════════════════════════════════════════
CREATE TABLE OfflineSyncLogs (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BranchId       UNIQUEIDENTIFIER NOT NULL,
    DeviceId       NVARCHAR(100)    NOT NULL,
    EntityType     NVARCHAR(50)     NOT NULL,
    LocalId        NVARCHAR(100)    NOT NULL,
    ServerId       NVARCHAR(100)    NULL,
    SyncStatus     NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
    Payload        NVARCHAR(MAX)    NOT NULL,
    ConflictDetail NVARCHAR(MAX)    NULL,
    RetryCount     INT              NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    SyncedAt       DATETIME2        NULL
);
CREATE INDEX IX_OfflineSync_Status ON OfflineSyncLogs(BranchId, SyncStatus);

GO

-- ════════════════════════════════════════
-- STORED PROCEDURES — Stock Reservation
-- ════════════════════════════════════════

CREATE PROCEDURE SP_ReserveStock
    @OrderId    UNIQUEIDENTIFIER,
    @ProductId  UNIQUEIDENTIFIER,
    @Qty        INT,
    @ExpiresAt  DATETIME2,
    @Success    BIT OUTPUT,
    @Message    NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @Physical INT, @Reserved INT, @Committed INT, @Available INT;
        DECLARE @ProductName NVARCHAR(150);

        -- Lock row
        SELECT @Physical  = PhysicalQty,
               @Reserved  = ReservedQty,
               @Committed = CommittedQty
        FROM ProductStocks WITH (UPDLOCK, ROWLOCK)
        WHERE ProductId = @ProductId;

        SET @Available = @Physical - @Reserved - @Committed;

        SELECT @ProductName = Name FROM Products WHERE Id = @ProductId;

        IF @Available < @Qty
        BEGIN
            SET @Success = 0;
            SET @Message = N'สินค้า "' + @ProductName + N'" มีไม่เพียงพอ (พร้อมขาย: '
                         + CAST(@Available AS NVARCHAR) + N', ต้องการ: ' + CAST(@Qty AS NVARCHAR) + N')';
            ROLLBACK;
            RETURN;
        END

        UPDATE ProductStocks
        SET ReservedQty = ReservedQty + @Qty, UpdatedAt = GETUTCDATE()
        WHERE ProductId = @ProductId;

        INSERT INTO StockReservations (Id, OrderId, ProductId, ReservedQty, Status, ExpiresAt)
        VALUES (NEWID(), @OrderId, @ProductId, @Qty, 'PENDING', @ExpiresAt);

        SET @Success = 1;
        SET @Message = N'สำเร็จ';
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        SET @Success = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END;
GO

CREATE PROCEDURE SP_ReleaseExpiredReservations
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ps
    SET ps.ReservedQty = ps.ReservedQty - sr.ReservedQty,
        ps.UpdatedAt = GETUTCDATE()
    FROM ProductStocks ps
    INNER JOIN StockReservations sr ON ps.ProductId = sr.ProductId
    WHERE sr.Status = 'PENDING' AND sr.ExpiresAt < GETUTCDATE();

    UPDATE StockReservations
    SET Status = 'RELEASED', ReleasedAt = GETUTCDATE()
    WHERE Status = 'PENDING' AND ExpiresAt < GETUTCDATE();
END;
GO

-- ════════════════════════════════════════
-- STORED PROCEDURES — Reports
-- ════════════════════════════════════════

CREATE PROCEDURE SP_GetSalesSummary
    @BranchId UNIQUEIDENTIFIER,
    @DateFrom DATE,
    @DateTo   DATE
AS
BEGIN
    -- Summary totals
    SELECT
        COUNT(*)                                    AS TotalOrders,
        ISNULL(SUM(TotalAmount), 0)                 AS TotalRevenue,
        ISNULL(SUM(DiscountAmount), 0)              AS TotalDiscount,
        ISNULL(SUM(TaxAmount), 0)                   AS TotalTax,
        ISNULL(SUM(TotalAmount - TaxAmount), 0)     AS NetRevenue
    FROM Orders
    WHERE BranchId = @BranchId AND Status = 'COMPLETED'
      AND CAST(CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo;

    -- By payment method
    SELECT p.PaymentMethod,
           SUM(p.Amount)       AS Amount,
           COUNT(DISTINCT o.Id) AS OrderCount
    FROM Payments p
    JOIN Orders o ON p.OrderId = o.Id
    WHERE o.BranchId = @BranchId AND o.Status = 'COMPLETED'
      AND CAST(o.CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo
    GROUP BY p.PaymentMethod;

    -- By order type
    SELECT OrderType, COUNT(*) AS OrderCount, SUM(TotalAmount) AS Revenue
    FROM Orders
    WHERE BranchId = @BranchId AND Status = 'COMPLETED'
      AND CAST(CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo
    GROUP BY OrderType;
END;
GO

CREATE PROCEDURE SP_GetProfitLoss
    @BranchId UNIQUEIDENTIFIER,
    @DateFrom DATE,
    @DateTo   DATE
AS
BEGIN
    SELECT
        ISNULL(SUM(o.TotalAmount), 0)                           AS Revenue,
        ISNULL(SUM(oi.Qty * oi.CostPrice), 0)                  AS COGS,
        ISNULL(SUM(o.TotalAmount) - SUM(oi.Qty * oi.CostPrice), 0) AS GrossProfit,
        CASE WHEN SUM(o.TotalAmount) > 0
             THEN ROUND(
                (SUM(o.TotalAmount) - SUM(oi.Qty * oi.CostPrice))
                / SUM(o.TotalAmount) * 100, 2)
             ELSE 0 END AS GrossMarginPct
    FROM Orders o
    JOIN OrderItems oi ON o.Id = oi.OrderId
    WHERE o.BranchId = @BranchId AND o.Status = 'COMPLETED'
      AND CAST(o.CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo;
END;
GO

CREATE PROCEDURE SP_GetTopProducts
    @BranchId UNIQUEIDENTIFIER,
    @DateFrom DATE,
    @DateTo   DATE,
    @Limit    INT = 10
AS
BEGIN
    SELECT TOP (@Limit)
        p.Id, p.Name, p.ImageUrl,
        SUM(oi.Qty)                                          AS TotalQty,
        SUM(oi.LineTotal)                                    AS TotalRevenue,
        SUM(oi.LineTotal - (oi.Qty * oi.CostPrice))          AS TotalProfit
    FROM OrderItems oi
    JOIN Products p ON oi.ProductId = p.Id
    JOIN Orders   o ON oi.OrderId   = o.Id
    WHERE o.BranchId = @BranchId AND o.Status = 'COMPLETED'
      AND CAST(o.CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo
    GROUP BY p.Id, p.Name, p.ImageUrl
    ORDER BY TotalRevenue DESC;
END;
GO

CREATE PROCEDURE SP_GetHourlySales
    @BranchId UNIQUEIDENTIFIER,
    @Date     DATE
AS
BEGIN
    SELECT
        DATEPART(HOUR, CreatedAt)   AS [Hour],
        COUNT(*)                     AS OrderCount,
        ISNULL(SUM(TotalAmount), 0) AS Revenue
    FROM Orders
    WHERE BranchId = @BranchId AND Status = 'COMPLETED'
      AND CAST(CreatedAt AS DATE) = @Date
    GROUP BY DATEPART(HOUR, CreatedAt)
    ORDER BY [Hour];
END;
GO

CREATE PROCEDURE SP_GetDailySales
    @BranchId UNIQUEIDENTIFIER,
    @DateFrom DATE,
    @DateTo   DATE
AS
BEGIN
    SELECT
        CAST(CreatedAt AS DATE)     AS [Date],
        COUNT(*)                     AS OrderCount,
        ISNULL(SUM(TotalAmount), 0) AS Revenue
    FROM Orders
    WHERE BranchId = @BranchId AND Status = 'COMPLETED'
      AND CAST(CreatedAt AS DATE) BETWEEN @DateFrom AND @DateTo
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY [Date];
END;
GO

-- ════════════════════════════════════════
-- SEED DATA
-- ════════════════════════════════════════
DECLARE @BranchId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Branches (Id, Name, Address, Phone, TaxId)
VALUES (@BranchId, N'สาขาหลัก', N'123 ถ.ตัวอย่าง แขวงตัวอย่าง กรุงเทพฯ 10000', '02-000-0000', '1234567890123');

-- Password hashes ต้อง generate ใน C# ด้วย BCrypt.Net
-- แต่เพื่อ dev ใส่ placeholder ก่อน แล้วให้ SeedData.cs override
INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Role, Pin)
VALUES
  (NEWID(), @BranchId, 'admin',    'HASH_ADMIN@1234',   N'ผู้ดูแลระบบ',   'IT_ADMIN',    'HASH_111111'),
  (NEWID(), @BranchId, 'exec',     'HASH_EXEC@1234',    N'ผู้บริหาร',      'EXECUTIVE',   'HASH_222222'),
  (NEWID(), @BranchId, 'manager',  'HASH_MGR@1234',     N'ผู้จัดการสาขา', 'MANAGER',     'HASH_333333'),
  (NEWID(), @BranchId, 'cashier1', 'HASH_CASH@1234',    N'แคชเชียร์ 1',   'CASHIER',     'HASH_444444'),
  (NEWID(), @BranchId, 'cashier2', 'HASH_CASH@1234',    N'แคชเชียร์ 2',   'CASHIER',     'HASH_555555'),
  (NEWID(), @BranchId, 'stock',    'HASH_STOCK@1234',   N'พนักงานสต็อก',  'STOCK_KEEPER','HASH_666666');

-- Categories
DECLARE @CatFood UNIQUEIDENTIFIER = NEWID();
DECLARE @CatBev  UNIQUEIDENTIFIER = NEWID();
DECLARE @CatDes  UNIQUEIDENTIFIER = NEWID();
DECLARE @CatRet  UNIQUEIDENTIFIER = NEWID();
INSERT INTO Categories (Id, BranchId, Name, Type, DisplayOrder) VALUES
  (@CatFood, @BranchId, N'อาหาร',     'FOOD',     1),
  (@CatBev,  @BranchId, N'เครื่องดื่ม','BEVERAGE', 2),
  (@CatDes,  @BranchId, N'ของหวาน',   'DESSERT',  3),
  (@CatRet,  @BranchId, N'สินค้าปลีก','RETAIL',   4);

-- DiningTables
INSERT INTO DiningTables (Id, BranchId, TableNumber, Zone, Capacity) VALUES
  (NEWID(), @BranchId, '1', N'โซนใน', 4),
  (NEWID(), @BranchId, '2', N'โซนใน', 4),
  (NEWID(), @BranchId, '3', N'โซนใน', 6),
  (NEWID(), @BranchId, '4', N'โซนนอก', 2),
  (NEWID(), @BranchId, '5', N'โซนนอก', 4);
GO
