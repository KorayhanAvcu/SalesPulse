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
// 2) MÜŞTERİLER  (sonraki adım)
// 3) SİPARİŞLER  (sonraki adım)
// ---------------------------------------------------------

// ---------------------------------------------------------
// Özet
// ---------------------------------------------------------
var products = catalog.Products;
Console.WriteLine();
Console.WriteLine($"=== ÜRÜN ÖZETİ ({products.Count:N0} ürün) ===");
foreach (var group in products.GroupBy(p => p.Group))
    Console.WriteLine($"{group.Key,-20}: {group.Count(),5:N0} ürün, fiyat {group.Min(p => p.Price),9:N2} – {group.Max(p => p.Price),10:N2} TL");

Console.WriteLine();
Console.WriteLine($"Sınırlı üretim : {products.Count(p => p.IsLimitedEdition):N0}");
Console.WriteLine($"Pasif ürün     : {products.Count(p => !p.IsActive):N0}");
Console.WriteLine($"Markasız ürün  : {products.Count(p => p.PublisherId is null):N0}");
Console.WriteLine($"Veri dönemi sonrası çıkacak (ön sipariş adayı): {products.Count(p => p.ReleaseDate > Settings.EndDate):N0}");

Console.WriteLine();
Console.WriteLine("=== RASTGELE 12 ÜRÜN ===");
var sampleRandom = new Random(1);
foreach (var p in products.OrderBy(_ => sampleRandom.Next()).Take(12))
    Console.WriteLine($"{p.Price,9:N2} TL | {p.ReleaseDate:yyyy-MM-dd} | {p.Name}");

Console.WriteLine();
Console.WriteLine($"Toplam süre: {stopwatch.Elapsed.TotalSeconds:F1} sn");
