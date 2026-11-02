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
        "Server=KORAYHAN\\SQLEXPRESS;Database=SalesPulseDb;Integrated Security=True;TrustServerCertificate=True";

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

    // ---- Sipariş durumları (Id'ler 01_tables.sql ile aynı) ----
    public const byte StatusPreOrder  = 1; // Ön Sipariş
    public const byte StatusPreparing = 2; // Hazırlanıyor
    public const byte StatusShipped   = 3; // Kargoda
    public const byte StatusDelivered = 4; // Teslim Edildi
    public const byte StatusCancelled = 5; // İptal Edildi
    public const byte StatusReturned  = 6; // İade Edildi

    public const double CancelRate = 0.04;
    public const double ReturnRate = 0.03;

    // Sipariş tarihine göre durum (veri dönemi sonuna, EndDate'e uzaklık)
    public const int PreparingDays = 2;   // son 2 gün: çoğu "Hazırlanıyor"
    public const int ShippingDays  = 5;   // 2-5 gün arası: çoğu "Kargoda"

    // ---- Müşteri kayıt tarihleri ----
    // %30'u veri döneminden önce (2022-2023) kayıt olmuş "eski" müşteri,
    // kalanlar dönem boyunca katılıyor (yıllık büyüme ile).
    public static readonly DateTime CustomerHistoryStart = new(2022, 1, 1);
    public const double ExistingCustomerRatio = 0.30;

    // Siparişlerin %8'i müşterinin kendi şehrinden farklı bir adrese gider (hediye, iş yeri...)
    public const double OtherShippingCityRate = 0.08;

    // ---- Ürün seçimi ----
    // Çıkış tarihinden en fazla bu kadar gün önce ön sipariş alınabilir
    public const int PreOrderWindowDays = 45;

    // Pasif ürünler bu tarihten sonra satılmaz
    public static readonly DateTime InactiveProductCutoff = new(2025, 1, 1);

    // ---- Fiyat ----
    // Ürün fiyatları "bugünkü" (EndDate) fiyat. Geçmişteki satış fiyatı
    // yıllık enflasyon kadar düşük: 1 yıl önce = fiyat / 1,35
    public const double YearlyPriceInflation = 1.35;

    // Kasım indirimleri (Black Friday haftası): siparişlerin %50'sinde %10-25 indirim
    public const double BlackFridayDiscountChance = 0.50;

    // ---- Taksit seçenekleri (sadece kredi kartı, InstallmentMinAmount üstü) ----
    public static readonly (byte Count, double Weight)[] Installments =
    {
        (1, 45), (2, 10), (3, 20), (6, 15), (9, 5), (12, 5)
    };

    // Kapıda ödeme bu tutarın üzerinde kabul edilmez
    public const decimal CashOnDeliveryMaxAmount = 5_000m;

    // ---- Saat dağılımı (0-23): gece düşük, akşam 20-22 zirve ----
    public static readonly double[] HourFactors =
    {
        1.2, 0.7, 0.4, 0.3, 0.2, 0.2, 0.4, 0.8,   // 00-07
        1.5, 2.5, 3.5, 4.0, 4.5, 4.5, 4.2, 4.2,   // 08-15
        4.5, 5.0, 5.5, 6.0, 7.0, 7.5, 6.5, 3.5    // 16-23
    };
}
