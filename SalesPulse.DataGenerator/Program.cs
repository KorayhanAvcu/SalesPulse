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

// Veri iki kez basılmasın: tablolar boş değilse dur
if (!writer.IsEmpty("Cities"))
{
    Console.WriteLine("HATA: Tablolarda zaten veri var.");
    Console.WriteLine("Baştan üretmek için önce SSMS'te database/01_tables.sql dosyasını çalıştır.");
    return;
}

var stopwatch = Stopwatch.StartNew();

// ---------------------------------------------------------
// 1) KATALOG: şehir, kategori, yayınevi, ürün
// ---------------------------------------------------------
Console.WriteLine("[1/3] Katalog üretiliyor...");
var catalog = new CatalogGenerator(random, writer);
catalog.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// ---------------------------------------------------------
// 2) MÜŞTERİLER
// ---------------------------------------------------------
Console.WriteLine("[2/3] Müşteriler üretiliyor...");
var customers = new CustomerGenerator(random, writer);
customers.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// ---------------------------------------------------------
// 3) SİPARİŞLER: sipariş + kalem + ödeme
// ---------------------------------------------------------
Console.WriteLine("[3/3] Siparişler üretiliyor...");
var orders = new OrderGenerator(random, writer, catalog.Products, customers.Customers);
orders.Run();
Console.WriteLine($"      Süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");

// ---------------------------------------------------------
// Özet
// ---------------------------------------------------------
var products = catalog.Products;
Console.WriteLine();
Console.WriteLine($"=== ÜRÜN ÖZETİ ({products.Count:N0} ürün) ===");
foreach (var group in products.GroupBy(p => p.Group))
    Console.WriteLine($"{group.Key,-20}: {group.Count(),5:N0} ürün, fiyat {group.Min(p => p.Price),9:N2} – {group.Max(p => p.Price),10:N2} TL");

Console.WriteLine();
Console.WriteLine($"=== SİPARİŞ ÖZETİ ===");
Console.WriteLine($"Müşteri        : {customers.Customers.Count,12:N0}");
Console.WriteLine($"Sipariş        : {orders.OrderCount,12:N0}");
Console.WriteLine($"Sipariş kalemi : {orders.DetailCount,12:N0}");
Console.WriteLine($"Toplam tutar   : {orders.TotalRevenue,12:N0} TL");
Console.WriteLine($"Ortalama sepet : {orders.TotalRevenue / orders.OrderCount,12:N2} TL");
foreach (var status in orders.StatusCounts.OrderBy(s => s.Key))
    Console.WriteLine($"Durum {status.Key}        : {status.Value,12:N0}");

Console.WriteLine();
Console.WriteLine($"Toplam süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");
