using System.Data;

namespace SalesPulse.DataGenerator.Generators;

// Üretilen her ürünün bellekteki hali.
// Weight: sipariş üretirken bu ürünün ne sıklıkla seçileceği (çok satanlar büyük).
public record ProductRow(
    int ProductId,
    int CategoryId,
    string SubCategory,
    string Group,
    int? PublisherId,
    string Name,
    decimal Price,
    DateTime ReleaseDate,
    bool IsLimitedEdition,
    bool IsActive,
    double Weight);

// Şehir, kategori, yayınevi ve ürünleri üretip veritabanına yazar.
public class CatalogGenerator
{
    private readonly Random _random;
    private readonly BulkWriter _writer;

    // Sonraki adımlar (müşteri, sipariş) bunları kullanacak
    public Dictionary<string, int> SubCategoryIds { get; } = new();
    public Dictionary<string, int> PublisherIds { get; } = new();
    public List<ProductRow> Products { get; } = new();

    // Sürüme göre fiyat çarpanı (bilinmeyen sürüm = 1.0)
    private static readonly Dictionary<string, decimal> VariantPriceFactor = new()
    {
        ["Deluxe Set"] = 1.5m, ["2'li Paket"] = 1.8m, ["DX Sürüm"] = 1.4m,
        ["Özel Renk Sürümü"] = 1.2m, ["Işıklı Sürüm"] = 1.3m, ["Deluxe Sürüm"] = 1.3m,
        ["Steelbook Sürüm"] = 1.2m, ["PlayStation Hits"] = 0.6m, ["Essentials"] = 0.6m,
        ["Platinum"] = 0.6m, ["İkinci El"] = 0.55m, ["İkinci El Kartuş"] = 0.55m,
        ["Kutulu (CIB)"] = 1.6m, ["Yenilenmiş"] = 0.8m, ["Kutulu Koleksiyonluk"] = 1.5m
    };

    // Sınırlı üretim sayılan sürümler
    private static readonly HashSet<string> LimitedVariants = new()
    {
        "Deluxe Set", "DX Sürüm", "Özel Renk Sürümü", "Deluxe Sürüm",
        "Steelbook Sürüm", "Kutulu (CIB)", "Kutulu Koleksiyonluk"
    };

    // Gerçek çıkış tarihleri (2024 ve sonrası çıkanlar satış grafiğinde sıçrama yapar)
    private static readonly Dictionary<string, DateTime> GameReleaseDates = new()
    {
        ["Demon's Souls"] = new(2020, 11, 12),
        ["Ratchet & Clank: Rift Apart"] = new(2021, 6, 11),
        ["Returnal"] = new(2021, 4, 30),
        ["Marvel's Spider-Man 2"] = new(2023, 10, 20),
        ["Astro Bot"] = new(2024, 9, 6),
        ["Stellar Blade"] = new(2024, 4, 26),
        ["Rise of the Ronin"] = new(2024, 3, 22),
        ["Ghost of Yōtei"] = new(2025, 10, 2),
        ["Death Stranding 2: On the Beach"] = new(2025, 6, 26),
        ["God of War Ragnarök"] = new(2022, 11, 9),
        ["Horizon Forbidden West"] = new(2022, 2, 18),
        ["Gran Turismo 7"] = new(2022, 3, 4),
    };

    public CatalogGenerator(Random random, BulkWriter writer)
    {
        _random = random;
        _writer = writer;
    }

    public void Run()
    {
        WriteCities();
        WriteCategories();
        WritePublishers();

        BuildSeriesProducts();     // manga & çizgi roman ciltleri + setler
        BuildCatalogProducts();    // figür, oyun, özel edisyon
        BuildFictionalProducts();  // kalan ürünler (kutu oyunu, giyim, mini figür)
        WriteProducts();
    }

    // =====================================================================
    // 1) ŞEHİRLER
    // =====================================================================
    private void WriteCities()
    {
        var table = new DataTable("Cities");
        table.Columns.Add("CityId", typeof(byte));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Region", typeof(string));

        foreach (var city in ReferenceData.Cities)
            table.Rows.Add(city.Id, city.Name, city.Region);

        _writer.Write(table);
    }

    // =====================================================================
    // 2) KATEGORİLER: önce 6 ana grup (Id 1-6), sonra alt kategoriler
    // =====================================================================
    private void WriteCategories()
    {
        var table = new DataTable("Categories");
        table.Columns.Add("CategoryId", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("ParentCategoryId", typeof(int));

        int id = 0;
        var groupIds = new Dictionary<string, int>();

        foreach (var group in ReferenceData.MainGroups)
        {
            groupIds[group] = ++id;
            table.Rows.Add(id, group, DBNull.Value);   // ana grubun parent'ı yok
        }

        foreach (var sub in ReferenceData.SubCategories)
        {
            SubCategoryIds[sub.Name] = ++id;
            table.Rows.Add(id, sub.Name, groupIds[sub.Group]);
        }

        _writer.Write(table);
    }

    // =====================================================================
    // 3) YAYINEVİ / MARKALAR
    // =====================================================================
    private void WritePublishers()
    {
        var table = new DataTable("Publishers");
        table.Columns.Add("PublisherId", typeof(int));
        table.Columns.Add("Name", typeof(string));

        int id = 0;
        foreach (var name in ReferenceData.Publishers)
        {
            PublisherIds[name] = ++id;
            table.Rows.Add(id, name);
        }

        _writer.Write(table);
    }

    // =====================================================================
    // 4a) SERİLER: her cilt ayrı ürün + varyant kapak + kutulu set
    // =====================================================================
    private void BuildSeriesProducts()
    {
        foreach (var series in ReferenceData.Series)
        {
            var sub = FindSub(series.SubCategory);
            decimal basePrice = RandomPrice(sub.MinPrice, sub.MaxPrice);
            double seriesWeight = PopularityWeight();

            // Son cilt 2025-2026 arasında çıkıyor, öncekiler ~2-3 ay arayla geriye doğru.
            // Böylece veri döneminde yeni ciltler çıkar; bazıları ise ön siparişe düşer.
            DateTime lastRelease = RandomDate(new DateTime(2025, 3, 1), new DateTime(2026, 12, 31));
            int interval = _random.Next(60, 101);

            for (int volume = 1; volume <= series.Volumes; volume++)
            {
                DateTime release = lastRelease.AddDays(-(series.Volumes - volume) * interval);
                decimal price = RoundPrice(basePrice * (1 + 0.01m * (volume / 5))); // ileriki ciltler biraz pahalı
                double weight = seriesWeight * (volume == 1 ? 2.0 : 1.0);           // 1. cilt yeni okur çeker

                AddProduct(sub, series.Publisher, VolumeName(series, volume), price, release, false, weight);

                if (series.HasVariantCover)
                    AddProduct(sub, series.Publisher, VolumeName(series, volume) + " (Varyant Kapak)",
                        RoundPrice(price * 1.4m), release, true, weight * 0.3);
            }

            // 5+ ciltli seriler için kutulu set
            if (series.Volumes >= 5)
            {
                var setSub = FindSub("Set & Kutulu Seri");
                string unit = series.VolumeLabel is "Sayı" or "#" ? "Fasikül" : "Kitap";
                string range = series.VolumeLabel is "Sayı" or "#" ? "Sayı" : "Cilt";
                string name = $"{series.Name} {range} 1-{series.Volumes} Set ({series.Volumes} Ayrı {unit})";
                decimal setPrice = RoundPrice(basePrice * series.Volumes * 0.85m);  // set indirimi %15

                AddProduct(setSub, series.Publisher, name, setPrice, lastRelease.AddDays(30), true, seriesWeight * 0.5);
            }
        }
    }

    // "Naruto Cilt 12", "King Conan #3", "Spawn Klasik Seri 8", tek kitapsa "Kurt"
    private static string VolumeName(SeriesEntry series, int volume) => series.VolumeLabel switch
    {
        "" when series.Volumes == 1 => series.Name,
        ""  => $"{series.Name} {volume}",
        "#" => $"{series.Name} #{volume}",
        _   => $"{series.Name} {series.VolumeLabel} {volume}"
    };

    // =====================================================================
    // 4b) GERÇEK KATALOG: figürler, oyunlar, özel edisyonlar (sürümleriyle)
    // =====================================================================
    private void BuildCatalogProducts()
    {
        var entries = ReferenceData.Figures
            .Concat(ReferenceData.Games)
            .Concat(ReferenceData.SpecialEditions);

        foreach (var entry in entries)
        {
            var sub = FindSub(entry.SubCategory);
            decimal basePrice = RandomPrice(sub.MinPrice, sub.MaxPrice);
            double entryWeight = PopularityWeight();
            DateTime release = CatalogReleaseDate(entry, sub);
            string[] variants = sub.Variants ?? new[] { "" };

            for (int i = 0; i < variants.Length; i++)
            {
                string variant = variants[i];
                string name = sub.Group switch
                {
                    ReferenceData.GroupFigures => $"{entry.Publisher} {entry.Name}" + (variant == "" ? "" : $" ({variant})"),
                    ReferenceData.GroupVideo   => $"{entry.Name}{(sub.Suffix == "" ? "" : " " + sub.Suffix)} - {variant}",
                    _                          => entry.Name
                };

                decimal factor = VariantPriceFactor.GetValueOrDefault(variant, 1.0m);
                bool limited = LimitedVariants.Contains(variant)
                            || sub.Name is "1/6 Ölçek Koleksiyon Figürü" or "Özel Edisyon";
                double weight = entryWeight * (i == 0 ? 1.0 : 0.4); // ana sürüm daha çok satar

                AddProduct(sub, entry.Publisher, name, RoundPrice(basePrice * factor), release, limited, weight);
            }
        }
    }

    private DateTime CatalogReleaseDate(CatalogEntry entry, SubCategoryInfo sub)
    {
        if (GameReleaseDates.TryGetValue(entry.Name, out var date))
            return date;

        return sub.Name switch
        {
            "PS5 Oyunu"            => RandomDate(new(2020, 11, 12), new(2023, 12, 31)),
            "PS4 Oyunu"            => RandomDate(new(2014, 1, 1),   new(2020, 12, 31)),
            "PS3 Oyunu"            => RandomDate(new(2007, 1, 1),   new(2013, 12, 31)),
            "PSP Oyunu"            => RandomDate(new(2005, 9, 1),   new(2011, 12, 31)),
            "Game Boy / GBA Oyunu" => RandomDate(new(1990, 1, 1),   new(2005, 12, 31)),
            "Retro El Konsolu"     => RandomDate(new(1990, 1, 1),   new(2009, 12, 31)),
            // figürler ve özel edisyonlar: bir kısmı veri döneminde/sonrasında çıkar
            _                      => RandomDate(new(2018, 1, 1),   new(2026, 12, 31))
        };
    }

    // =====================================================================
    // 4c) KURGUSAL ÜRÜNLER: kalan sayıyı popülerliğe göre paylaştır
    // =====================================================================
    private void BuildFictionalProducts()
    {
        int remaining = Math.Max(0, Settings.ProductCount - Products.Count);
        var subs = ReferenceData.SubCategories
            .Where(s => s.Style is NameStyle.Item or NameStyle.Subtitle)
            .ToList();
        double totalPopularity = subs.Sum(s => s.Popularity);
        var usedNames = new HashSet<string>();

        for (int i = 0; i < subs.Count; i++)
        {
            var sub = subs[i];
            int count = i == subs.Count - 1
                ? remaining - (int)subs.Take(i).Sum(s => Math.Round(remaining * s.Popularity / totalPopularity))
                : (int)Math.Round(remaining * sub.Popularity / totalPopularity);

            for (int n = 0; n < count; n++)
            {
                string name = UniqueFictionalName(sub, usedNames);
                string? publisher = sub.Publishers.Length == 0 ? null : Pick(sub.Publishers);

                AddProduct(sub, publisher, name,
                    RandomPrice(sub.MinPrice, sub.MaxPrice),
                    RandomDate(new(2019, 1, 1), new(2026, 12, 31)),
                    _random.NextDouble() < 0.02,
                    PopularityWeight());
            }
        }
    }

    private string UniqueFictionalName(SubCategoryInfo sub, HashSet<string> usedNames)
    {
        string name = "";
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string series = $"{Pick(ReferenceData.SeriesPrefixes)} {Pick(ReferenceData.SeriesNouns)}";
            name = sub.Style == NameStyle.Subtitle
                ? $"{series}: {Pick(ReferenceData.Subtitles)} {sub.Suffix}"
                : $"{series} {sub.Suffix}";
            if (usedNames.Add(name)) break;
        }
        return name;
    }

    // =====================================================================
    // 5) ÜRÜNLERİ YAZ
    // =====================================================================
    private void WriteProducts()
    {
        var table = new DataTable("Products");
        table.Columns.Add("ProductId", typeof(int));
        table.Columns.Add("CategoryId", typeof(int));
        table.Columns.Add("PublisherId", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Price", typeof(decimal));
        table.Columns.Add("ReleaseDate", typeof(DateTime));
        table.Columns.Add("IsLimitedEdition", typeof(bool));
        table.Columns.Add("IsActive", typeof(bool));

        foreach (var p in Products)
            table.Rows.Add(p.ProductId, p.CategoryId,
                p.PublisherId.HasValue ? p.PublisherId.Value : DBNull.Value,
                p.Name, p.Price, p.ReleaseDate, p.IsLimitedEdition, p.IsActive);

        _writer.Write(table);
    }

    // =====================================================================
    // Yardımcılar
    // =====================================================================
    private void AddProduct(SubCategoryInfo sub, string? publisher, string name, decimal price,
                            DateTime release, bool limited, double weight)
    {
        // 2023 öncesi çıkan ürünlerin %3'ü artık satışta değil (pasif)
        bool isActive = !(release.Year < 2023 && _random.NextDouble() < 0.03);

        Products.Add(new ProductRow(
            ProductId: Products.Count + 1,
            CategoryId: SubCategoryIds[sub.Name],
            SubCategory: sub.Name,
            Group: sub.Group,
            PublisherId: publisher is null ? null : PublisherIds[publisher],
            Name: name,
            Price: price,
            ReleaseDate: release.Date,
            IsLimitedEdition: limited,
            IsActive: isActive,
            Weight: weight));
    }

    private static SubCategoryInfo FindSub(string name) =>
        ReferenceData.SubCategories.First(s => s.Name == name);

    private T Pick<T>(T[] items) => items[_random.Next(items.Length)];

    // Log-uniform ağırlık: 1 ile ~55 arası. Çoğu ürün düşük, birkaçı "hit".
    private double PopularityWeight() => Math.Exp(_random.NextDouble() * 4);

    private decimal RandomPrice(decimal min, decimal max) =>
        RoundPrice(min + (decimal)_random.NextDouble() * (max - min));

    // Mağaza fiyatı gibi yuvarla: 347,23 → 349,90
    public static decimal RoundPrice(decimal price) =>
        Math.Max(9.90m, Math.Round(price / 10m) * 10m - 0.10m);

    private DateTime RandomDate(DateTime from, DateTime to) =>
        from.AddDays(_random.Next((to - from).Days + 1));
}
