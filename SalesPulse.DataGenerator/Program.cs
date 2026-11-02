using System.Diagnostics;
using System.Text;
using SalesPulse.DataGenerator;
using SalesPulse.DataGenerator.Generators;

Console.OutputEncoding = Encoding.UTF8; // Türkçe karakterler için

// --dry-run: veritabanına yazmadan sadece üretimi test et
bool dryRun = args.Contains("--dry-run");

Console.WriteLine("=== SalesPulse Veri Üretici ===");
Console.WriteLine(dryRun ? "Mod: DRY-RUN (veritabanına yazılmayacak)" : $"Hedef: {Settings.ConnectionString}");
Console.WriteLine();

var random = new Random(Settings.Seed);
var writer = new BulkWriter(Settings.ConnectionString, dryRun);

// Veri iki kez basılmasın: müşteri/sipariş tabloları doluysa dur
if (!writer.IsEmpty("Customers") || !writer.IsEmpty("Orders"))
{
    Console.WriteLine("HATA: Müşteri/sipariş tablolarında zaten veri var.");
    Console.WriteLine("Baştan üretmek için önce SSMS'te database/01_tables.sql dosyasını çalıştır.");
    return;
}

// Katalog daha önce yazıldıysa tekrar yazılmaz; aynı seed ile bellekte yeniden kurulur
bool catalogExists = !writer.IsEmpty("Cities");
if (catalogExists)
{
    Console.WriteLine("Katalog veritabanında zaten var: yeniden yazılmayacak, sadece bellekte kurulacak.");
    writer.SkipTables.UnionWith(new[] { "Cities", "Categories", "Publishers", "Products" });
}

var stopwatch = Stopwatch.StartNew();

// ---------------------------------------------------------
// 1) KATALOG: şehir, kategori, yayınevi, ürün
// ---------------------------------------------------------
Console.WriteLine("[1/3] Katalog üretiliyor...");
var catalog = new CatalogGenerator(random, writer);
catalog.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// Bellekteki katalog veritabanındakiyle aynı mı? Değilse siparişler yanlış fiyatla oluşur → dur
if (catalogExists)
{
    var (dbCount, dbSum) = writer.ProductFingerprint();
    int memCount = catalog.Products.Count;
    decimal memSum = catalog.Products.Sum(p => p.Price);
    Console.WriteLine($"      Katalog kontrolü: DB {dbCount:N0} ürün / {dbSum:N2} TL, bellek {memCount:N0} ürün / {memSum:N2} TL");
    if (dbCount != memCount || dbSum != memSum)
    {
        Console.WriteLine("HATA: Veritabanındaki katalog bu kodun ürettiğiyle aynı değil.");
        Console.WriteLine("Önce SSMS'te database/01_tables.sql dosyasını çalıştırıp tabloları sıfırla.");
        return;
    }
}

// ---------------------------------------------------------
// 2) MÜŞTERİLER
// ---------------------------------------------------------
Console.WriteLine("[2/3] Müşteriler üretiliyor...");
var customers = new CustomerGenerator(random, writer);
customers.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// ---------------------------------------------------------
// 3) SİPARİŞLER + SİPARİŞ KALEMLERİ + ÖDEMELER
// ---------------------------------------------------------
Console.WriteLine("[3/3] Siparişler üretiliyor (Orders → OrderDetails → Payments)...");
var orders = new OrderGenerator(random, writer, catalog, customers);
orders.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// ---------------------------------------------------------
// Özet (sadece konsol raporu; dashboard raporları SQL'den gelir)
// ---------------------------------------------------------
var products = catalog.Products;
Console.WriteLine();
Console.WriteLine($"=== ÜRÜN ÖZETİ ({products.Count:N0} ürün) ===");
foreach (var group in products.GroupBy(p => p.Group))
    Console.WriteLine($"{group.Key,-20}: {group.Count(),5:N0} ürün, fiyat {group.Min(p => p.Price),9:N2} – {group.Max(p => p.Price),10:N2} TL");

string[] statusNames = { "", "Ön Sipariş", "Hazırlanıyor", "Kargoda", "Teslim Edildi", "İptal Edildi", "İade Edildi" };
string[] methodNames = { "", "Kredi Kartı", "Banka Kartı", "Havale / EFT", "Kapıda Ödeme", "Dijital Cüzdan" };
long totalOrders = Settings.OrderCount;

Console.WriteLine();
Console.WriteLine("=== SİPARİŞ ÖZETİ ===");
Console.WriteLine($"Sipariş        : {totalOrders:N0}");
Console.WriteLine($"Sipariş kalemi : {orders.DetailCount:N0} (sipariş başına {orders.DetailCount / (double)totalOrders:F2})");
Console.WriteLine($"Ödeme          : {totalOrders:N0}");

Console.WriteLine();
Console.WriteLine("Yıllara göre:");
foreach (var (year, y) in orders.ByYear.OrderBy(x => x.Key))
    Console.WriteLine($"  {year}: {y.Orders,9:N0} sipariş, {y.Revenue,18:N2} TL ciro, ort. sepet {y.Revenue / y.Orders,8:N2} TL");

Console.WriteLine();
Console.WriteLine("Durumlar:");
for (int s = 1; s <= 6; s++)
    Console.WriteLine($"  {statusNames[s],-14}: {orders.StatusCounts[s],9:N0} (%{100.0 * orders.StatusCounts[s] / totalOrders:F1})");

Console.WriteLine();
Console.WriteLine("Ödeme yöntemleri:");
for (int m = 1; m <= 5; m++)
    Console.WriteLine($"  {methodNames[m],-14}: {orders.PaymentCounts[m],9:N0} (%{100.0 * orders.PaymentCounts[m] / totalOrders:F1})");

var perCustomer = orders.OrdersPerCustomer;
int buyers = perCustomer.Count(c => c > 0);
int repeat = perCustomer.Count(c => c > 1);
Console.WriteLine();
Console.WriteLine("Müşteri davranışı:");
Console.WriteLine($"  Sipariş veren müşteri      : {buyers:N0} / {perCustomer.Length:N0}");
Console.WriteLine($"  Tekrar alışveriş yapan     : {repeat:N0} (%{100.0 * repeat / Math.Max(1, buyers):F1})");
Console.WriteLine($"  Tek siparişli              : {perCustomer.Count(c => c == 1):N0}");
Console.WriteLine($"  En çok sipariş veren       : {perCustomer.Max():N0} sipariş");
Console.WriteLine($"  Koleksiyoncu sipariş payı  : %{100.0 * orders.CollectorOrders / totalOrders:F1}");
Console.WriteLine($"  Ön siparişli ürün içeren   : {orders.PreOrderItemOrders:N0} sipariş");

Console.WriteLine();
Console.WriteLine($"Toplam süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");
if (!dryRun)
    Console.WriteLine("Sonraki adım: SSMS'te database/05_verify_data.sql ile kontrol et.");
