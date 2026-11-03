# SalesPulse — E-Ticaret Büyük Veri Analitiği

**ASP.NET Core 8 · Dapper · SQL Server · 2 milyon+ sipariş**

SalesPulse, Türkiye çapında çizgi roman, manga, figür, video oyunu ve masaüstü oyun satan hayali bir "geek" e-ticaret mağazasının yönetim panelidir. Milyonlarca siparişi **SQL Server üzerinde** filtreler, toplar ve sayfalar; uygulamaya sadece sonucu taşır.

> M&Y Yazılım Eğitim Akademi — 7. Proje (Dapper Case)

---

## İçindekiler

1. [Projenin amacı](#projenin-amacı)
2. [Kullanılan teknolojiler](#kullanılan-teknolojiler)
3. [Klasör yapısı (katmanlar)](#klasör-yapısı-katmanlar)
4. [Kurulum (adım adım)](#kurulum-adım-adım)
5. [Veritabanı ve tablo ilişkileri](#veritabanı-ve-tablo-ilişkileri)
6. [Örnek veri nasıl üretildi?](#örnek-veri-nasıl-üretildi)
7. [SQL nesneleri: indeksler ve stored procedure'ler](#sql-nesneleri-indeksler-ve-stored-procedureler)
8. [Performans çalışması (öncesi / sonrası)](#performans-çalışması-öncesi--sonrası)
9. [Uygulama adresleri](#uygulama-adresleri)
10. [Proje durumu](#proje-durumu)

---

## Projenin amacı

Bir e-ticaret yöneticisi tek bir panelden şunları görmek ister:

- **Dashboard:** Toplam sipariş, ciro, müşteri, ortalama sepet; aylık satış trendi; en iyi kategoriler, şehirler ve ürünler; sipariş durumları.
- **Order Explorer:** 2 milyon sipariş içinde arama, filtreleme, sıralama, sayfalama, sipariş detayı ve CSV dışa aktarma.
- **Sales Intelligence:** En çok harcayan müşteriler, kategorilerin aylık performansı, tekrar alışveriş oranı ve geçen yılla karşılaştırma.

Asıl hedef sadece veriyi listelemek değil; **verimli SQL sorguları, Dapper ile veri erişimi, sunucu taraflı filtreleme ve indeksle ölçülebilir hızlanma** göstermek.

**Önemli kural:** 2 milyon satır asla C#'a çekilip orada `GroupBy` ile raporlanmaz. Bütün toplama, sıralama ve sayfalama SQL Server'da yapılır.

---

## Kullanılan teknolojiler

| Teknoloji | Neden |
|---|---|
| ASP.NET Core 8 MVC | Web uygulaması ve controller'lar |
| Dapper | Stored procedure sonuçlarını C# sınıflarına (DTO) otomatik çevirmek |
| Microsoft.Data.SqlClient | SQL Server bağlantısı ve `SqlBulkCopy` ile hızlı veri yükleme |
| SQL Server 2022 (Express) | Veritabanı, indeksler, stored procedure'ler |
| Chart.js | Grafikler (arayüz aşamasında) |

---

## Klasör yapısı (katmanlar)

Proje bir restoran gibi düşünülebilir: herkes sadece kendi işini yapar.

```
SalesPulse.sln
├── SalesPulse.Entity          → DTO'lar: veriyi taşıyan basit sınıflar ("tabak")
│   └── Dtos/
├── SalesPulse.DataAccess      → Veritabanıyla konuşan tek katman ("garson")
│   ├── Connection/            → Bağlantı fabrikası
│   └── Repositories/          → Dashboard, Order, Sales repository'leri (Dapper)
├── SalesPulse.Business        → İş kuralları ("salon şefi")
│   └── Services/              → Dashboard, Order, Sales servisleri + tarih kuralları
├── SalesPulse.Web             → Controller'lar, Program.cs (DI), arayüz ("karşılayıcı")
├── SalesPulse.DataGenerator   → 2 milyon siparişi üreten konsol uygulaması
└── database/                  → SQL dosyaları (tablolar, indeksler, SP'ler, testler)
```

**Bir isteğin yolculuğu:**

```
Tarayıcı → Controller → Service → Repository (Dapper) → Stored Procedure (SQL Server)
                                                              │
Tarayıcı ← Controller ← Service ← Repository ← DTO'lar ◄──────┘
```

---

## Kurulum (adım adım)

### Gereksinimler
- Visual Studio 2022 (ya da .NET 8 SDK)
- SQL Server 2019+ (Express yeterli) ve SQL Server Management Studio (SSMS)

### 1. Bağlantı adresini ayarla
İki yerde SQL Server adı yazıyor. `KORAYHAN\SQLEXPRESS` yerine kendi sunucu adını yaz:
- `SalesPulse.Web/appsettings.json` → `ConnectionStrings:SqlConnection`
- `SalesPulse.DataGenerator/Settings.cs` → `ConnectionString`

### 2. Veritabanını ve tabloları oluştur
SSMS'te sırayla çalıştır:
```
database/00_create_database.sql   → SalesPulseDb veritabanı
database/01_tables.sql            → Tablolar, PK/FK ilişkileri, sabit veriler
```
> ⚠️ `01_tables.sql` tabloları silip yeniden oluşturur. Veri basıldıktan sonra çalıştırırsan **bütün veri silinir**.

### 3. Veriyi üret (yaklaşık 3 dakika)
Visual Studio'da `SalesPulse.DataGenerator` projesini başlangıç projesi yap ve çalıştır (Ctrl+F5).
İstersen önce veritabanına yazmadan dene: `dotnet run -- --dry-run`

### 4. Veriyi doğrula
```
database/05_verify_data.sql   → Satır sayıları, FK'ler, tutar tutarlılığı ("Sonuc" kolonunda HATA olmamalı)
```

### 5. Performansı ölç, indeksleri ve SP'leri ekle
```
database/04_performance_tests.sql   → İndeks YOKKEN çalıştır, süreleri kaydet ("öncesi")
database/02_indexes.sql             → 5 indeks
database/03_stored_procedures.sql   → 13 stored procedure
database/04_performance_tests.sql   → Tekrar çalıştır ("sonrası")
```

### 6. Web uygulamasını çalıştır
`SalesPulse.Web`'i başlangıç projesi yap ve F5'e bas. Test için:
```
https://localhost:XXXX/Dashboard/Data?startDate=2026-01-01&endDate=2026-10-31
```

---

## Veritabanı ve tablo ilişkileri

```
Cities (81) ◄──────────── Customers (100.000)
   ▲                           ▲
   │ ShippingCityId            │ CustomerId
   │                           │
OrderStatuses (6) ◄──── Orders (2.000.000) ────► Payments (2.000.000) ───► PaymentMethods (5)
                               │                   (her siparişe 1 ödeme)
                               │ OrderId
                               ▼
                        OrderDetails (3.500.065)
                               │ ProductId
                               ▼
Publishers (29) ◄─────── Products (2.000) ───────► Categories (39)
                                                     └─ ParentCategoryId → Categories
                                                        (6 ana grup + 33 alt kategori)
```

| Tablo | Satır | İçerik |
|---|---|---|
| Orders | 2.000.000 | Sipariş tarihi, müşteri, teslimat şehri, durum, toplam tutar |
| OrderDetails | 3.500.065 | Siparişin kalemleri: ürün, adet, satış anındaki fiyat |
| Payments | 2.000.000 | Ödeme yöntemi, taksit, ödeme tarihi |
| Customers | 100.000 | Ad, soyad, e-posta, şehir, kayıt tarihi |
| Products | 2.000 | Ürün adı, fiyat, çıkış tarihi, sınırlı üretim |
| Categories | 39 | 6 ana grup (Çizgi Roman, Manga, Figür…) + 33 alt kategori |
| Cities | 81 | Plaka koduyla 81 il ve bölgeleri |
| Publishers | 29 | Yayınevi ve markalar |
| OrderStatuses | 6 | Ön Sipariş, Hazırlanıyor, Kargoda, Teslim Edildi, İptal, İade |
| PaymentMethods | 5 | Kredi kartı, banka kartı, havale, kapıda ödeme, dijital cüzdan |

**Bilinçli tasarım kararları:**
- `Orders.TotalAmount` ve `Orders.ShippingCityId` tekrar tutuluyor (denormalizasyon). Böylece dashboard raporları OrderDetails/Customers tablolarına JOIN yapmadan hesaplanabiliyor.
- `OrderDetails.UnitPrice` satış anındaki fiyatı saklıyor; ürün fiyatı sonradan değişse de eski siparişler bozulmuyor.
- `CityId` plaka kodu (İstanbul = 34), bu yüzden IDENTITY değil.

---

## Örnek veri nasıl üretildi?

Veri hazır bir JSON'dan değil, **C# ile yazılmış bir veri üreticiyle** (`SalesPulse.DataGenerator`) toplu olarak üretildi. Bütün ayarlar `Settings.cs` dosyasında.

| Gerçekçilik kuralı | Nasıl yapıldı |
|---|---|
| Sezon etkisi | Her ayın bir çarpanı var: Kasım ×1,8 (Black Friday), Aralık ×1,6, Şubat ×0,7 |
| Kampanya günleri | 11.11 ×2, Black Friday ×3, sonraki 3 gün ×1,8 |
| Yıllık büyüme | Her yıl %20 daha fazla sipariş |
| Hafta sonu | Hafta sonu siparişleri %15 fazla |
| Gün içi saatler | Gece az, akşam 20:00–22:00 zirve |
| Şehir yoğunluğu | Nüfus ve e-ticaret yoğunluğuna göre ağırlık (İstanbul en yüksek) |
| Tekrar alışveriş | Müşterilerin %20'si "koleksiyoncu" ve siparişlerin ~%60'ını veriyor |
| Müşteri kayıtları | Müşteri, kayıt tarihinden önce sipariş veremez |
| Ürün popülerliği | Alt kategori popülerliği + yaz aylarında bazı kategoriler ×1,6 |
| Ön sipariş | Ürün çıkış tarihinden 45 gün önce satılabilir |
| Fiyat | Geçmiş siparişlerde fiyat yıllık %35 enflasyonla geriye indirgenir; Black Friday'de %10–25 indirim |
| Sipariş durumu | %4 iptal, %3 iade; son günlerin siparişleri "Hazırlanıyor/Kargoda" |
| Ödeme | %55 kredi kartı; taksit sadece kredi kartında ve 1.000 TL üstünde |

- **Tekrarlanabilir:** Sabit bir seed (2024) kullanıldığı için her çalıştırmada birebir aynı veri üretilir.
- **Hızlı:** `SqlBulkCopy` ile 50.000'lik paketler halinde yazılır. 2 milyon sipariş + kalemler + ödemeler yaklaşık **3 dakikada** yüklenir.
- **Güvenilir FK'ler:** Toplu yüklemede FK kontrolü açık (`CheckConstraints`), böylece SQL Server FK'lere "güvenir" ve sorgu planlarında kullanabilir.

**Üretim sonucu:** 2.000.000 sipariş · 3.500.065 kalem · %87,4 tekrar alışveriş · %60,2 koleksiyoncu payı · %92,5 teslim, %4,0 iptal, %3,0 iade.

---

## SQL nesneleri: indeksler ve stored procedure'ler

### İndeksler (`02_indexes.sql`)

| İndeks | Hangi sorguya yarıyor |
|---|---|
| `IX_Orders_OrderDate` (INCLUDE CustomerId, ShippingCityId, StatusId, TotalAmount) | Dashboard KPI, aylık trend, şehir/durum dağılımı, Order Explorer tarih filtresi ve "en yeni önce" sıralaması |
| `IX_Orders_CustomerId_OrderDate` | Müşteriye göre sorgular, en iyi müşteriler |
| `IX_OrderDetails_OrderId` (INCLUDE ProductId, Quantity, UnitPrice) | Sipariş detayı, kategori/ürün raporları |
| `UX_Payments_OrderId` (UNIQUE) | Sipariş–ödeme 1-1 ilişkisini garanti eder + Order Explorer'daki ödeme yöntemi JOIN'i |
| `IX_Products_CategoryId` | Kategori raporlarında ürün JOIN'i |

### Stored procedure'ler (`03_stored_procedures.sql`) — 13 adet

| Sayfa | Prosedür | İş |
|---|---|---|
| Dashboard | `sp_Dashboard_Kpis` | 4 KPI kartı |
| | `sp_Dashboard_MonthlyTrend` | Son 12 ayın satış ve sipariş sayısı |
| | `sp_Dashboard_TopCategories` | Ciroya göre ilk 10 kategori |
| | `sp_Dashboard_TopCities` | En çok satış yapılan 10 şehir |
| | `sp_Dashboard_StatusDistribution` | Sipariş durumları |
| | `sp_Dashboard_TopProducts` | Adet veya ciroya göre ilk 10 ürün |
| Order Explorer | `sp_Orders_Search` | Arama + filtre + sıralama + OFFSET/FETCH sayfalama (dinamik SQL, beyaz listeli sıralama) |
| | `sp_Orders_GetDetail` | Sipariş başlığı + kalemleri |
| | `sp_Orders_Export` | Filtreli CSV (en fazla 10.000 satır) |
| Sales Intelligence | `sp_Sales_TopCustomers` | En çok ciro yapan 20 müşteri |
| | `sp_Sales_CategoryMonthly` | Ana kategori gruplarının aylık performansı |
| | `sp_Sales_RepeatCustomerRate` | Tekrar alışveriş oranı (GROUP BY + HAVING) |
| | `sp_Sales_PeriodComparison` | İki tarih aralığının karşılaştırması |

Ortak kurallar: tarih filtresi indeks kullanılabilecek şekilde (`OrderDate >= @Start AND OrderDate < @End + 1 gün`); ciroda iptal ve iade hariç; bütün parametreler `sp_executesql` / Dapper parametresi olarak gider (SQL injection yok).

---

## Performans çalışması (öncesi / sonrası)

**Koşullar:** SQL Server 2022 Express (16.0.1000), yerel makine, sıcak cache (her sorgu iki kez çalıştırıldı, ikinci sonuç alındı), `SET STATISTICS IO ON` ve `SET STATISTICS TIME ON`. Ham çıktılar: `database/perf_before_messages.txt` ve `database/perf_after_messages.txt`.

| Sorgu | Öncesi | Sonrası | Logical reads (sayfa) | Kullanılan indeks |
|---|---|---|---|---|
| Q1 Dashboard KPI (2026 Ocak–Ekim) | 280 ms | 201 ms | 8.436 → 2.404 | IX_Orders_OrderDate |
| Q2 Order Explorer, sayfa 1 (İstanbul, son 3 ay) | 982 ms | ~0 ms | ~18.100 → 274 | IX_Orders_OrderDate + UX_Payments_OrderId |
| Q3 Top 20 müşteri + tekrar oranı (2025) | 624 ms | 516 ms | 16.872 → 5.760 | IX_Orders_OrderDate |
| Q4 Sipariş detayı (tek sipariş) | 145 ms | ~0 ms | 13.888 → 3 | IX_OrderDetails_OrderId |
| Q6 Derin sayfa: OFFSET 50.000 / keyset | 798 / 845 ms | 6 / 0 ms | 8.436 → 197 / 6 | IX_Orders_OrderDate |

### Bulgular

1. **Seçici sorgularda kazanç dev.** Tek sipariş ya da "ilk 25 kayıt" gibi az satır isteyen sorgular (Q2, Q4, Q6) indeksle 100 kattan fazla hızlandı. Q4'te 2 satır için önce 13.888 sayfa okunuyordu, şimdi 3 sayfa (B-tree'de kök → ara → yaprak).
2. **Geniş aralıklarda kazanç sınırlı.** Q1 ve Q3 tablonun %31–37'sini topluyor. Okuma 3,5 kat azaldı ama süre CPU'ya bağlı (sayma, tekrarsız müşteri bulma, gruplama), bu yüzden sadece %17–28 kısaldı. Bu tür raporlar için önceden hesaplanmış özet tablolar ya da columnstore indeks sonraki adım olabilir.
3. **Bir sorgu en zayıf JOIN'i kadar hızlıdır.** Q2'de sadece tarih indeksi varken (Payments indeksi yokken) sorgu **2.320 ms**'ye çıktı: SQL Server 25 siparişin her biri için Payments'ın geçici kopyasını taradı (5,7 milyon okuma). FK kolonu `Payments.OrderId`'ye indeks eklenince 0 ms'ye indi. **FK kolonlarına indeks koymak** bu yüzden kural.
4. **Keyset sayfalama**, derin sayfalarda OFFSET'ten daha istikrarlı: sayfa numarası büyüdükçe maliyeti artmıyor (6 okuma).

---

## Uygulama adresleri

| Adres | Dönen |
|---|---|
| `/Dashboard` | Dashboard sayfası |
| `/Dashboard/Data?startDate=2026-01-01&endDate=2026-10-31` | Dashboard verisi (JSON) |
| `/Orders` | Order Explorer sayfası |
| `/Orders/List?search=Yılmaz&cityId=34&statusId=4&page=2&pageSize=25&sortColumn=TotalAmount&sortDir=DESC` | Sayfalı sipariş listesi (JSON) |
| `/Orders/Detail/1500000` | Sipariş detayı (JSON, yoksa 404) |
| `/Orders/Export?cityId=34&startDate=2026-10-01&endDate=2026-10-31` | CSV dosyası |
| `/Sales` | Sales Intelligence sayfası |
| `/Sales/Data?startDate=2026-01-01&endDate=2026-10-31` | Sales Intelligence verisi (JSON) |

Tarih verilmezse varsayılan aralık **son 12 ay**dır (veri 31.10.2026'da bitiyor).

---

## Proje durumu

- [x] Veri modeli, PK/FK ilişkileri
- [x] Veri üretici (2 milyon sipariş) ve doğrulama
- [x] 5 indeks, 13 stored procedure
- [x] İndeks öncesi/sonrası performans ölçümü
- [x] Entity, DataAccess (Dapper), Business, Controller katmanları
- [ ] Arayüz: layout, Dashboard, Order Explorer, Sales Intelligence (Chart.js)
- [ ] Ekran görüntüleri ve execution plan görüntüleri
