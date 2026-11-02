-- =============================================
-- SalesPulse - Veri doğrulama
-- Veri üretici çalıştıktan sonra çalıştır. Her blok bir kontrol;
-- "Sonuc" kolonunda HATA görmemelisin.
-- =============================================
USE SalesPulseDb;
GO
SET NOCOUNT ON;

-- 1) Satır sayıları (PDF hedefleri ile)
SELECT TableName, RowCnt, Target,
       CASE WHEN RowCnt >= Target THEN 'OK' ELSE 'HATA: hedefin altında' END AS Sonuc
FROM (VALUES
    ('Orders',       (SELECT COUNT_BIG(*) FROM dbo.Orders),       2000000),
    ('OrderDetails', (SELECT COUNT_BIG(*) FROM dbo.OrderDetails), 3500000),
    ('Payments',     (SELECT COUNT_BIG(*) FROM dbo.Payments),     2000000),
    ('Customers',    (SELECT COUNT_BIG(*) FROM dbo.Customers),     100000),
    ('Products',     (SELECT COUNT_BIG(*) FROM dbo.Products),        2000),
    ('Categories',   (SELECT COUNT_BIG(*) FROM dbo.Categories),        30),
    ('Cities',       (SELECT COUNT_BIG(*) FROM dbo.Cities),            81)
) v(TableName, RowCnt, Target);

-- 2) FK'ler "güvenilir" mi? (is_not_trusted = 1 ise optimizer FK bilgisini kullanamaz)
SELECT name AS ForeignKey, OBJECT_NAME(parent_object_id) AS TableName,
       CASE WHEN is_not_trusted = 0 AND is_disabled = 0 THEN 'OK' ELSE 'HATA: güvenilmez FK' END AS Sonuc
FROM sys.foreign_keys
ORDER BY TableName;

-- 3) Her siparişin tam 1 ödemesi var mı?
SELECT 'Ödemesiz sipariş' AS Kontrol, COUNT(*) AS Adet,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'HATA' END AS Sonuc
FROM dbo.Orders o
WHERE NOT EXISTS (SELECT 1 FROM dbo.Payments p WHERE p.OrderId = o.OrderId)
UNION ALL
SELECT 'Birden fazla ödemeli sipariş', COUNT(*), CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'HATA' END
FROM (SELECT OrderId FROM dbo.Payments GROUP BY OrderId HAVING COUNT(*) > 1) x
UNION ALL
SELECT 'Kalemsiz sipariş', COUNT(*), CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'HATA' END
FROM dbo.Orders o
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrderDetails d WHERE d.OrderId = o.OrderId);

-- 4) Sipariş toplamı = kalem toplamı = ödeme tutarı mı? (ilk 10 uyumsuz)
SELECT TOP (10) o.OrderId, o.TotalAmount, d.LineTotal, p.Amount
FROM dbo.Orders o
JOIN (SELECT OrderId, SUM(Quantity * UnitPrice) AS LineTotal FROM dbo.OrderDetails GROUP BY OrderId) d
     ON d.OrderId = o.OrderId
JOIN dbo.Payments p ON p.OrderId = o.OrderId
WHERE o.TotalAmount <> d.LineTotal OR o.TotalAmount <> p.Amount;
-- Boş sonuç = OK

-- 5) Müşteri kayıt tarihinden önce sipariş var mı?
SELECT 'Kayıttan önce sipariş' AS Kontrol, COUNT(*) AS Adet,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'HATA' END AS Sonuc
FROM dbo.Orders o
JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
WHERE o.OrderDate < c.CreatedDate;

-- 6) Gerçekçilik: yıllık ve aylık dağılım (Kasım/Aralık zirve, yıllık büyüme beklenir)
SELECT YEAR(OrderDate) AS Yil, MONTH(OrderDate) AS Ay, COUNT(*) AS Siparis,
       SUM(TotalAmount) AS Ciro
FROM dbo.Orders
GROUP BY YEAR(OrderDate), MONTH(OrderDate)
ORDER BY Yil, Ay;

-- 7) Durum ve ödeme yöntemi dağılımı
SELECT s.Name AS Durum, COUNT(*) AS Adet, CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS DECIMAL(5,2)) AS Yuzde
FROM dbo.Orders o JOIN dbo.OrderStatuses s ON s.StatusId = o.StatusId
GROUP BY s.Name ORDER BY Adet DESC;

SELECT m.Name AS OdemeYontemi, COUNT(*) AS Adet, CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS DECIMAL(5,2)) AS Yuzde
FROM dbo.Payments p JOIN dbo.PaymentMethods m ON m.PaymentMethodId = p.PaymentMethodId
GROUP BY m.Name ORDER BY Adet DESC;

-- 8) Şehir yoğunluğu: ilk 10 şehir
SELECT TOP (10) c.Name AS Sehir, COUNT(*) AS Siparis
FROM dbo.Orders o JOIN dbo.Cities c ON c.CityId = o.ShippingCityId
GROUP BY c.Name ORDER BY Siparis DESC;

-- 9) Tekrar alışveriş: müşteri başına sipariş sayısı dağılımı
SELECT Bucket, COUNT(*) AS Musteri
FROM (
    SELECT CASE WHEN n = 1 THEN '1' WHEN n <= 5 THEN '2-5' WHEN n <= 20 THEN '6-20'
                WHEN n <= 50 THEN '21-50' ELSE '50+' END AS Bucket,
           CASE WHEN n = 1 THEN 1 WHEN n <= 5 THEN 2 WHEN n <= 20 THEN 3 WHEN n <= 50 THEN 4 ELSE 5 END AS Sira
    FROM (SELECT CustomerId, COUNT(*) AS n FROM dbo.Orders GROUP BY CustomerId) x
) b
GROUP BY Bucket, Sira
ORDER BY Sira;
GO
