namespace SalesPulse.DataGenerator;

// ---- Kayıt tipleri ----
// Weight / Popularity değerleri "göreli": toplamın 100 olması gerekmez,
// WeightedRandom hepsini kendi içinde orantılar.

public record CityInfo(byte Id, string Name, string Region, double Weight);

// Ürün isminin nasıl kurulacağı
public enum NameStyle
{
    Series,    // gerçek seri listesinden: "Naruto Cilt 12", "King Conan #3"
    SeriesSet, // gerçek serilerden kutulu set: "Naruto Cilt 1-62 Set (62 Ayrı Kitap)"
    Catalog,   // gerçek katalog listesinden (figür, oyun, özel edisyon)
    Subtitle,  // kurgusal: "Demir Kalkan: Son Kale" + ek
    Item       // kurgusal: "Neon Samuray" + ek  (ör. "Logolu Tişört")
}

public record SubCategoryInfo(
    string Name,
    string Group,               // ana grup adı
    decimal MinPrice,
    decimal MaxPrice,
    double Popularity,          // satış payı (göreli)
    bool SummerBoost,           // yazın daha çok satar mı?
    string[] Publishers,        // kurgusal/katalog ürünlerde yayınevi/markalar (boşsa markasız)
    NameStyle Style,
    string Suffix,              // isim sonuna eklenecek ifade
    string[]? Variants = null); // Catalog stilinde: her kayıt için sürümler

// Gerçek katalog kaydı: hangi alt kategori, hangi yayınevi/marka, hangi isim
public record CatalogEntry(string SubCategory, string Publisher, string Name);

// Gerçek çizgi roman / manga serisi
//   Volumes        : kaç cilt/sayı üretilecek (1 = tek kitap)
//   VolumeLabel    : "Cilt", "Sayı", "#" ya da "" (numarasız tek kitap)
//   HasVariantCover: her cildin bir de "(Varyant Kapak)" ürünü var mı?
public record SeriesEntry(
    string SubCategory,
    string Publisher,
    string Name,
    int Volumes,
    string VolumeLabel = "Cilt",
    bool HasVariantCover = false);

public static class ReferenceData
{
    // =========================================================
    // 81 İL: plaka kodu, ad, bölge, sipariş ağırlığı
    // Ağırlık ≈ nüfus + e-ticaret yoğunluğu; üniversite şehirleri
    // (Eskişehir, Erzurum, Isparta...) geek ürünlerde biraz yukarıda.
    // =========================================================
    public static readonly CityInfo[] Cities =
    {
        new(1,  "Adana",          "Akdeniz",             2.00),
        new(2,  "Adıyaman",       "Güneydoğu Anadolu",   0.30),
        new(3,  "Afyonkarahisar", "Ege",                 0.50),
        new(4,  "Ağrı",           "Doğu Anadolu",        0.20),
        new(5,  "Amasya",         "Karadeniz",           0.25),
        new(6,  "Ankara",         "İç Anadolu",          9.00),
        new(7,  "Antalya",        "Akdeniz",             3.50),
        new(8,  "Artvin",         "Karadeniz",           0.12),
        new(9,  "Aydın",          "Ege",                 1.00),
        new(10, "Balıkesir",      "Marmara",             1.10),
        new(11, "Bilecik",        "Marmara",             0.20),
        new(12, "Bingöl",         "Doğu Anadolu",        0.15),
        new(13, "Bitlis",         "Doğu Anadolu",        0.15),
        new(14, "Bolu",           "Karadeniz",           0.40),
        new(15, "Burdur",         "Akdeniz",             0.20),
        new(16, "Bursa",          "Marmara",             4.00),
        new(17, "Çanakkale",      "Marmara",             0.70),
        new(18, "Çankırı",        "İç Anadolu",          0.15),
        new(19, "Çorum",          "Karadeniz",           0.35),
        new(20, "Denizli",        "Ege",                 1.10),
        new(21, "Diyarbakır",     "Güneydoğu Anadolu",   0.90),
        new(22, "Edirne",         "Marmara",             0.60),
        new(23, "Elazığ",         "Doğu Anadolu",        0.50),
        new(24, "Erzincan",       "Doğu Anadolu",        0.20),
        new(25, "Erzurum",        "Doğu Anadolu",        0.70),
        new(26, "Eskişehir",      "İç Anadolu",          2.20),
        new(27, "Gaziantep",      "Güneydoğu Anadolu",   1.80),
        new(28, "Giresun",        "Karadeniz",           0.30),
        new(29, "Gümüşhane",      "Karadeniz",           0.10),
        new(30, "Hakkari",        "Doğu Anadolu",        0.10),
        new(31, "Hatay",          "Akdeniz",             0.90),
        new(32, "Isparta",        "Akdeniz",             0.60),
        new(33, "Mersin",         "Akdeniz",             1.70),
        new(34, "İstanbul",       "Marmara",            25.00),
        new(35, "İzmir",          "Ege",                 7.00),
        new(36, "Kars",           "Doğu Anadolu",        0.15),
        new(37, "Kastamonu",      "Karadeniz",           0.25),
        new(38, "Kayseri",        "İç Anadolu",          1.50),
        new(39, "Kırklareli",     "Marmara",             0.30),
        new(40, "Kırşehir",       "İç Anadolu",          0.20),
        new(41, "Kocaeli",        "Marmara",             3.00),
        new(42, "Konya",          "İç Anadolu",          2.20),
        new(43, "Kütahya",        "Ege",                 0.40),
        new(44, "Malatya",        "Doğu Anadolu",        0.60),
        new(45, "Manisa",         "Ege",                 1.00),
        new(46, "Kahramanmaraş",  "Akdeniz",             0.60),
        new(47, "Mardin",         "Güneydoğu Anadolu",   0.30),
        new(48, "Muğla",          "Ege",                 1.10),
        new(49, "Muş",            "Doğu Anadolu",        0.15),
        new(50, "Nevşehir",       "İç Anadolu",          0.25),
        new(51, "Niğde",          "İç Anadolu",          0.25),
        new(52, "Ordu",           "Karadeniz",           0.50),
        new(53, "Rize",           "Karadeniz",           0.30),
        new(54, "Sakarya",        "Marmara",             1.20),
        new(55, "Samsun",         "Karadeniz",           1.30),
        new(56, "Siirt",          "Güneydoğu Anadolu",   0.15),
        new(57, "Sinop",          "Karadeniz",           0.15),
        new(58, "Sivas",          "İç Anadolu",          0.50),
        new(59, "Tekirdağ",       "Marmara",             1.30),
        new(60, "Tokat",          "Karadeniz",           0.40),
        new(61, "Trabzon",        "Karadeniz",           1.10),
        new(62, "Tunceli",        "Doğu Anadolu",        0.08),
        new(63, "Şanlıurfa",      "Güneydoğu Anadolu",   0.80),
        new(64, "Uşak",           "Ege",                 0.35),
        new(65, "Van",            "Doğu Anadolu",        0.50),
        new(66, "Yozgat",         "İç Anadolu",          0.25),
        new(67, "Zonguldak",      "Karadeniz",           0.40),
        new(68, "Aksaray",        "İç Anadolu",          0.30),
        new(69, "Bayburt",        "Karadeniz",           0.07),
        new(70, "Karaman",        "İç Anadolu",          0.20),
        new(71, "Kırıkkale",      "İç Anadolu",          0.30),
        new(72, "Batman",         "Güneydoğu Anadolu",   0.30),
        new(73, "Şırnak",         "Güneydoğu Anadolu",   0.15),
        new(74, "Bartın",         "Karadeniz",           0.15),
        new(75, "Ardahan",        "Doğu Anadolu",        0.08),
        new(76, "Iğdır",          "Doğu Anadolu",        0.12),
        new(77, "Yalova",         "Marmara",             0.40),
        new(78, "Karabük",        "Karadeniz",           0.30),
        new(79, "Kilis",          "Güneydoğu Anadolu",   0.10),
        new(80, "Osmaniye",       "Akdeniz",             0.35),
        new(81, "Düzce",          "Karadeniz",           0.40),
    };

    // =========================================================
    // YAYINEVİ / MARKA İSİMLERİ
    // Çizgi roman & manga yayınevleri: edessakitabevi.com kataloğundan
    // =========================================================
    const string GerekliSeyler = "Gerekli Şeyler";
    const string Athica        = "Athica Yayınları";
    const string Akilcelen     = "Akılçelen Kitaplar";
    const string Kurukafa      = "Kurukafa Yayınları";
    const string KayipKita     = "Kayıp Kıta";
    const string ArkaBahce     = "Arkabahçe";
    const string MarmaraCizgi  = "Marmara Çizgi";
    const string CizgiDusler   = "Çizgi Düşler";
    const string Jbc           = "JBC Yayıncılık";
    const string Baobab        = "Baobab";
    const string Altikirkbes   = "Altıkırkbeş Yayınları";
    const string Artemis       = "Artemis";

    // Figür markaları
    const string MarvelLegends = "Marvel Legends";
    const string BlackSeries   = "Star Wars: The Black Series";
    const string Mafex         = "MAFEX";
    const string Figuarts      = "S.H.Figuarts";
    const string MonsterArts   = "S.H.MonsterArts";
    const string HotToys       = "Hot Toys";

    // Oyun yayıncıları / konsol üreticileri
    const string Sony          = "Sony Interactive Entertainment";
    const string Nintendo      = "Nintendo";

    // Masaüstü oyun ve giyim/aksesuar (kurgusal - istersen gerçekleriyle değiştir)
    static readonly string[] BoardGamePublishers = { "Zar Atölyesi", "Kale Oyunları", "Meeple Evi", "Gizli Kart", "Ejder Masası" };
    static readonly string[] MerchBrands         = { "Geek Wear", "Nerd Yuvası", "Kupa Kulübü", "Retro Rozet" };

    static readonly string[] None = Array.Empty<string>();

    // =========================================================
    // KATEGORİ AĞACI: 6 ana grup, 33 alt kategori
    // =========================================================
    public const string GroupComics   = "Çizgi Roman";
    public const string GroupManga    = "Manga";
    public const string GroupFigures  = "Figür & Koleksiyon";
    public const string GroupVideo    = "Video Oyunları";
    public const string GroupTabletop = "Masaüstü Oyunlar";
    public const string GroupMerch    = "Giyim & Aksesuar";

    public static readonly string[] MainGroups =
    {
        GroupComics, GroupManga, GroupFigures, GroupVideo, GroupTabletop, GroupMerch
    };

    // Series / Set / Catalog stilindeki kategorilerde yayınevleri
    // aşağıdaki gerçek listelerden gelir, burada boş bırakılır.
    public static readonly SubCategoryInfo[] SubCategories =
    {
        // ---- Çizgi Roman (edessakitabevi.com menüsü) ----
        new("Marvel",                GroupComics,  300,   900, 4.0, false, None, NameStyle.Series,    ""),
        new("DC Comics",             GroupComics,  300,   900, 2.5, false, None, NameStyle.Series,    ""),
        new("Image Comics",          GroupComics,  300,   850, 2.5, false, None, NameStyle.Series,    ""),
        new("Frankofon",             GroupComics,  400,  1200, 1.5, false, None, NameStyle.Series,    ""),
        new("Conan & Red Sonja",     GroupComics,  100,   300, 0.8, false, None, NameStyle.Series,    ""),
        new("Bağımsız & Klasik",     GroupComics,  200,   800, 1.2, false, None, NameStyle.Series,    ""),
        new("Özel Edisyon",          GroupComics, 1200,  4000, 1.0, false, None, NameStyle.Catalog,   ""),
        new("Set & Kutulu Seri",     GroupComics, 1500, 15000, 1.2, false, None, NameStyle.SeriesSet, ""),

        // ---- Manga ----
        new("Shōnen Manga",          GroupManga,   180,   350, 10.0, true,  None, NameStyle.Series, ""),
        new("Seinen Manga",          GroupManga,   200,   450,  3.0, true,  None, NameStyle.Series, ""),
        new("Shōjo & Josei Manga",   GroupManga,   180,   320,  1.0, true,  None, NameStyle.Series, ""),

        // ---- Figür & Koleksiyon ----
        new("Aksiyon Figürü",          GroupFigures, 1200,  2500, 5.0, false, new[] { MarvelLegends, BlackSeries }, NameStyle.Catalog, "", new[] { "", "Deluxe Set", "2'li Paket" }),
        new("Premium Aksiyon Figürü",  GroupFigures, 3000,  7000, 2.0, false, new[] { Mafex, Figuarts },            NameStyle.Catalog, "", new[] { "", "DX Sürüm" }),
        new("Anime Figürü",            GroupFigures, 2500,  5500, 3.0, false, new[] { Figuarts },                   NameStyle.Catalog, "", new[] { "", "Özel Renk Sürümü" }),
        new("Kaiju Figürü",            GroupFigures, 3000,  9000, 1.0, false, new[] { MonsterArts },                NameStyle.Catalog, "", new[] { "", "Işıklı Sürüm" }),
        new("1/6 Ölçek Koleksiyon Figürü", GroupFigures, 12000, 30000, 0.6, false, new[] { HotToys },               NameStyle.Catalog, "", new[] { "", "Deluxe Sürüm" }),
        new("Mini & Vinyl Figür",      GroupFigures,  300,  1200, 2.0, false, None,                                 NameStyle.Item,    "Mini Figür"),

        // ---- Video Oyunları ----
        new("PS5 Oyunu",            GroupVideo, 2000,  3500, 5.0, false, new[] { Sony },           NameStyle.Catalog, "(PS5)", new[] { "Standart Sürüm", "Deluxe Sürüm", "Steelbook Sürüm" }),
        new("PS4 Oyunu",            GroupVideo,  800,  2500, 4.0, false, new[] { Sony },           NameStyle.Catalog, "(PS4)", new[] { "Standart Sürüm", "PlayStation Hits", "Steelbook Sürüm" }),
        new("PS3 Oyunu",            GroupVideo,  400,  1500, 1.5, false, new[] { Sony },           NameStyle.Catalog, "(PS3)", new[] { "Standart", "Essentials", "İkinci El" }),
        new("PSP Oyunu",            GroupVideo,  400,  1800, 1.2, false, new[] { Sony },           NameStyle.Catalog, "(PSP)", new[] { "UMD", "Platinum", "İkinci El" }),
        new("Game Boy / GBA Oyunu", GroupVideo,  800,  4500, 1.5, false, new[] { Nintendo },       NameStyle.Catalog, "",      new[] { "Kartuş", "Kutulu (CIB)", "İkinci El Kartuş" }),
        new("Retro El Konsolu",     GroupVideo, 2500, 12000, 0.6, false, new[] { Nintendo, Sony }, NameStyle.Catalog, "",      new[] { "İkinci El", "Yenilenmiş", "Kutulu Koleksiyonluk" }),

        // ---- Masaüstü Oyunlar ----
        new("Kutu Oyunu",           GroupTabletop, 700, 4500, 4.0, true,  BoardGamePublishers, NameStyle.Subtitle, "Kutu Oyunu"),
        new("Kart Oyunu (TCG)",     GroupTabletop, 150, 1200, 5.0, true,  BoardGamePublishers, NameStyle.Item,     "Booster Paketi"),
        new("Masaüstü RPG",         GroupTabletop, 800, 3500, 1.5, false, BoardGamePublishers, NameStyle.Subtitle, "Kural Kitabı"),
        new("Zar & Aksesuar",       GroupTabletop, 100,  900, 2.0, false, BoardGamePublishers, NameStyle.Item,     "Zar Seti"),
        new("Minyatür Savaş Oyunu", GroupTabletop, 900, 6000, 1.2, false, BoardGamePublishers, NameStyle.Item,     "Minyatür Ordu Seti"),
        new("Parti Oyunu",          GroupTabletop, 300, 1200, 2.0, true,  BoardGamePublishers, NameStyle.Subtitle, "Parti Oyunu"),

        // ---- Giyim & Aksesuar ----
        new("Tişört",               GroupMerch, 350,  900, 3.5, true,  MerchBrands, NameStyle.Item, "Logolu Tişört"),
        new("Kupa",                 GroupMerch, 200,  500, 2.5, false, MerchBrands, NameStyle.Item, "Kupa"),
        new("Poster & Tablo",       GroupMerch, 150, 1500, 2.0, false, MerchBrands, NameStyle.Item, "Poster"),
        new("Anahtarlık & Rozet",   GroupMerch,  80,  300, 2.5, false, MerchBrands, NameStyle.Item, "Anahtarlık"),
    };

    // =========================================================
    // GERÇEK SERİLER (edessakitabevi.com kataloğundan)
    // Volumes: sitede görülen en yüksek cilt/sayı numarası
    // Ürün adı: "Naruto Cilt 12", "King Conan #3", tek kitapsa sadece ad
    // Volumes >= 5 olan seriler için ayrıca "Set & Kutulu Seri" ürünü üretilir
    // =========================================================
    public static readonly SeriesEntry[] Series =
    {
        // ---- Shōnen Manga ----
        new("Shōnen Manga", GerekliSeyler, "Naruto", 62),
        new("Shōnen Manga", GerekliSeyler, "One Piece", 62),
        new("Shōnen Manga", GerekliSeyler, "Bleach", 48),
        new("Shōnen Manga", GerekliSeyler, "Kahramanlık Akademim", 33),
        new("Shōnen Manga", GerekliSeyler, "Titana Saldırı", 33),
        new("Shōnen Manga", GerekliSeyler, "Yedi Ölümcül Günah", 30),
        new("Shōnen Manga", GerekliSeyler, "Kuroko'nun Basketbolu", 23),
        new("Shōnen Manga", GerekliSeyler, "Fairy Tail", 22),
        new("Shōnen Manga", GerekliSeyler, "Jujutsu Kaisen", 20),
        new("Shōnen Manga", GerekliSeyler, "Tokyo Revengers", 19),
        new("Shōnen Manga", GerekliSeyler, "Chainsaw Man Testere Adam", 17),
        new("Shōnen Manga", GerekliSeyler, "Tuvalet Hayaleti Hanako-Kun", 15),
        new("Shōnen Manga", GerekliSeyler, "Spy x Family", 15),
        new("Shōnen Manga", GerekliSeyler, "Dandadan", 8),
        new("Shōnen Manga", GerekliSeyler, "Noragami Başıboş Tanrı", 1),
        new("Shōnen Manga", Athica,        "Kuzey Yıldızı'nın Yumruğu", 1),
        new("Shōnen Manga", Akilcelen,     "Death Note Ölüm Defteri", 13),
        new("Shōnen Manga", Akilcelen,     "Mashle", 8),
        new("Shōnen Manga", Akilcelen,     "Blue Box - Mavi Kutu", 5),
        new("Shōnen Manga", Kurukafa,      "Kaiju No: 8 - 8 No'lu Canavar", 9),
        new("Shōnen Manga", Kurukafa,      "Moriarty the Patriot - Vatansever Moriarty", 6),
        new("Shōnen Manga", KayipKita,     "Gecenin Çağrısı", 6),
        new("Shōnen Manga", KayipKita,     "Beni Ay'a Götür", 5),
        new("Shōnen Manga", KayipKita,     "Pandora Hearts", 3),
        new("Shōnen Manga", MarmaraCizgi,  "Frieren", 7),

        // ---- Seinen Manga ----
        new("Seinen Manga", Akilcelen,     "One-Punch Man Tek Yumruk", 30),
        new("Seinen Manga", Athica,        "Berserk", 20),
        new("Seinen Manga", GerekliSeyler, "Oşi No Ko", 13),
        new("Seinen Manga", MarmaraCizgi,  "Monster", 9),
        new("Seinen Manga", KayipKita,     "Parazit", 7),
        new("Seinen Manga", KayipKita,     "Yeni Geçit", 6),
        new("Seinen Manga", KayipKita,     "Mieruko-Chan", 4),
        new("Seinen Manga", Akilcelen,     "Delicious in Dungeon - Zindan Lezzetleri", 1),

        // ---- Shōjo & Josei Manga ----
        new("Shōjo & Josei Manga", GerekliSeyler, "Buna Gizem Deme", 9),
        new("Shōjo & Josei Manga", MarmaraCizgi,  "Nana", 7),

        // ---- Marvel ----
        new("Marvel", MarmaraCizgi, "Marvel Team Up", 14, "#"),
        new("Marvel", MarmaraCizgi, "Venom", 7),
        new("Marvel", MarmaraCizgi, "The New Avengers", 5),
        new("Marvel", MarmaraCizgi, "Ultimate Spider-Man", 3),
        new("Marvel", MarmaraCizgi, "Spider-Punk: Av Başlıyor", 1, ""),
        new("Marvel", ArkaBahce,    "Fantastic Four", 4, HasVariantCover: true),
        new("Marvel", ArkaBahce,    "Tony Stark Iron Man", 2, HasVariantCover: true),
        new("Marvel", ArkaBahce,    "Doktor Strange", 2, HasVariantCover: true),
        new("Marvel", ArkaBahce,    "Daredevil - Mark Waid", 1, HasVariantCover: true),
        new("Marvel", ArkaBahce,    "Godzilla vs Hulk", 1, "", true),
        new("Marvel", ArkaBahce,    "Godzilla vs Spider-Man", 1, "", true),
        new("Marvel", ArkaBahce,    "Godzilla vs Avengers", 1, "", true),
        new("Marvel", ArkaBahce,    "Godzilla vs Thor", 1, "", true),
        new("Marvel", ArkaBahce,    "Godzilla vs X-Men", 1, "", true),
        new("Marvel", ArkaBahce,    "Godzilla vs Fantastik Dörtlü", 1, "", true),
        new("Marvel", Jbc,          "Moon Knight: Siyah, Beyaz & Kan", 1, ""),

        // ---- DC Comics ----
        new("DC Comics", Jbc,         "Absolute Batman", 1),
        new("DC Comics", ArkaBahce,   "Absolute Green Lantern", 1, HasVariantCover: true),
        new("DC Comics", ArkaBahce,   "Absolute Flash", 1, HasVariantCover: true),
        new("DC Comics", ArkaBahce,   "Green Lantern: Karanlık", 1, "", true),

        // ---- Image Comics ----
        new("Image Comics", MarmaraCizgi, "Yürüyen Ölüler", 32),
        new("Image Comics", ArkaBahce,    "Spawn Klasik Seri", 8, ""),
        new("Image Comics", Athica,       "Invincible", 8),
        new("Image Comics", MarmaraCizgi, "Southern Bastards", 4),
        new("Image Comics", MarmaraCizgi, "Küçük Canavarlar", 2),
        new("Image Comics", MarmaraCizgi, "Crossover", 2),
        new("Image Comics", MarmaraCizgi, "Monstress", 1),

        // ---- Frankofon ----
        new("Frankofon", MarmaraCizgi, "Canavar Dörtlemesi", 4),
        new("Frankofon", MarmaraCizgi, "Gazap Fırtınası Üçlemesi", 3),
        new("Frankofon", MarmaraCizgi, "Bugünün Efsaneleri", 3),
        new("Frankofon", MarmaraCizgi, "Kara Tarikat Tugayları", 1, ""),
        new("Frankofon", CizgiDusler,  "Her Şeye Rağmen", 1, ""),
        new("Frankofon", ArkaBahce,    "Karanlık Güzel", 1, ""),
        new("Frankofon", Baobab,       "Kurt", 1, ""),
        new("Frankofon", Baobab,       "Hariciye Günlükleri", 1, ""),

        // ---- Conan & Red Sonja ----
        new("Conan & Red Sonja", MarmaraCizgi, "Conan The Barbarian", 25, "Sayı"),
        new("Conan & Red Sonja", MarmaraCizgi, "King Conan", 6, "#"),

        // ---- Bağımsız & Klasik ----
        new("Bağımsız & Klasik", MarmaraCizgi, "Ninja Kaplumbağalar - Son Ronin Kayıp Yıllar", 5, "Sayı"),
        new("Bağımsız & Klasik", MarmaraCizgi, "Ninja Kaplumbağalar", 1),
        new("Bağımsız & Klasik", MarmaraCizgi, "Breaker", 1),
        new("Bağımsız & Klasik", CizgiDusler,  "100 Kurşun", 2),
        new("Bağımsız & Klasik", Altikirkbes,  "Krazy Kat - ve Rüzgar Esti Güneybatıdan", 1, ""),
        new("Bağımsız & Klasik", Altikirkbes,  "Krazy Kat - Boynu Bükük Bir Yürek", 1, ""),
        new("Bağımsız & Klasik", Altikirkbes,  "Kedi Felix - Karın Tokluğu", 1, ""),
        new("Bağımsız & Klasik", Artemis,      "Kadın Savaşçılar", 1, ""),
    };

    // =========================================================
    // GERÇEK KATALOG: ÖZEL EDİSYONLAR (sert kapak / ciltli / kutulu)
    // =========================================================
    public static readonly CatalogEntry[] SpecialEditions =
    {
        new("Özel Edisyon", MarmaraCizgi, "İç Savaş Özel Edisyon (Ciltli)"),
        new("Özel Edisyon", MarmaraCizgi, "Silver Surfer - Yıldızların Ötesindeki Karanlık Özel Edisyon (Ciltli)"),
        new("Özel Edisyon", MarmaraCizgi, "Amazing Spider-Man Epik Koleksiyon 2001-2002 Eve Dönüş"),
        new("Özel Edisyon", MarmaraCizgi, "Spider-Man Almanak 1967"),
        new("Özel Edisyon", MarmaraCizgi, "Ninja Kaplumbağalar - Son Ronin (Ciltli - Sert Kapak)"),
        new("Özel Edisyon", MarmaraCizgi, "Ninja Kaplumbağalar - Son Ronin Kayıp Yıllar Özel Edisyon"),
        new("Özel Edisyon", CizgiDusler,  "Infinite Crisis - Sonsuz Buhran Sert Kapak (Ciltli)"),
        new("Özel Edisyon", CizgiDusler,  "Her Şeye Rağmen (Sert Kapak Edisyon)"),
        new("Özel Edisyon", ArkaBahce,    "Marvel Evreni Tarihi (Sert Kapak)"),
        new("Özel Edisyon", Jbc,          "Absolute Batman Cilt 1: Hayvanat Bahçesi Sert Kapak"),
        new("Özel Edisyon", Athica,       "Kuzey Yıldızı'nın Yumruğu Cilt 1 (Kutulu)"),
    };

    // =========================================================
    // GERÇEK KATALOG: FİGÜRLER (marka → karakter)
    // Ürün adı: "{Marka} {Karakter}" + sürüm  → "Marvel Legends Spider-Man (Deluxe Set)"
    // =========================================================
    public static readonly CatalogEntry[] Figures =
        Line("Aksiyon Figürü", MarvelLegends,
            "Spider-Man", "Iron Man", "Captain America", "Thor", "Hulk", "Wolverine",
            "Deadpool", "Black Panther", "Doctor Strange", "Venom", "Scarlet Witch", "Loki")
        .Concat(Line("Aksiyon Figürü", BlackSeries,
            "Darth Vader", "Luke Skywalker", "Boba Fett", "The Mandalorian", "Stormtrooper",
            "Ahsoka Tano", "Kylo Ren", "Obi-Wan Kenobi", "Han Solo", "Darth Maul"))
        .Concat(Line("Premium Aksiyon Figürü", Mafex,
            "Batman", "Superman", "Joker", "Wonder Woman", "Spider-Man", "Iron Man",
            "Wolverine", "Robin", "The Flash", "Captain America"))
        .Concat(Line("Premium Aksiyon Figürü", Figuarts,
            "Kamen Rider", "Ultraman", "Spider-Man", "Iron Man", "Batman"))
        .Concat(Line("Anime Figürü", Figuarts,
            "Son Goku", "Vegeta", "Son Gohan", "Naruto Uzumaki", "Sasuke Uchiha",
            "Monkey D. Luffy", "Roronoa Zoro", "Ichigo Kurosaki", "Tanjiro Kamado", "Satoru Gojo"))
        .Concat(Line("Kaiju Figürü", MonsterArts,
            "Godzilla (1954)", "Godzilla (2019)", "Shin Godzilla", "King Ghidorah",
            "Mothra", "Rodan", "Mechagodzilla", "Kong"))
        .Concat(Line("1/6 Ölçek Koleksiyon Figürü", HotToys,
            "Iron Man", "Darth Vader", "The Mandalorian", "Batman", "Joker",
            "Spider-Man", "Thor", "Captain America", "Loki", "Doctor Strange"))
        .ToArray();

    // =========================================================
    // GERÇEK KATALOG: PLAYSTATION ÖZEL OYUNLARI ve GAME BOY
    // Ürün adı: "{Oyun} (PS4) - {Sürüm}"
    // =========================================================
    public static readonly CatalogEntry[] Games =
        Line("PS5 Oyunu", Sony,
            "Demon's Souls", "Ratchet & Clank: Rift Apart", "Returnal", "Marvel's Spider-Man 2",
            "Astro Bot", "Stellar Blade", "Rise of the Ronin", "Ghost of Yōtei",
            "Death Stranding 2: On the Beach", "God of War Ragnarök", "Horizon Forbidden West",
            "Gran Turismo 7")
        .Concat(Line("PS4 Oyunu", Sony,
            "Bloodborne", "Uncharted 4: A Thief's End", "The Last of Us Part II", "God of War",
            "Marvel's Spider-Man", "Horizon Zero Dawn", "Ghost of Tsushima", "Days Gone",
            "Death Stranding", "Detroit: Become Human", "Until Dawn", "inFAMOUS Second Son",
            "The Order: 1886", "Gran Turismo Sport"))
        .Concat(Line("PS3 Oyunu", Sony,
            "The Last of Us", "Uncharted 2: Among Thieves", "Uncharted 3: Drake's Deception",
            "God of War III", "LittleBigPlanet 2", "Killzone 2", "inFAMOUS", "Heavy Rain",
            "Beyond: Two Souls", "Journey", "Resistance: Fall of Man", "Gran Turismo 5",
            "Ratchet & Clank Future: A Crack in Time"))
        .Concat(Line("PSP Oyunu", Sony,
            "God of War: Chains of Olympus", "God of War: Ghost of Sparta", "Daxter",
            "Patapon", "Patapon 2", "LocoRoco", "Killzone: Liberation", "Jeanne d'Arc",
            "Ratchet & Clank: Size Matters", "Resistance: Retribution", "Gran Turismo"))
        .Concat(Line("Game Boy / GBA Oyunu", Nintendo,
            "Tetris", "Pokémon Red", "Pokémon Blue", "Pokémon Yellow", "Pokémon Gold",
            "Pokémon Silver", "Pokémon Crystal", "The Legend of Zelda: Link's Awakening",
            "Super Mario Land", "Super Mario Land 2: 6 Golden Coins", "Kirby's Dream Land",
            "Metroid II: Return of Samus", "Wario Land: Super Mario Land 3", "Pokémon Emerald",
            "Pokémon FireRed", "Metroid Fusion", "The Legend of Zelda: The Minish Cap",
            "Golden Sun", "Advance Wars"))
        .Concat(Line("Retro El Konsolu", Nintendo,
            "Game Boy (DMG-01)", "Game Boy Pocket", "Game Boy Color",
            "Game Boy Advance", "Game Boy Advance SP"))
        .Concat(Line("Retro El Konsolu", Sony,
            "PSP-1000", "PSP-2000 Slim", "PSP-3000", "PSP Go"))
        .ToArray();

    static IEnumerable<CatalogEntry> Line(string subCategory, string publisher, params string[] names)
        => names.Select(n => new CatalogEntry(subCategory, publisher, n));

    // Bütün yayınevi/markaların tekil listesi (Publishers tablosuna gidecek)
    public static readonly string[] Publishers =
        Series.Select(s => s.Publisher)
            .Concat(SpecialEditions.Select(s => s.Publisher))
            .Concat(SubCategories.SelectMany(s => s.Publishers))
            .Concat(Figures.Select(f => f.Publisher))
            .Concat(Games.Select(g => g.Publisher))
            .Distinct()
            .ToArray();

    // =========================================================
    // KURGUSAL İSİM HAVUZLARI (kutu oyunları, giyim, mini figür)
    // Seri adı = Önek + İsim  →  "Gölge" + "Lejyonu" = "Gölge Lejyonu"
    // 20 × 20 = 400 farklı seri adı
    // =========================================================
    public static readonly string[] SeriesPrefixes =
    {
        "Gölge", "Demir", "Kızıl", "Gece", "Yıldız", "Kayıp", "Son", "Karanlık",
        "Altın", "Fırtına", "Buz", "Ateş", "Kristal", "Neon", "Kadim", "Gümüş",
        "Kızıl Ay", "Siber", "Yasak", "Sonsuz"
    };

    public static readonly string[] SeriesNouns =
    {
        "Lejyonu", "Savaşçı", "Kartal", "Şövalye", "Avcısı", "İmparatorluğu",
        "Kalkanı", "Kılıcı", "Bekçisi", "Muhafızları", "Ejderhası", "Kurdu",
        "Hayaleti", "Samuray", "Robotu", "Korsanları", "Büyücüsü", "Ninja",
        "Gezgini", "Krallığı"
    };

    public static readonly string[] Subtitles =
    {
        "Karanlık Çağ", "Yeniden Doğuş", "Son Kale", "Kayıp Topraklar",
        "Sonsuz Gece", "İsyan", "Efsaneler", "Uyanış", "Kanlı Ay",
        "Unutulmuş Tahtlar", "Yıldızlararası", "Son Savaş", "Gizli Tapınak",
        "Kıyamet Günü", "Başlangıç"
    };

    // =========================================================
    // MÜŞTERİ İSİMLERİ
    // =========================================================
    public static readonly string[] FirstNames =
    {
        // Erkek (50)
        "Ahmet", "Mehmet", "Mustafa", "Ali", "Hüseyin", "Hasan", "İbrahim", "Emre",
        "Burak", "Mert", "Can", "Cem", "Deniz", "Efe", "Eren", "Kaan", "Kerem",
        "Onur", "Oğuz", "Serkan", "Tolga", "Umut", "Yusuf", "Barış", "Berk",
        "Arda", "Alp", "Baran", "Batuhan", "Doruk", "Ege", "Emir", "Furkan",
        "Gökhan", "Halil", "Kemal", "Murat", "Okan", "Ozan", "Selim", "Sinan",
        "Taha", "Volkan", "Yiğit", "Ömer", "Koray", "Çağrı", "Enes", "Görkem", "Tuna",
        // Kadın (50)
        "Ayşe", "Fatma", "Zeynep", "Elif", "Merve", "Büşra", "Esra", "Ece",
        "Selin", "İrem", "Damla", "Gizem", "Ceren", "Derya", "Melis", "Nazlı",
        "Pınar", "Seda", "Sena", "Tuğba", "Yağmur", "Aslı", "Buse", "Dilara",
        "Ebru", "Gamze", "Hande", "İlayda", "Kübra", "Lale", "Melike", "Naz",
        "Nehir", "Özge", "Rabia", "Sibel", "Şeyma", "Tuana", "Yasemin", "Zehra",
        "Beril", "Defne", "Duygu", "Ezgi", "Hazal", "Işıl", "Nil", "Su", "Asya", "Ela"
    };

    public static readonly string[] LastNames =
    {
        "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk",
        "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara",
        "Koç", "Kurt", "Özkan", "Şimşek", "Polat", "Özcan", "Korkmaz", "Çakır",
        "Erdoğan", "Yavuz", "Acar", "Şen", "Aktaş", "Güler", "Yalçın", "Güneş",
        "Bozkurt", "Bulut", "Keskin", "Ünal", "Turan", "Gül", "Özer", "Işık",
        "Kaplan", "Avcı", "Sarı", "Tekin", "Taş", "Köse", "Yüksel", "Ateş",
        "Aktürk", "Karaca", "Uçar", "Duman", "Uysal", "Kaçar", "Erdem", "Tunç",
        "Akın", "Coşkun", "Başaran", "Altun", "Akbulut", "Sevim", "Toprak", "Karakaya",
        "Çiftçi", "Eren", "Bayram", "Güngör", "Akgül", "Oral", "Gündoğdu", "Ekinci",
        "Kocabaş", "Sezer", "Durmaz", "Erol", "Kalkan", "Çınar", "Bilgin", "Ayhan",
        "Karataş", "Akyüz", "Tan", "Uğur", "Özbek", "Demirci", "Dinç", "Soylu",
        "Gök", "Ercan", "Akdeniz", "Önal", "Baysal", "Ergin", "Savaş", "Yazıcı",
        "Kahraman", "Orhan", "Avcu", "Tezcan"
    };

    // E-posta alan adları (kurgusal / genel)
    public static readonly string[] EmailDomains =
    {
        "mail.com", "posta.net", "geekmail.com", "eposta.org", "kutu.net"
    };
}
