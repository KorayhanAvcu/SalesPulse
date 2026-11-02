-- =============================================
-- SalesPulse - Stored Procedure'ler
--
-- Ortak kurallar:
--   * Tarih aralığı: @StartDate ve @EndDate DATE; @EndDate günü DAHİL.
--     Sorgularda yarı açık aralık kullanılır: OrderDate >= @StartDate AND OrderDate < @EndDate + 1 gün
--     (OrderDate üzerinde fonksiyon kullanılmaz → indeks kullanılabilir, "SARGable").
--   * Ciro: İptal (5) ve İade (6) siparişler HARİÇ.
--   * Hepsi CREATE OR ALTER → dosya tekrar çalıştırılabilir (SQL Server 2016 SP1+).
--
-- Dapper'dan çağırma örneği:
--   await conn.QueryAsync<T>("dbo.sp_Dashboard_TopCities",
--       new { StartDate = start, EndDate = end, Top = 10 },
--       commandType: CommandType.StoredProcedure);
-- =============================================
USE SalesPulseDb;
GO

-- =============================================================
-- DASHBOARD
-- =============================================================

-- 1) KPI kartları: toplam sipariş, ciro, müşteri, ortalama sepet
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_Kpis
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    SELECT
        COUNT(*)                                                     AS TotalOrders,
        ISNULL(SUM(CASE WHEN o.StatusId NOT IN (5, 6) THEN o.TotalAmount END), 0) AS TotalRevenue,
        COUNT(DISTINCT o.CustomerId)                                 AS TotalCustomers,
        ISNULL(SUM(CASE WHEN o.StatusId NOT IN (5, 6) THEN o.TotalAmount END)
               / NULLIF(SUM(CASE WHEN o.StatusId NOT IN (5, 6) THEN 1 END), 0), 0) AS AverageBasket
    FROM dbo.Orders o
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate <  @To;
END
GO

-- 2) Aylık satış eğilimi: @EndDate'in ayı dahil son 12 ay
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_MonthlyTrend
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From DATE = DATEADD(MONTH, -11, DATEFROMPARTS(YEAR(@EndDate), MONTH(@EndDate), 1));
    DECLARE @To   DATE = DATEADD(MONTH, 1,   DATEFROMPARTS(YEAR(@EndDate), MONTH(@EndDate), 1));

    SELECT
        DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1)     AS [Month],
        COUNT(*)                                                     AS OrderCount,
        ISNULL(SUM(CASE WHEN o.StatusId NOT IN (5, 6) THEN o.TotalAmount END), 0) AS Revenue
    FROM dbo.Orders o
    WHERE o.OrderDate >= @From
      AND o.OrderDate <  @To
    GROUP BY DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1)
    ORDER BY [Month];
END
GO

-- 3) Kategori performansı: ciroya göre ilk N alt kategori (ana grup adıyla)
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_TopCategories
    @StartDate DATE,
    @EndDate   DATE,
    @Top       INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    SELECT TOP (@Top)
        c.CategoryId,
        c.Name                              AS CategoryName,
        parent.Name                         AS GroupName,
        SUM(od.Quantity)                    AS QuantitySold,
        SUM(od.Quantity * od.UnitPrice)     AS Revenue
    FROM dbo.Orders o
    JOIN dbo.OrderDetails od ON od.OrderId   = o.OrderId
    JOIN dbo.Products p      ON p.ProductId  = od.ProductId
    JOIN dbo.Categories c    ON c.CategoryId = p.CategoryId
    LEFT JOIN dbo.Categories parent ON parent.CategoryId = c.ParentCategoryId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate <  @To
      AND o.StatusId NOT IN (5, 6)
    GROUP BY c.CategoryId, c.Name, parent.Name
    ORDER BY Revenue DESC;
END
GO

-- 4) Bölgesel dağılım: en çok satış yapılan N şehir
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_TopCities
    @StartDate DATE,
    @EndDate   DATE,
    @Top       INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    SELECT TOP (@Top)
        ci.CityId,
        ci.Name             AS CityName,
        ci.Region,
        COUNT(*)            AS OrderCount,
        SUM(o.TotalAmount)  AS Revenue
    FROM dbo.Orders o
    JOIN dbo.Cities ci ON ci.CityId = o.ShippingCityId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate <  @To
      AND o.StatusId NOT IN (5, 6)
    GROUP BY ci.CityId, ci.Name, ci.Region
    ORDER BY Revenue DESC;
END
GO

-- 5) Sipariş durumları dağılımı (tüm durumlar, 0 olanlar dahil)
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_StatusDistribution
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    SELECT
        s.StatusId,
        s.Name                         AS StatusName,
        ISNULL(x.OrderCount, 0)        AS OrderCount
    FROM dbo.OrderStatuses s
    LEFT JOIN (
        SELECT o.StatusId, COUNT(*) AS OrderCount
        FROM dbo.Orders o
        WHERE o.OrderDate >= @StartDate
          AND o.OrderDate <  @To
        GROUP BY o.StatusId
    ) x ON x.StatusId = s.StatusId
    ORDER BY s.StatusId;
END
GO

-- 6) En çok satan ürünler: @OrderBy = 'quantity' (adet) | 'revenue' (ciro)
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_TopProducts
    @StartDate DATE,
    @EndDate   DATE,
    @Top       INT = 10,
    @OrderBy   VARCHAR(10) = 'quantity'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    WITH ProductSales AS (
        SELECT
            od.ProductId,
            SUM(od.Quantity)                AS QuantitySold,
            SUM(od.Quantity * od.UnitPrice) AS Revenue
        FROM dbo.Orders o
        JOIN dbo.OrderDetails od ON od.OrderId = o.OrderId
        WHERE o.OrderDate >= @StartDate
          AND o.OrderDate <  @To
          AND o.StatusId NOT IN (5, 6)
        GROUP BY od.ProductId
    )
    SELECT TOP (@Top)
        p.ProductId,
        p.Name          AS ProductName,
        c.Name          AS CategoryName,
        ps.QuantitySold,
        ps.Revenue
    FROM ProductSales ps
    JOIN dbo.Products p   ON p.ProductId  = ps.ProductId
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    ORDER BY
        CASE WHEN @OrderBy = 'revenue' THEN ps.Revenue ELSE ps.QuantitySold END DESC;
END
GO

-- =============================================================
-- ORDER EXPLORER
-- =============================================================

-- 7) Arama + filtre + sıralama + sunucu taraflı sayfalama (OFFSET/FETCH)
--    1. sonuç kümesi: toplam kayıt sayısı (sayfa sayısı için)
--    2. sonuç kümesi: istenen sayfa
--    Dapper: using var multi = await conn.QueryMultipleAsync(...);
--            var total = await multi.ReadSingleAsync<int>();
--            var rows  = await multi.ReadAsync<OrderListItem>();
--
--    Sıralama kolonu dışarıdan string geldiği için BEYAZ LİSTE ile kontrol edilir
--    (SQL injection'a kapalı); filtreler sp_executesql parametresi olarak geçer.
CREATE OR ALTER PROCEDURE dbo.sp_Orders_Search
    @Search     NVARCHAR(100) = NULL,   -- sipariş no (sayıysa) veya müşteri adı/e-posta
    @StartDate  DATE          = NULL,
    @EndDate    DATE          = NULL,
    @CityId     TINYINT       = NULL,
    @StatusId   TINYINT       = NULL,
    @MinAmount  DECIMAL(12,2) = NULL,
    @MaxAmount  DECIMAL(12,2) = NULL,
    @SortColumn VARCHAR(20)   = 'OrderDate',
    @SortDir    VARCHAR(4)    = 'DESC',
    @Page       INT           = 1,
    @PageSize   INT           = 25
AS
BEGIN
    SET NOCOUNT ON;

    -- Güvenlik ve sınırlar
    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), N'');   -- boş arama = filtre yok
    IF @Page < 1 SET @Page = 1;
    IF @PageSize NOT BETWEEN 1 AND 100 SET @PageSize = 25;

    DECLARE @OrderExpr NVARCHAR(50) =
        CASE @SortColumn
            WHEN 'OrderId'     THEN N'o.OrderId'
            WHEN 'Customer'    THEN N'cu.LastName'
            WHEN 'City'        THEN N'ci.Name'
            WHEN 'TotalAmount' THEN N'o.TotalAmount'
            WHEN 'Status'      THEN N'o.StatusId'
            ELSE                    N'o.OrderDate'
        END;
    DECLARE @Dir NVARCHAR(4) = CASE WHEN UPPER(@SortDir) = 'ASC' THEN N'ASC' ELSE N'DESC' END;

    DECLARE @OrderNo INT = TRY_CAST(@Search AS INT);
    DECLARE @Like NVARCHAR(102) = CASE WHEN @Search IS NULL OR @OrderNo IS NOT NULL THEN NULL ELSE @Search + N'%' END;
    DECLARE @To DATETIME2(0) = CASE WHEN @EndDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0))) END;

    -- Sadece gelen filtreler WHERE'e eklenir → her kombinasyon kendi planını alır
    DECLARE @Where NVARCHAR(MAX) = N' WHERE 1 = 1';
    IF @OrderNo   IS NOT NULL SET @Where += N' AND o.OrderId = @OrderNo';
    IF @Like      IS NOT NULL SET @Where += N' AND (cu.FirstName + N'' '' + cu.LastName LIKE @Like OR cu.LastName LIKE @Like OR cu.Email LIKE @Like)';
    IF @StartDate IS NOT NULL SET @Where += N' AND o.OrderDate >= @StartDate';
    IF @To        IS NOT NULL SET @Where += N' AND o.OrderDate < @To';
    IF @CityId    IS NOT NULL SET @Where += N' AND o.ShippingCityId = @CityId';
    IF @StatusId  IS NOT NULL SET @Where += N' AND o.StatusId = @StatusId';
    IF @MinAmount IS NOT NULL SET @Where += N' AND o.TotalAmount >= @MinAmount';
    IF @MaxAmount IS NOT NULL SET @Where += N' AND o.TotalAmount <= @MaxAmount';

    DECLARE @From NVARCHAR(MAX) = N'
        FROM dbo.Orders o
        JOIN dbo.Customers cu ON cu.CustomerId = o.CustomerId
        JOIN dbo.Cities ci    ON ci.CityId     = o.ShippingCityId';

    DECLARE @Params NVARCHAR(MAX) = N'
        @OrderNo INT, @Like NVARCHAR(102), @StartDate DATE, @To DATETIME2(0),
        @CityId TINYINT, @StatusId TINYINT, @MinAmount DECIMAL(12,2), @MaxAmount DECIMAL(12,2),
        @Offset INT, @PageSize INT';

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT COUNT(*) AS TotalCount' + @From + @Where + N';

        SELECT
            o.OrderId,
            cu.FirstName + N'' '' + cu.LastName AS CustomerName,
            ci.Name          AS CityName,
            o.OrderDate,
            o.TotalAmount,
            pm.Name          AS PaymentMethod,
            os.Name          AS StatusName,
            o.StatusId' + @From + N'
        JOIN dbo.OrderStatuses os ON os.StatusId = o.StatusId
        LEFT JOIN dbo.Payments pa        ON pa.OrderId = o.OrderId
        LEFT JOIN dbo.PaymentMethods pm  ON pm.PaymentMethodId = pa.PaymentMethodId'
        + @Where + N'
        ORDER BY ' + @OrderExpr + N' ' + @Dir + N', o.OrderId ' + @Dir + N'
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;';

    EXEC sys.sp_executesql @Sql, @Params,
        @OrderNo = @OrderNo, @Like = @Like, @StartDate = @StartDate, @To = @To,
        @CityId = @CityId, @StatusId = @StatusId, @MinAmount = @MinAmount, @MaxAmount = @MaxAmount,
        @Offset = @Offset, @PageSize = @PageSize;
END
GO

-- 8) Sipariş detay modalı: 1. küme başlık, 2. küme kalemler
CREATE OR ALTER PROCEDURE dbo.sp_Orders_GetDetail
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId, o.OrderDate, o.TotalAmount, os.Name AS StatusName,
        cu.CustomerId, cu.FirstName + N' ' + cu.LastName AS CustomerName, cu.Email,
        ci.Name AS ShippingCity,
        pm.Name AS PaymentMethod, pa.InstallmentCount, pa.PaymentDate
    FROM dbo.Orders o
    JOIN dbo.Customers cu     ON cu.CustomerId = o.CustomerId
    JOIN dbo.Cities ci        ON ci.CityId     = o.ShippingCityId
    JOIN dbo.OrderStatuses os ON os.StatusId   = o.StatusId
    LEFT JOIN dbo.Payments pa       ON pa.OrderId = o.OrderId
    LEFT JOIN dbo.PaymentMethods pm ON pm.PaymentMethodId = pa.PaymentMethodId
    WHERE o.OrderId = @OrderId;

    SELECT
        od.OrderDetailId, p.ProductId, p.Name AS ProductName, c.Name AS CategoryName,
        od.Quantity, od.UnitPrice, od.Quantity * od.UnitPrice AS LineTotal
    FROM dbo.OrderDetails od
    JOIN dbo.Products p   ON p.ProductId  = od.ProductId
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    WHERE od.OrderId = @OrderId
    ORDER BY od.OrderDetailId;
END
GO

-- 9) Kontrollü CSV dışa aktarma: filtreler sp_Orders_Search ile aynı mantık,
--    ama tek seferde en fazla @MaxRows (üst sınır 10.000) satır.
CREATE OR ALTER PROCEDURE dbo.sp_Orders_Export
    @StartDate  DATE          = NULL,
    @EndDate    DATE          = NULL,
    @CityId     TINYINT       = NULL,
    @StatusId   TINYINT       = NULL,
    @MinAmount  DECIMAL(12,2) = NULL,
    @MaxAmount  DECIMAL(12,2) = NULL,
    @MaxRows    INT           = 10000
AS
BEGIN
    SET NOCOUNT ON;
    IF @MaxRows NOT BETWEEN 1 AND 10000 SET @MaxRows = 10000;
    DECLARE @To DATETIME2(0) = CASE WHEN @EndDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0))) END;

    SELECT TOP (@MaxRows)
        o.OrderId,
        cu.FirstName + N' ' + cu.LastName AS CustomerName,
        ci.Name       AS CityName,
        o.OrderDate,
        o.TotalAmount,
        pm.Name       AS PaymentMethod,
        os.Name       AS StatusName
    FROM dbo.Orders o
    JOIN dbo.Customers cu     ON cu.CustomerId = o.CustomerId
    JOIN dbo.Cities ci        ON ci.CityId     = o.ShippingCityId
    JOIN dbo.OrderStatuses os ON os.StatusId   = o.StatusId
    LEFT JOIN dbo.Payments pa       ON pa.OrderId = o.OrderId
    LEFT JOIN dbo.PaymentMethods pm ON pm.PaymentMethodId = pa.PaymentMethodId
    WHERE (@StartDate IS NULL OR o.OrderDate >= @StartDate)
      AND (@To        IS NULL OR o.OrderDate <  @To)
      AND (@CityId    IS NULL OR o.ShippingCityId = @CityId)
      AND (@StatusId  IS NULL OR o.StatusId = @StatusId)
      AND (@MinAmount IS NULL OR o.TotalAmount >= @MinAmount)
      AND (@MaxAmount IS NULL OR o.TotalAmount <= @MaxAmount)
    ORDER BY o.OrderDate DESC
    OPTION (RECOMPILE); -- "opsiyonel filtre" kalıbında her çağrıya uygun plan
END
GO

-- =============================================================
-- SALES INTELLIGENCE
-- =============================================================

-- 10) En yüksek ciro sağlayan N müşteri
CREATE OR ALTER PROCEDURE dbo.sp_Sales_TopCustomers
    @StartDate DATE,
    @EndDate   DATE,
    @Top       INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    WITH CustomerSales AS (
        SELECT
            o.CustomerId,
            COUNT(*)            AS OrderCount,
            SUM(o.TotalAmount)  AS Revenue,
            MAX(o.OrderDate)    AS LastOrderDate
        FROM dbo.Orders o
        WHERE o.OrderDate >= @StartDate
          AND o.OrderDate <  @To
          AND o.StatusId NOT IN (5, 6)
        GROUP BY o.CustomerId
    )
    SELECT TOP (@Top)
        cu.CustomerId,
        cu.FirstName + N' ' + cu.LastName AS CustomerName,
        ci.Name AS CityName,
        cs.OrderCount,
        cs.Revenue,
        cs.Revenue / cs.OrderCount AS AverageBasket,
        cs.LastOrderDate
    FROM CustomerSales cs
    JOIN dbo.Customers cu ON cu.CustomerId = cs.CustomerId
    JOIN dbo.Cities ci    ON ci.CityId     = cu.CityId
    ORDER BY cs.Revenue DESC;
END
GO

-- 11) Kategorilerin aylık performansı (ana grup bazında; grafik için satır = ay × grup)
CREATE OR ALTER PROCEDURE dbo.sp_Sales_CategoryMonthly
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    SELECT
        DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1) AS [Month],
        g.CategoryId                        AS GroupId,
        g.Name                              AS GroupName,
        SUM(od.Quantity)                    AS QuantitySold,
        SUM(od.Quantity * od.UnitPrice)     AS Revenue
    FROM dbo.Orders o
    JOIN dbo.OrderDetails od ON od.OrderId   = o.OrderId
    JOIN dbo.Products p      ON p.ProductId  = od.ProductId
    JOIN dbo.Categories c    ON c.CategoryId = p.CategoryId
    JOIN dbo.Categories g    ON g.CategoryId = ISNULL(c.ParentCategoryId, c.CategoryId)
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate <  @To
      AND o.StatusId NOT IN (5, 6)
    GROUP BY DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1), g.CategoryId, g.Name
    ORDER BY [Month], Revenue DESC;
END
GO

-- 12) Tekrar alışveriş yapan müşteri oranı (HAVING ile)
--     Dönemde sipariş veren müşterilerden kaçı dönem içinde 2+ sipariş verdi?
CREATE OR ALTER PROCEDURE dbo.sp_Sales_RepeatCustomerRate
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @To DATETIME2(0) = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2(0)));

    DECLARE @Buyers INT, @RepeatBuyers INT;

    SELECT @Buyers = COUNT(DISTINCT o.CustomerId)
    FROM dbo.Orders o
    WHERE o.OrderDate >= @StartDate AND o.OrderDate < @To
      AND o.StatusId NOT IN (5, 6);

    SELECT @RepeatBuyers = COUNT(*)
    FROM (
        SELECT o.CustomerId
        FROM dbo.Orders o
        WHERE o.OrderDate >= @StartDate AND o.OrderDate < @To
          AND o.StatusId NOT IN (5, 6)
        GROUP BY o.CustomerId
        HAVING COUNT(*) >= 2
    ) r;

    SELECT
        @Buyers                                                       AS Buyers,
        @RepeatBuyers                                                 AS RepeatBuyers,
        CAST(100.0 * @RepeatBuyers / NULLIF(@Buyers, 0) AS DECIMAL(5,2)) AS RepeatRatePercent;
END
GO

-- 13) İki tarih aralığının karşılaştırması (ör. bu yıl vs geçen yıl aynı dönem)
CREATE OR ALTER PROCEDURE dbo.sp_Sales_PeriodComparison
    @AStartDate DATE, @AEndDate DATE,   -- A: önceki dönem
    @BStartDate DATE, @BEndDate DATE    -- B: karşılaştırılan dönem
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ATo DATETIME2(0) = DATEADD(DAY, 1, CAST(@AEndDate AS DATETIME2(0)));
    DECLARE @BTo DATETIME2(0) = DATEADD(DAY, 1, CAST(@BEndDate AS DATETIME2(0)));

    WITH Periods AS (
        SELECT 'A' AS Period, CAST(@AStartDate AS DATETIME2(0)) AS FromDate, @ATo AS ToDate
        UNION ALL
        SELECT 'B', CAST(@BStartDate AS DATETIME2(0)), @BTo
    ),
    Metrics AS (
        SELECT
            p.Period,
            COUNT(*)                     AS OrderCount,
            SUM(o.TotalAmount)           AS Revenue,
            COUNT(DISTINCT o.CustomerId) AS CustomerCount
        FROM Periods p
        JOIN dbo.Orders o
          ON o.OrderDate >= p.FromDate
         AND o.OrderDate <  p.ToDate
         AND o.StatusId NOT IN (5, 6)
        GROUP BY p.Period
    )
    SELECT
        a.OrderCount    AS A_OrderCount,   b.OrderCount    AS B_OrderCount,
        CAST(100.0 * (b.OrderCount - a.OrderCount) / NULLIF(a.OrderCount, 0) AS DECIMAL(8,2)) AS OrderChangePercent,
        a.Revenue       AS A_Revenue,      b.Revenue       AS B_Revenue,
        CAST(100.0 * (b.Revenue - a.Revenue) / NULLIF(a.Revenue, 0) AS DECIMAL(8,2))          AS RevenueChangePercent,
        a.CustomerCount AS A_CustomerCount, b.CustomerCount AS B_CustomerCount,
        a.Revenue / NULLIF(a.OrderCount, 0) AS A_AverageBasket,
        b.Revenue / NULLIF(b.OrderCount, 0) AS B_AverageBasket
    FROM (SELECT * FROM Metrics WHERE Period = 'A') a
    CROSS JOIN (SELECT * FROM Metrics WHERE Period = 'B') b;
END
GO

-- Oluşan prosedürler
SELECT name, create_date, modify_date
FROM sys.procedures
WHERE name LIKE 'sp[_]%'
ORDER BY name;
GO
