-- =============================================
-- SalesPulse - Nonclustered indeksler
-- Tekrar çalıştırılabilir. Her indeksin hangi sorguya hizmet ettiği yanında yazılı;
-- öncesi/sonrası ölçümler docs/performance altında.
-- =============================================
USE SalesPulseDb;
GO

-- Tarih aralığı filtresi: dashboard ve analiz sorgularının tamamı.
-- INCLUDE sayesinde tabloya geri dönmeden (key lookup olmadan) cevap verir.
-- Order Explorer'ın varsayılan "en yeni sipariş" sıralaması da bu indeksi geriye okur.
DROP INDEX IF EXISTS IX_Orders_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate
    ON dbo.Orders (OrderDate)
    INCLUDE (CustomerId, ShippingCityId, StatusId, TotalAmount);

-- Müşteri araması: müşterinin siparişleri tarih sırasıyla.
DROP INDEX IF EXISTS IX_Orders_CustomerId_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDate
    ON dbo.Orders (CustomerId, OrderDate)
    INCLUDE (ShippingCityId, StatusId, TotalAmount);

-- Order Explorer şehir filtresi.
DROP INDEX IF EXISTS IX_Orders_ShippingCityId_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_ShippingCityId_OrderDate
    ON dbo.Orders (ShippingCityId, OrderDate)
    INCLUDE (CustomerId, StatusId, TotalAmount);

-- Order Explorer durum filtresi ve üst bardaki bildirim sayıları.
DROP INDEX IF EXISTS IX_Orders_StatusId_OrderDate ON dbo.Orders;
CREATE NONCLUSTERED INDEX IX_Orders_StatusId_OrderDate
    ON dbo.Orders (StatusId, OrderDate)
    INCLUDE (CustomerId, ShippingCityId, TotalAmount);

-- Sipariş detayı ve kategori/ürün raporlarındaki Orders -> OrderDetails JOIN'i.
-- Bu indeks yokken tek siparişin kalemleri için 3,5M satır taranıyordu.
DROP INDEX IF EXISTS IX_OrderDetails_OrderId ON dbo.OrderDetails;
CREATE NONCLUSTERED INDEX IX_OrderDetails_OrderId
    ON dbo.OrderDetails (OrderId)
    INCLUDE (ProductId, Quantity, UnitPrice);

-- Her siparişin bir ödemesi var; UNIQUE bu kuralı veritabanında da garanti eder.
DROP INDEX IF EXISTS UX_Payments_OrderId ON dbo.Payments;
CREATE UNIQUE NONCLUSTERED INDEX UX_Payments_OrderId
    ON dbo.Payments (OrderId)
    INCLUDE (PaymentMethodId, InstallmentCount, PaymentDate);

-- Müşteri adı araması (ön ek: "Ayşe", "Yılmaz", "Ayşe Yıl").
DROP INDEX IF EXISTS IX_Customers_FirstName_LastName ON dbo.Customers;
CREATE NONCLUSTERED INDEX IX_Customers_FirstName_LastName
    ON dbo.Customers (FirstName, LastName);

DROP INDEX IF EXISTS IX_Customers_LastName ON dbo.Customers;
CREATE NONCLUSTERED INDEX IX_Customers_LastName
    ON dbo.Customers (LastName);
GO
