-- =============================================
-- SalesPulse - Tablolar, PK/FK ilişkileri ve sabit (lookup) veriler
--
-- NOT: Bu dosyada bilerek NONCLUSTERED INDEX yok.
-- İndeksler 02_indexes.sql dosyasında; önce indekssiz performans ölçülecek.
-- =============================================
USE SalesPulseDb;
GO

-- ---------------------------------------------
-- 0) Temizlik (geliştirme aşaması için)
-- !!! DİKKAT: Veri basıldıktan sonra bu dosyayı tekrar çalıştırırsan
-- !!! TÜM VERİ SİLİNİR. Silme sırası FK bağımlılığına göre: önce çocuklar.
-- ---------------------------------------------
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.OrderDetails;
DROP TABLE IF EXISTS dbo.Orders;
DROP TABLE IF EXISTS dbo.Customers;
DROP TABLE IF EXISTS dbo.Products;
DROP TABLE IF EXISTS dbo.Publishers;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Cities;
DROP TABLE IF EXISTS dbo.PaymentMethods;
DROP TABLE IF EXISTS dbo.OrderStatuses;
GO

-- ---------------------------------------------
-- 1) Sabit (lookup) tablolar
-- ---------------------------------------------
CREATE TABLE dbo.OrderStatuses (
    StatusId    TINYINT       NOT NULL,
    Name        NVARCHAR(30)  NOT NULL,
    CONSTRAINT PK_OrderStatuses PRIMARY KEY (StatusId)
);

CREATE TABLE dbo.PaymentMethods (
    PaymentMethodId TINYINT       NOT NULL,
    Name            NVARCHAR(30)  NOT NULL,
    CONSTRAINT PK_PaymentMethods PRIMARY KEY (PaymentMethodId)
);

-- CityId = plaka kodu (İstanbul = 34). Bu yüzden IDENTITY değil.
CREATE TABLE dbo.Cities (
    CityId  TINYINT       NOT NULL,
    Name    NVARCHAR(50)  NOT NULL,
    Region  NVARCHAR(30)  NOT NULL,   -- Marmara, Ege, İç Anadolu ...
    CONSTRAINT PK_Cities PRIMARY KEY (CityId)
);
GO

-- ---------------------------------------------
-- 2) Katalog: kategori, yayınevi/üretici, ürün
-- ---------------------------------------------
-- Ana grup satırlarında ParentCategoryId = NULL (ör. "Çizgi Roman")
-- Alt kategoriler ana gruba işaret eder (ör. "Manga" -> "Çizgi Roman")
CREATE TABLE dbo.Categories (
    CategoryId        INT IDENTITY(1,1) NOT NULL,
    Name              NVARCHAR(100)     NOT NULL,
    ParentCategoryId  INT               NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentCategoryId)
        REFERENCES dbo.Categories (CategoryId)
);

CREATE TABLE dbo.Publishers (
    PublisherId  INT IDENTITY(1,1) NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
    CONSTRAINT PK_Publishers PRIMARY KEY (PublisherId)
);

CREATE TABLE dbo.Products (
    ProductId         INT IDENTITY(1,1) NOT NULL,
    CategoryId        INT               NOT NULL,   -- her zaman alt kategori
    PublisherId       INT               NULL,       -- aksesuarlarda olmayabilir
    Name              NVARCHAR(200)     NOT NULL,
    Price             DECIMAL(10,2)     NOT NULL,
    ReleaseDate       DATE              NOT NULL,   -- ön sipariş mantığı için
    IsLimitedEdition  BIT               NOT NULL CONSTRAINT DF_Products_IsLimitedEdition DEFAULT (0),
    IsActive          BIT               NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CONSTRAINT PK_Products PRIMARY KEY (ProductId),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT FK_Products_Publishers FOREIGN KEY (PublisherId)
        REFERENCES dbo.Publishers (PublisherId),
    CONSTRAINT CK_Products_Price CHECK (Price > 0)
);
GO

-- ---------------------------------------------
-- 3) Müşteriler
-- ---------------------------------------------
CREATE TABLE dbo.Customers (
    CustomerId   INT IDENTITY(1,1) NOT NULL,
    FirstName    NVARCHAR(50)      NOT NULL,
    LastName     NVARCHAR(50)      NOT NULL,
    Email        NVARCHAR(150)     NOT NULL,
    CityId       TINYINT           NOT NULL,
    CreatedDate  DATETIME2(0)      NOT NULL,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerId),
    CONSTRAINT UQ_Customers_Email UNIQUE (Email),
    CONSTRAINT FK_Customers_Cities FOREIGN KEY (CityId)
        REFERENCES dbo.Cities (CityId)
);
GO

-- ---------------------------------------------
-- 4) Siparişler (2.000.000+ satır)
-- ShippingCityId ve TotalAmount bilinçli denormalizasyon:
-- raporlarda Customers/OrderDetails JOIN'ini gereksiz kılar.
-- ---------------------------------------------
CREATE TABLE dbo.Orders (
    OrderId         INT IDENTITY(1,1) NOT NULL,
    CustomerId      INT               NOT NULL,
    ShippingCityId  TINYINT           NOT NULL,
    OrderDate       DATETIME2(0)      NOT NULL,
    StatusId        TINYINT           NOT NULL,
    TotalAmount     DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (OrderId),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Orders_Cities FOREIGN KEY (ShippingCityId)
        REFERENCES dbo.Cities (CityId),
    CONSTRAINT FK_Orders_OrderStatuses FOREIGN KEY (StatusId)
        REFERENCES dbo.OrderStatuses (StatusId),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);

-- UnitPrice: satış anındaki fiyat (ürün fiyatı sonradan değişebilir)
CREATE TABLE dbo.OrderDetails (
    OrderDetailId  INT IDENTITY(1,1) NOT NULL,
    OrderId        INT               NOT NULL,
    ProductId      INT               NOT NULL,
    Quantity       SMALLINT          NOT NULL,
    UnitPrice      DECIMAL(10,2)     NOT NULL,
    CONSTRAINT PK_OrderDetails PRIMARY KEY (OrderDetailId),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders (OrderId),
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0)
);

-- Her siparişin bir ödemesi var (1-1). Bunu veri üretici garanti eder.
CREATE TABLE dbo.Payments (
    PaymentId         INT IDENTITY(1,1) NOT NULL,
    OrderId           INT               NOT NULL,
    PaymentMethodId   TINYINT           NOT NULL,
    Amount            DECIMAL(12,2)     NOT NULL,
    InstallmentCount  TINYINT           NOT NULL CONSTRAINT DF_Payments_InstallmentCount DEFAULT (1),
    PaymentDate       DATETIME2(0)      NOT NULL,
    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders (OrderId),
    CONSTRAINT FK_Payments_PaymentMethods FOREIGN KEY (PaymentMethodId)
        REFERENCES dbo.PaymentMethods (PaymentMethodId)
);
GO

-- ---------------------------------------------
-- 5) Sabit verilerin eklenmesi
-- (Şehir, kategori, yayınevi, ürün... veri üreticiden gelecek)
-- ---------------------------------------------
INSERT INTO dbo.OrderStatuses (StatusId, Name) VALUES
    (1, N'Ön Sipariş'),
    (2, N'Hazırlanıyor'),
    (3, N'Kargoda'),
    (4, N'Teslim Edildi'),
    (5, N'İptal Edildi'),
    (6, N'İade Edildi');

INSERT INTO dbo.PaymentMethods (PaymentMethodId, Name) VALUES
    (1, N'Kredi Kartı'),
    (2, N'Banka Kartı'),
    (3, N'Havale / EFT'),
    (4, N'Kapıda Ödeme'),
    (5, N'Dijital Cüzdan');
GO
