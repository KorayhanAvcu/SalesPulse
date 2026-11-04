-- =============================================================
-- 07) Execution Plan ekran görüntüleri için sorgular
-- =============================================================
-- Kullanım:
--   1. SSMS'te SalesPulseDb seçili olsun.
--   2. Ctrl+M  (Gerçek Yürütme Planını Ekle) açık olsun.
--   3. Her bloğu AYRI AYRI seçip F5 ile çalıştır.
--   4. "Yürütme Planı" sekmesinin ekran görüntüsünü al (Win + Shift + S)
--      ve docs/screenshots/ klasörüne verilen adla kaydet.
-- =============================================================

-- -------------------------------------------------------------
-- PLAN 1 → plan-1-index-seek.png
-- Tek siparişin kalemleri. Beklenen: Index Seek (IX_OrderDetails_OrderId)
-- 3,5 milyon satırlık tabloda sadece 3 sayfa okunur.
-- -------------------------------------------------------------
SELECT od.OrderDetailId, p.Name, od.Quantity, od.UnitPrice
FROM dbo.OrderDetails od
JOIN dbo.Products p ON p.ProductId = od.ProductId
WHERE od.OrderId = 1500000;
GO

-- -------------------------------------------------------------
-- PLAN 2 → plan-2-columnstore.png
-- Son 12 ayın kategori bazlı aylık cirosu.
-- Beklenen: Columnstore Index Scan (NCCI_OrderDetails_Analytics,
-- NCCI_Orders_Analytics) + Batch mode Hash Match
-- -------------------------------------------------------------
EXEC dbo.sp_Sales_CategoryMonthly @StartDate = '2025-11-01', @EndDate = '2026-10-31';
GO

-- -------------------------------------------------------------
-- PLAN 3 (karşılaştırma için) → plan-3-ayni-sorgu-scan.png
-- Aynı tarih aralığında, columnstore KULLANMADAN.
-- Beklenen: rowstore Index Scan + satır satır Hash Match
-- -------------------------------------------------------------
SELECT g.Name AS GroupName, SUM(od.Quantity * od.UnitPrice) AS Revenue
FROM dbo.Orders o
JOIN dbo.OrderDetails od ON od.OrderId   = o.OrderId
JOIN dbo.Products p      ON p.ProductId  = od.ProductId
JOIN dbo.Categories c    ON c.CategoryId = p.CategoryId
JOIN dbo.Categories g    ON g.CategoryId = ISNULL(c.ParentCategoryId, c.CategoryId)
WHERE o.OrderDate >= '2025-11-01' AND o.OrderDate < '2026-11-01'
  AND o.StatusId NOT IN (5, 6)
GROUP BY g.Name
OPTION (IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX);
GO
