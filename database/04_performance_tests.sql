-- =============================================
-- SalesPulse - Performans ölçümü (indeks öncesi / sonrası)
--
-- NASIL KULLANILIR:
--   1) SSMS'te "Include Actual Execution Plan" aç (Ctrl+M).
--   2) Bu dosyayı indeksler YOKKEN çalıştır.
--      Messages sekmesinden her sorgu için şunları kaydet:
--        - "SQL Server Execution Times: ... elapsed time = X ms"
--        - "Table 'Orders'. ... logical reads Y"
--        - Aşağıdaki PRINT satırındaki süre
--      Execution Plan sekmesinde plana sağ tık → "Save Execution Plan As..."
--      → docs/plans/Q1_before.sqlplan gibi kaydet (README için ekran görüntüsü de al).
--   3) 02_indexes.sql'i çalıştır.
--   4) Bu dosyayı TEKRAR çalıştır, "sonrası" değerlerini kaydet.
--   5) Her sorguyu 2-3 kez çalıştırıp ilk (soğuk) ölçümü değil, sonrakileri (sıcak) karşılaştır.
--      Ya da aşağıdaki SOĞUK CACHE bloğunu açıp her seferinde soğuk ölç. Hangisini seçersen
--      README'de belirt ve iki ölçümde de aynısını kullan.
--
-- İndeksleri silip "öncesi"ni tekrar ölçmek için en alttaki bloğu kullan.
-- =============================================
USE SalesPulseDb;
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
SET NOCOUNT ON;
GO

-- SOĞUK CACHE (isteğe bağlı, SADECE kendi geliştirme sunucunda!)
-- CHECKPOINT;
-- DBCC DROPCLEANBUFFERS;
-- GO

DECLARE @t DATETIME2(7);

-- =============================================================
-- Q1) Dashboard KPI'ları - 2026 Ocak-Ekim
-- Beklenen: indekssiz Clustered Index SCAN (2M satır) →
--           IX_Orders_OrderDate ile Index SEEK (sadece aralıktaki satırlar)
-- =============================================================
SET @t = SYSDATETIME();

SELECT
    COUNT(*)                                                     AS TotalOrders,
    SUM(CASE WHEN StatusId NOT IN (5, 6) THEN TotalAmount END)   AS TotalRevenue,
    COUNT(DISTINCT CustomerId)                                   AS TotalCustomers
FROM dbo.Orders
WHERE OrderDate >= '2026-01-01' AND OrderDate < '2026-11-01';

PRINT CONCAT('Q1 KPI süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- =============================================================
-- Q2) Order Explorer - İstanbul, son 3 ay, en yeni önce, 1. sayfa + toplam sayı
-- Beklenen: indekssiz SCAN + SORT → indeksli SEEK, sıralama indeksten
-- =============================================================
SET @t = SYSDATETIME();

SELECT COUNT(*) AS TotalCount
FROM dbo.Orders o
WHERE o.ShippingCityId = 34
  AND o.OrderDate >= '2026-08-01' AND o.OrderDate < '2026-11-01';

SELECT o.OrderId, cu.FirstName + N' ' + cu.LastName AS CustomerName, ci.Name AS CityName,
       o.OrderDate, o.TotalAmount, pm.Name AS PaymentMethod, os.Name AS StatusName
FROM dbo.Orders o
JOIN dbo.Customers cu     ON cu.CustomerId = o.CustomerId
JOIN dbo.Cities ci        ON ci.CityId     = o.ShippingCityId
JOIN dbo.OrderStatuses os ON os.StatusId   = o.StatusId
LEFT JOIN dbo.Payments pa       ON pa.OrderId = o.OrderId
LEFT JOIN dbo.PaymentMethods pm ON pm.PaymentMethodId = pa.PaymentMethodId
WHERE o.ShippingCityId = 34
  AND o.OrderDate >= '2026-08-01' AND o.OrderDate < '2026-11-01'
ORDER BY o.OrderDate DESC, o.OrderId DESC
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT CONCAT('Q2 Order Explorer (sayfa 1) süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- =============================================================
-- Q3) Sales Intelligence - 2025'in en çok ciro yapan 20 müşterisi + tekrar alışveriş (HAVING)
-- Beklenen: indekssiz SCAN + Hash Aggregate → IX_Orders_OrderDate / IX_Orders_CustomerId_OrderDate
-- =============================================================
SET @t = SYSDATETIME();

SELECT TOP (20)
    o.CustomerId, COUNT(*) AS OrderCount, SUM(o.TotalAmount) AS Revenue
FROM dbo.Orders o
WHERE o.OrderDate >= '2025-01-01' AND o.OrderDate < '2026-01-01'
  AND o.StatusId NOT IN (5, 6)
GROUP BY o.CustomerId
ORDER BY Revenue DESC;

SELECT COUNT(*) AS RepeatBuyers
FROM (
    SELECT o.CustomerId
    FROM dbo.Orders o
    WHERE o.OrderDate >= '2025-01-01' AND o.OrderDate < '2026-01-01'
      AND o.StatusId NOT IN (5, 6)
    GROUP BY o.CustomerId
    HAVING COUNT(*) >= 2
) r;

PRINT CONCAT('Q3 Top müşteri + tekrar oranı süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- =============================================================
-- Q4) Sipariş detay modalı - tek siparişin kalemleri
-- En çarpıcı fark: FK kolonu OrderId indekssizken 3,5M satırlık SCAN,
--                  IX_OrderDetails_OrderId ile birkaç satırlık SEEK
-- =============================================================
SET @t = SYSDATETIME();

SELECT od.OrderDetailId, p.Name, od.Quantity, od.UnitPrice
FROM dbo.OrderDetails od
JOIN dbo.Products p ON p.ProductId = od.ProductId
WHERE od.OrderId = 1500000;

PRINT CONCAT('Q4 Sipariş detayı süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- =============================================================
-- Q5) Dashboard kategori performansı - son 3 ay, ciroya göre ilk 10 kategori
-- =============================================================
SET @t = SYSDATETIME();

SELECT TOP (10)
    c.Name AS CategoryName, SUM(od.Quantity * od.UnitPrice) AS Revenue
FROM dbo.Orders o
JOIN dbo.OrderDetails od ON od.OrderId   = o.OrderId
JOIN dbo.Products p      ON p.ProductId  = od.ProductId
JOIN dbo.Categories c    ON c.CategoryId = p.CategoryId
WHERE o.OrderDate >= '2026-08-01' AND o.OrderDate < '2026-11-01'
  AND o.StatusId NOT IN (5, 6)
GROUP BY c.Name
ORDER BY Revenue DESC;

PRINT CONCAT('Q5 Kategori performansı süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- =============================================================
-- Q6) BONUS: Derin sayfa - OFFSET vs KEYSET
-- OFFSET 50.000 → SQL Server 50.025 satırı okuyup 50.000'ini atar.
-- Keyset → "son gördüğüm kayıttan sonrakiler": sayfa numarası ne olursa olsun aynı maliyet.
-- (İndeksler varken çalıştır; fark indeksliyken net görünür.)
-- =============================================================
SET @t = SYSDATETIME();

SELECT o.OrderId, o.OrderDate, o.TotalAmount
FROM dbo.Orders o
ORDER BY o.OrderDate DESC, o.OrderId DESC
OFFSET 50000 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT CONCAT('Q6a OFFSET 50000 süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');

-- Keyset: önceki sayfanın son satırı (@LastDate, @LastId) olarak gelir
DECLARE @LastDate DATETIME2(0), @LastId INT;
SELECT @LastDate = OrderDate, @LastId = OrderId
FROM dbo.Orders
ORDER BY OrderDate DESC, OrderId DESC
OFFSET 49999 ROWS FETCH NEXT 1 ROWS ONLY;   -- sadece test için: 50.000. satırı bul

SET @t = SYSDATETIME();

SELECT TOP (25) o.OrderId, o.OrderDate, o.TotalAmount
FROM dbo.Orders o
WHERE o.OrderDate < @LastDate
   OR (o.OrderDate = @LastDate AND o.OrderId < @LastId)
ORDER BY o.OrderDate DESC, o.OrderId DESC;

PRINT CONCAT('Q6b KEYSET süresi: ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()), ' ms');
GO

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

/* =============================================================
   SONUÇ TABLOSU (README'ye taşı)

   | Sorgu                         | Öncesi (ms) | Sonrası (ms) | Logical reads öncesi | sonrası | Kullanılan indeks                |
   |-------------------------------|-------------|--------------|----------------------|---------|----------------------------------|
   | Q1 Dashboard KPI              |             |              |                      |         | IX_Orders_OrderDate              |
   | Q2 Order Explorer sayfa 1     |             |              |                      |         | IX_Orders_OrderDate, UX_Payments_OrderId |
   | Q3 Top 20 müşteri + HAVING    |             |              |                      |         | IX_Orders_OrderDate              |
   | Q4 Sipariş detayı             |             |              |                      |         | IX_OrderDetails_OrderId          |
   | Q5 Kategori performansı       |             |              |                      |         | IX_Orders_OrderDate, IX_OrderDetails_OrderId |

   Ölçüm koşulları: SQL Server sürümü (SELECT @@VERSION), CPU/RAM, sıcak/soğuk cache.
   ============================================================= */

/* =============================================================
   "ÖNCESİ"Nİ TEKRAR ÖLÇMEK İÇİN İNDEKSLERİ SİL
   (Seçip çalıştır; sonra 02_indexes.sql ile geri oluştur)
*/
DROP INDEX IF EXISTS IX_Orders_OrderDate            ON dbo.Orders;
DROP INDEX IF EXISTS IX_Orders_CustomerId_OrderDate ON dbo.Orders;
DROP INDEX IF EXISTS IX_OrderDetails_OrderId        ON dbo.OrderDetails;
DROP INDEX IF EXISTS UX_Payments_OrderId            ON dbo.Payments;
DROP INDEX IF EXISTS IX_Products_CategoryId         ON dbo.Products;
   ============================================================= 
