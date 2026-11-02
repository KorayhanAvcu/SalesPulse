-- =============================================
-- SalesPulse - Stored procedure'ler
-- Tekrar çalıştırılabilir (CREATE OR ALTER).
--
-- Ortak kurallar:
--   @From dahil, @To hariç: OrderDate >= @From AND OrderDate < @To
--   Ciro = iptal (5) ve iade (6) dışındaki siparişlerin tutarı. Tüm raporlarda aynı.
--   OPTION (RECOMPILE): seçilen aralık 1 günden 3 yıla kadar değişebildiği için
--   bir aralığa göre derlenen plan diğerine kötü gelir (parameter sniffing).
-- =============================================
USE SalesPulseDb;
GO

-- ---------------------------------------------
-- DASHBOARD
-- ---------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetKpis
    @From DATETIME2(0),
    @To   DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*)                   AS TotalOrders,
        ISNULL(SUM(CASE WHEN StatusId NOT IN (5, 6) THEN TotalAmount ELSE 0 END), 0) AS TotalRevenue,
        COUNT(DISTINCT CustomerId) AS TotalCustomers,
        ISNULL(SUM(CASE WHEN StatusId NOT IN (5, 6) THEN TotalAmount ELSE 0 END)
             / NULLIF(SUM(CASE WHEN StatusId NOT IN (5, 6) THEN 1 ELSE 0 END), 0), 0) AS AverageBasket
    FROM dbo.Orders
    WHERE OrderDate >= @From AND OrderDate < @To
    OPTION (RECOMPILE);
END
GO

-- Seçili aralığın son 12 ayı (aralık 12 aydan kısaysa aralığın tamamı)
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetMonthlyTrend
    @From DATETIME2(0),
    @To   DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Last DATETIME2(0) = DATEADD(SECOND, -1, @To);
    DECLARE @TrendFrom DATETIME2(0) = DATEADD(MONTH, -11, DATEFROMPARTS(YEAR(@Last), MONTH(@Last), 1));
    IF @TrendFrom < @From SET @TrendFrom = @From;

    SELECT
        YEAR(OrderDate)  AS [Year],
        MONTH(OrderDate) AS [Month],
        COUNT(*)         AS OrderCount,
        SUM(CASE WHEN StatusId NOT IN (5, 6) THEN TotalAmount ELSE 0 END) AS Revenue
    FROM dbo.Orders
    WHERE OrderDate >= @TrendFrom AND OrderDate < @To
    GROUP BY YEAR(OrderDate), MONTH(OrderDate)
    ORDER BY [Year], [Month]
    OPTION (RECOMPILE);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetTopCategories
    @From DATETIME2(0),
    @To   DATETIME2(0),
    @Top  INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        c.Name                         AS CategoryName,
        parent.Name                    AS ParentName,
        SUM(d.Quantity)                AS Quantity,
        SUM(d.Quantity * d.UnitPrice)  AS Revenue
    FROM dbo.Orders o
    JOIN dbo.OrderDetails d ON d.OrderId = o.OrderId
    JOIN dbo.Products p     ON p.ProductId = d.ProductId
    JOIN dbo.Categories c   ON c.CategoryId = p.CategoryId
    LEFT JOIN dbo.Categories parent ON parent.CategoryId = c.ParentCategoryId
    WHERE o.OrderDate >= @From AND o.OrderDate < @To
      AND o.StatusId NOT IN (5, 6)
    GROUP BY c.Name, parent.Name
    ORDER BY Revenue DESC
    OPTION (RECOMPILE);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetStatusCounts
    @From DATETIME2(0),
    @To   DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.StatusId,
        s.Name             AS StatusName,
        COUNT(o.OrderId)   AS OrderCount
    FROM dbo.OrderStatuses s
    LEFT JOIN dbo.Orders o
        ON o.StatusId = s.StatusId
       AND o.OrderDate >= @From AND o.OrderDate < @To
    GROUP BY s.StatusId, s.Name
    ORDER BY s.StatusId
    OPTION (RECOMPILE);
END
GO

-- İki sonuç kümesi döner: 1) satış adedine göre  2) ciroya göre ilk @Top ürün.
-- Toplama bir kez yapılır, iki ayrı sorguya gerek kalmaz.
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetTopProducts
    @From DATETIME2(0),
    @To   DATETIME2(0),
    @Top  INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.ProductId,
        SUM(d.Quantity)               AS Quantity,
        SUM(d.Quantity * d.UnitPrice) AS Revenue
    INTO #ProductSales
    FROM dbo.Orders o
    JOIN dbo.OrderDetails d ON d.OrderId = o.OrderId
    WHERE o.OrderDate >= @From AND o.OrderDate < @To
      AND o.StatusId NOT IN (5, 6)
    GROUP BY d.ProductId
    OPTION (RECOMPILE);

    SELECT TOP (@Top) p.ProductId, p.Name AS ProductName, c.Name AS CategoryName, s.Quantity, s.Revenue
    FROM #ProductSales s
    JOIN dbo.Products p   ON p.ProductId = s.ProductId
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    ORDER BY s.Quantity DESC, s.Revenue DESC;

    SELECT TOP (@Top) p.ProductId, p.Name AS ProductName, c.Name AS CategoryName, s.Quantity, s.Revenue
    FROM #ProductSales s
    JOIN dbo.Products p   ON p.ProductId = s.ProductId
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    ORDER BY s.Revenue DESC, s.Quantity DESC;
END
GO

-- ---------------------------------------------
-- SİPARİŞ DETAYI: iki sonuç kümesi (başlık + kalemler)
-- ---------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_Orders_GetDetail
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId,
        o.CustomerId,
        c.FirstName + N' ' + c.LastName AS CustomerName,
        c.Email,
        ci.Name          AS CityName,
        o.OrderDate,
        o.TotalAmount,
        s.StatusId,
        s.Name           AS StatusName,
        pm.Name          AS PaymentMethod,
        pay.InstallmentCount,
        pay.PaymentDate
    FROM dbo.Orders o
    JOIN dbo.Customers c      ON c.CustomerId = o.CustomerId
    JOIN dbo.Cities ci        ON ci.CityId = o.ShippingCityId
    JOIN dbo.OrderStatuses s  ON s.StatusId = o.StatusId
    JOIN dbo.Payments pay     ON pay.OrderId = o.OrderId
    JOIN dbo.PaymentMethods pm ON pm.PaymentMethodId = pay.PaymentMethodId
    WHERE o.OrderId = @OrderId;

    SELECT
        p.ProductId,
        p.Name      AS ProductName,
        cat.Name    AS CategoryName,
        d.Quantity,
        d.UnitPrice,
        d.Quantity * d.UnitPrice AS LineTotal
    FROM dbo.OrderDetails d
    JOIN dbo.Products p     ON p.ProductId = d.ProductId
    JOIN dbo.Categories cat ON cat.CategoryId = p.CategoryId
    WHERE d.OrderId = @OrderId
    ORDER BY d.OrderDetailId;
END
GO

-- ---------------------------------------------
-- SATIŞ ANALİZİ
-- ---------------------------------------------
-- Önce sipariş tablosunda toplanır, isimler en son eklenir:
-- 100.000 müşterinin ismini GROUP BY'a sokmaktan çok daha ucuz.
CREATE OR ALTER PROCEDURE dbo.usp_Report_GetTopCustomers
    @From      DATETIME2(0),
    @To        DATETIME2(0),
    @Top       INT = 20,
    @MinOrders INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Sales AS
    (
        SELECT TOP (@Top)
            CustomerId,
            COUNT(*)         AS OrderCount,
            SUM(TotalAmount) AS Revenue,
            MAX(OrderDate)   AS LastOrderDate
        FROM dbo.Orders
        WHERE OrderDate >= @From AND OrderDate < @To
          AND StatusId NOT IN (5, 6)
        GROUP BY CustomerId
        HAVING COUNT(*) >= @MinOrders
        ORDER BY Revenue DESC
    )
    SELECT
        c.CustomerId,
        c.FirstName + N' ' + c.LastName AS CustomerName,
        ci.Name AS CityName,
        s.OrderCount,
        s.Revenue,
        s.Revenue / s.OrderCount AS AverageBasket,
        s.LastOrderDate
    FROM Sales s
    JOIN dbo.Customers c ON c.CustomerId = s.CustomerId
    JOIN dbo.Cities ci   ON ci.CityId = c.CityId
    ORDER BY s.Revenue DESC
    OPTION (RECOMPILE);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_GetTopCities
    @From DATETIME2(0),
    @To   DATETIME2(0),
    @Top  INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Sales AS
    (
        SELECT TOP (@Top)
            ShippingCityId,
            COUNT(*)                   AS OrderCount,
            COUNT(DISTINCT CustomerId) AS CustomerCount,
            SUM(TotalAmount)           AS Revenue
        FROM dbo.Orders
        WHERE OrderDate >= @From AND OrderDate < @To
          AND StatusId NOT IN (5, 6)
        GROUP BY ShippingCityId
        ORDER BY Revenue DESC
    )
    SELECT
        ci.Name   AS CityName,
        ci.Region,
        s.OrderCount,
        s.CustomerCount,
        s.Revenue,
        s.Revenue / s.OrderCount AS AverageBasket
    FROM Sales s
    JOIN dbo.Cities ci ON ci.CityId = s.ShippingCityId
    ORDER BY s.Revenue DESC
    OPTION (RECOMPILE);
END
GO

-- Seçili aralıkta en çok ciro yapan @Top kategorinin aylık cirosu
CREATE OR ALTER PROCEDURE dbo.usp_Report_GetCategoryMonthly
    @From DATETIME2(0),
    @To   DATETIME2(0),
    @Top  INT = 6
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Monthly AS
    (
        SELECT
            p.CategoryId,
            YEAR(o.OrderDate)  AS [Year],
            MONTH(o.OrderDate) AS [Month],
            SUM(d.Quantity * d.UnitPrice) AS Revenue
        FROM dbo.Orders o
        JOIN dbo.OrderDetails d ON d.OrderId = o.OrderId
        JOIN dbo.Products p     ON p.ProductId = d.ProductId
        WHERE o.OrderDate >= @From AND o.OrderDate < @To
          AND o.StatusId NOT IN (5, 6)
        GROUP BY p.CategoryId, YEAR(o.OrderDate), MONTH(o.OrderDate)
    ),
    Ranked AS
    (
        SELECT *, DENSE_RANK() OVER (ORDER BY CategoryTotal DESC, CategoryId) AS CategoryRank
        FROM (SELECT *, SUM(Revenue) OVER (PARTITION BY CategoryId) AS CategoryTotal FROM Monthly) t
    )
    SELECT c.Name AS CategoryName, r.[Year], r.[Month], r.Revenue
    FROM Ranked r
    JOIN dbo.Categories c ON c.CategoryId = r.CategoryId
    WHERE r.CategoryRank <= @Top
    ORDER BY r.[Year], r.[Month], r.CategoryRank
    OPTION (RECOMPILE);
END
GO

-- Tekrar alışveriş oranı: aralıkta en az 2 geçerli siparişi olan müşteri / sipariş veren müşteri
CREATE OR ALTER PROCEDURE dbo.usp_Report_GetRepeatRate
    @From DATETIME2(0),
    @To   DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PerCustomer AS
    (
        SELECT CustomerId, COUNT(*) AS OrderCount
        FROM dbo.Orders
        WHERE OrderDate >= @From AND OrderDate < @To
          AND StatusId NOT IN (5, 6)
        GROUP BY CustomerId
    )
    SELECT
        COUNT(*) AS CustomerCount,
        ISNULL(SUM(CASE WHEN OrderCount >= 2 THEN 1 ELSE 0 END), 0) AS RepeatCustomerCount,
        ISNULL(SUM(CASE WHEN OrderCount >= 2 THEN OrderCount ELSE 0 END), 0) AS RepeatOrderCount,
        ISNULL(SUM(OrderCount), 0) AS OrderCount
    FROM PerCustomer
    OPTION (RECOMPILE);
END
GO

-- İki tarih aralığının karşılaştırması: 'A' ve 'B' için birer satır
CREATE OR ALTER PROCEDURE dbo.usp_Report_ComparePeriods
    @FromA DATETIME2(0),
    @ToA   DATETIME2(0),
    @FromB DATETIME2(0),
    @ToB   DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        'A' AS Period,
        COUNT(*)                   AS OrderCount,
        ISNULL(SUM(TotalAmount), 0) AS Revenue,
        COUNT(DISTINCT CustomerId) AS CustomerCount,
        ISNULL(SUM(TotalAmount) / NULLIF(COUNT(*), 0), 0) AS AverageBasket
    FROM dbo.Orders
    WHERE OrderDate >= @FromA AND OrderDate < @ToA AND StatusId NOT IN (5, 6)
    UNION ALL
    SELECT
        'B',
        COUNT(*),
        ISNULL(SUM(TotalAmount), 0),
        COUNT(DISTINCT CustomerId),
        ISNULL(SUM(TotalAmount) / NULLIF(COUNT(*), 0), 0)
    FROM dbo.Orders
    WHERE OrderDate >= @FromB AND OrderDate < @ToB AND StatusId NOT IN (5, 6)
    OPTION (RECOMPILE);
END
GO
