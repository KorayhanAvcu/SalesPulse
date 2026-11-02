-- =============================================
-- SalesPulse - İndeksler
--
-- ÇALIŞTIRMA SIRASI:
--   1) 04_performance_tests.sql'i indeks YOKKEN çalıştır, süreleri kaydet ("öncesi")
--   2) Bu dosyayı çalıştır
--   3) 04_performance_tests.sql'i tekrar çalıştır ("sonrası")
--
-- Dosya tekrar çalıştırılabilir: her indeks önce silinir, sonra oluşturulur.
-- =============================================
USE SalesPulseDb;
GO

-- ---------------------------------------------
-- 1) Orders(OrderDate)
-- Kullanan sorgular: dashboard KPI'ları, aylık trend, şehir dağılımı,
-- durum dağılımı, Order Explorer tarih filtresi ve "en yeni önce" sıralaması.
-- INCLUDE kolonları sayesinde bu sorgular tabloya (clustered index'e) hiç gitmez:
-- "covering index".
-- ---------------------------------------------
DROP INDEX IF EXISTS IX_Orders_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate
    ON dbo.Orders (OrderDate)
    INCLUDE (CustomerId, ShippingCityId, StatusId, TotalAmount);
GO

-- ---------------------------------------------
-- 2) Orders(CustomerId, OrderDate)
-- Kullanan sorgular: en çok ciro yapan 20 müşteri, tekrar alışveriş oranı,
-- Order Explorer'da müşteriye göre arama.
-- ---------------------------------------------
DROP INDEX IF EXISTS IX_Orders_CustomerId_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDate
    ON dbo.Orders (CustomerId, OrderDate)
    INCLUDE (StatusId, TotalAmount);
GO

-- ---------------------------------------------
-- 3) OrderDetails(OrderId)
-- FK kolonları SQL Server'da otomatik indekslenmez! Bu indeks olmadan
-- "bir siparişin kalemleri" sorgusu 3,5 milyon satırı tarar.
-- Kullanan sorgular: sipariş detay modalı, kategori/ürün raporları (Orders → OrderDetails JOIN).
-- ---------------------------------------------
DROP INDEX IF EXISTS IX_OrderDetails_OrderId ON dbo.OrderDetails;
CREATE NONCLUSTERED INDEX IX_OrderDetails_OrderId
    ON dbo.OrderDetails (OrderId)
    INCLUDE (ProductId, Quantity, UnitPrice);
GO

-- ---------------------------------------------
-- 4) Payments(OrderId) - UNIQUE
-- İki işi birden yapar:
--   a) Sipariş-ödeme 1-1 ilişkisini veritabanında garanti eder
--   b) Order Explorer'daki "ödeme yöntemi" kolonu için hızlı JOIN
-- ---------------------------------------------
DROP INDEX IF EXISTS UX_Payments_OrderId ON dbo.Payments;
CREATE UNIQUE NONCLUSTERED INDEX UX_Payments_OrderId
    ON dbo.Payments (OrderId)
    INCLUDE (PaymentMethodId);
GO

-- ---------------------------------------------
-- 5) Küçük tablolar: Products(CategoryId)
-- 2.000 satır; etkisi küçük ama kategori raporlarında JOIN'i ucuzlatır.
-- ---------------------------------------------
DROP INDEX IF EXISTS IX_Products_CategoryId ON dbo.Products;
CREATE NONCLUSTERED INDEX IX_Products_CategoryId
    ON dbo.Products (CategoryId)
    INCLUDE (Name);
GO

-- İstatistikleri güncelle (toplu yüklemeden sonra iyi alışkanlık)
UPDATE STATISTICS dbo.Orders WITH FULLSCAN;
UPDATE STATISTICS dbo.OrderDetails WITH FULLSCAN;
UPDATE STATISTICS dbo.Payments WITH FULLSCAN;
GO

-- Oluşan indeksler ve boyutları
SELECT  t.name AS TableName,
        i.name AS IndexName,
        i.type_desc,
        SUM(ps.used_page_count) * 8 / 1024 AS SizeMB
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.dm_db_partition_stats ps ON ps.object_id = i.object_id AND ps.index_id = i.index_id
WHERE t.name IN ('Orders', 'OrderDetails', 'Payments', 'Products')
GROUP BY t.name, i.name, i.type_desc
ORDER BY t.name, i.name;
GO
