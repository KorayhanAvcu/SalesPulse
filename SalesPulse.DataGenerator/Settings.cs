namespace SalesPulse.DataGenerator;

// Veri üretiminin bütün "ayar düğmeleri" burada.
// README'deki "Veri üretim yöntemi" bölümü bu dosyadan yazılabilir.
public static class Settings
{
    // Aynı seed = her çalıştırmada birebir aynı veri
    public const int Seed = 2024;

    // Veritabanı bağlantısı (Web/appsettings.json ile aynı)
    // Kendi bilgisayarında çalıştıran kişi Server kısmını değiştirmeli.
    public const string ConnectionString =
        "Server=DESKTOP-NDMQ7CM\\SQLEXPRESS;Database=SalesPulseDb;Integrated Security=True;TrustServerCertificate=True";

    // ---- Hacimler ----
    // ~1.200 ürün gerçek katalogdan (seriler, figürler, oyunlar), kalanı kurgusal
    public const int ProductCount  = 2_000;
    public const int CustomerCount = 100_000;
    public const int OrderCount    = 2_000_000;
    public const int BatchSize     = 50_000;   // SqlBulkCopy parti büyüklüğü

    // ---- Tarih aralığı ----
    public static readonly DateTime StartDate = new(2024, 1, 1);
    public static readonly DateTime EndDate   = new(2026, 10, 31, 23, 59, 59);

    // ---- Sezon etkisi: ay bazında sipariş yoğunluğu çarpanı ----
    // Index 0 = Ocak ... 11 = Aralık
    public static readonly double[] MonthFactors =
    {
        0.75, // Ocak     - yılbaşı sonrası durgunluk
        0.70, // Şubat
        0.85, // Mart
        0.90, // Nisan
        0.95, // Mayıs
        1.10, // Haziran  - yaz tatili
        1.10, // Temmuz
        1.05, // Ağustos
        0.95, // Eylül    - okul dönüşü
        1.00, // Ekim
        1.80, // Kasım    - Black Friday
        1.60  // Aralık   - yılbaşı hediyeleri
    };

    // Yıllık büyüme: 2025, 2024'ten %20; 2026, 2025'ten %20 daha yoğun
    public const double YearlyGrowth = 1.20;

    // Hafta sonu siparişleri biraz daha fazla
    public const double WeekendFactor = 1.15;

    // Yaz aylarında (Haz-Ağu) "SummerBoost" kategorilerinin satış çarpanı
    public const double SummerCategoryBoost = 1.6;

    // ---- Müşteri davranışı ----
    // Müşterilerin %20'si "koleksiyoncu": siparişlerin ~%60'ını verir
    public const double CollectorRatio      = 0.20;
    public const double CollectorOrderShare = 0.60;

    // %25'i tek seferlik: sadece 1 sipariş verir (tekrar alışveriş oranı anlamlı olsun)
    public const double OneTimeRatio = 0.25;

    // Üstel seçim: 1'den büyükse eski müşteriler daha sık alışveriş eder
    // (1,5 gibi değerlerde en eski müşteri 2.000+ sipariş veriyor, gerçekçi değil)
    public const double LoyaltySkew = 1.15;

    // Müşterilerin %30'u veri döneminden önce kayıt olmuş (ilk günden sipariş olsun diye)
    public static readonly DateTime CustomerSignupStart = new(2022, 1, 1);
    public const double PreStartCustomerShare = 0.30;

    // Siparişlerin %8'i başka bir şehre gider (hediye, iş adresi...)
    public const double OtherCityShippingRate = 0.08;

    // ---- Ürün satış dönemi ----
    public const int    PreOrderWindowDays = 90;   // çıkıştan en fazla 90 gün önce ön sipariş açılır
    public const double PreOrderFactor     = 0.5;  // ön siparişteki ürün daha az satar
    public const int    NewReleaseDays     = 60;   // çıkış sonrası "yeni ürün" dönemi
    public const double NewReleaseBoost    = 3.0;  // yeni çıkan ürün daha çok satar

    // Pasif ürünler bu tarihten sonra satılmıyor
    public static readonly DateTime InactiveCutoff = new(2025, 1, 1);

    // ---- Fiyat ----
    // Products.Price bugünkü fiyat; geçmişte yıllık %25 enflasyon kadar ucuzdu
    public const double YearlyPriceIncrease = 1.25;
    public const double NovemberDiscount    = 0.10; // Black Friday indirimi

    // ---- Sipariş saati: 0-23 arası yoğunluk (gece düşük, akşam tepe) ----
    public static readonly double[] HourWeights =
    {
        1.0, 0.6, 0.3, 0.2, 0.2, 0.3, 0.6, 1.2,
        2.5, 3.5, 4.5, 5.0, 5.5, 5.0, 5.0, 5.0,
        5.5, 6.0, 6.5, 7.5, 8.5, 9.0, 7.0, 3.0
    };

    // Kalem başına adet
    public static readonly (short Quantity, double Weight)[] Quantities =
    {
        (1, 80), (2, 15), (3, 5)
    };

    // ---- Sepet: siparişteki ürün çeşidi sayısı (ortalama ~1,75) ----
    public static readonly (int Lines, double Weight)[] BasketSizes =
    {
        (1, 50), (2, 30), (3, 15), (4, 5)
    };

    // ---- Ödeme yöntemleri (Id'ler 01_tables.sql ile aynı) ----
    public static readonly (byte PaymentMethodId, double Weight)[] PaymentMethods =
    {
        (1, 55), // Kredi Kartı
        (2, 20), // Banka Kartı
        (3,  7), // Havale / EFT
        (4,  8), // Kapıda Ödeme
        (5, 10)  // Dijital Cüzdan
    };

    // Taksit sadece kredi kartında ve bu tutarın üzerindeki siparişlerde
    public const decimal InstallmentMinAmount = 1_000m;

    public static readonly (byte Count, double Weight)[] Installments =
    {
        (1, 35), (3, 30), (6, 20), (9, 10), (12, 5)
    };

    // Havale ödemesi en geç 24 saatte gelir, kapıda ödeme teslimatta alınır
    public const int TransferMaxDelayHours = 24;
    public const int CashOnDeliveryDays    = 3;

    public const byte PaymentCreditCard     = 1;
    public const byte PaymentBankTransfer   = 3;
    public const byte PaymentCashOnDelivery = 4;

    // ---- Sipariş durumları (Id'ler 01_tables.sql ile aynı) ----
    public const byte StatusPreOrder  = 1; // Ön Sipariş
    public const byte StatusPreparing = 2; // Hazırlanıyor
    public const byte StatusShipped   = 3; // Kargoda
    public const byte StatusDelivered = 4; // Teslim Edildi
    public const byte StatusCancelled = 5; // İptal Edildi
    public const byte StatusReturned  = 6; // İade Edildi

    public const double CancelRate = 0.04;
    public const double ReturnRate = 0.03;
}
