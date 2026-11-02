using System.Data;

namespace SalesPulse.DataGenerator.Generators;

// Orders, OrderDetails ve Payments tablolarını üretir.
//
// Akış:
//   1) 2.000.000 sipariş zamanı üretilir (sezon, büyüme, hafta sonu, kampanya, saat) ve sıralanır
//      → OrderId sırası = tarih sırası (gerçek sistemdeki gibi).
//   2) Siparişler BatchSize'lık parçalar halinde üretilir; her parça önce Orders,
//      sonra OrderDetails, sonra Payments olarak yazılır (FK sırası). Bellek sabit kalır.
public class OrderGenerator
{
    private readonly Random _random;
    private readonly BulkWriter _writer;
    private readonly CustomerGenerator _customers;

    private readonly WeightedRandom<string> _subNormal;
    private readonly WeightedRandom<string> _subSummer;
    private readonly Dictionary<string, SubCategoryPool> _pools;
    private readonly WeightedRandom<int> _basketSize;
    private readonly WeightedRandom<byte> _paymentMethod;
    private readonly WeightedRandom<byte> _installments;
    private readonly WeightedRandom<byte> _city;

    // ---- Özet sayaçları (konsol raporu için) ----
    public long[] StatusCounts { get; } = new long[7];
    public long[] PaymentCounts { get; } = new long[6];
    public Dictionary<int, (long Orders, decimal Revenue)> ByYear { get; } = new();
    public int[] OrdersPerCustomer { get; private set; } = Array.Empty<int>();
    public long DetailCount { get; private set; }
    public long CollectorOrders { get; private set; }
    public long PreOrderItemOrders { get; private set; }

    public OrderGenerator(Random random, BulkWriter writer, CatalogGenerator catalog, CustomerGenerator customers)
    {
        _random = random;
        _writer = writer;
        _customers = customers;

        // Alt kategori seçimi: Popularity; yaz aylarında SummerBoost kategoriler daha çok
        var subs = ReferenceData.SubCategories;
        _subNormal = new WeightedRandom<string>(subs.Select(s => (s.Name, s.Popularity)), random);
        _subSummer = new WeightedRandom<string>(
            subs.Select(s => (s.Name, s.Popularity * (s.SummerBoost ? Settings.SummerCategoryBoost : 1.0))), random);

        _pools = catalog.Products
            .GroupBy(p => p.SubCategory)
            .ToDictionary(g => g.Key, g => new SubCategoryPool(g));

        _basketSize = new WeightedRandom<int>(Settings.BasketSizes.Select(b => (b.Lines, b.Weight)), random);
        _paymentMethod = new WeightedRandom<byte>(Settings.PaymentMethods.Select(p => (p.PaymentMethodId, p.Weight)), random);
        _installments = new WeightedRandom<byte>(Settings.Installments.Select(i => (i.Count, i.Weight)), random);
        _city = new WeightedRandom<byte>(ReferenceData.Cities.Select(c => (c.Id, c.Weight)), random);
    }

    public void Run()
    {
        long[] orderTicks = BuildOrderTimes();
        OrdersPerCustomer = new int[_customers.CreatedTicks.Length];

        int total = orderTicks.Length;
        int orderDetailId = 0;

        for (int start = 0; start < total; start += Settings.BatchSize)
        {
            int count = Math.Min(Settings.BatchSize, total - start);

            var orders = NewOrdersTable();
            var details = NewDetailsTable();
            var payments = NewPaymentsTable();

            for (int k = 0; k < count; k++)
            {
                int orderId = start + k + 1;
                var orderDate = new DateTime(orderTicks[start + k]);
                BuildOrder(orderId, orderDate, orders, details, payments, ref orderDetailId);
            }

            _writer.Write(orders);
            _writer.Write(details);
            _writer.Write(payments);
            Console.WriteLine($"      ilerleme: {start + count:N0} / {total:N0} sipariş");
        }

        DetailCount = orderDetailId;
    }

    // =====================================================================
    // 1) SİPARİŞ ZAMANLARI
    // =====================================================================
    private long[] BuildOrderTimes()
    {
        var dayPicker = new WeightedRandom<DateTime>(DayWeights.Build(Settings.StartDate, Settings.EndDate), _random);
        var hourPicker = new WeightedRandom<int>(Settings.HourFactors.Select((w, h) => (h, w)), _random);

        var ticks = new long[Settings.OrderCount];
        for (int i = 0; i < ticks.Length; i++)
        {
            var t = dayPicker.Next()
                .AddHours(hourPicker.Next())
                .AddSeconds(_random.Next(3600));
            ticks[i] = t.Ticks;
        }

        Array.Sort(ticks);
        return ticks;
    }

    // =====================================================================
    // 2) TEK SİPARİŞ
    // =====================================================================
    private void BuildOrder(int orderId, DateTime orderDate, DataTable orders, DataTable details,
                            DataTable payments, ref int orderDetailId)
    {
        // ---- Müşteri: bu tarihe kadar kayıt olmuşlar arasından ağırlıklı ----
        int customerIndex = PickCustomer(orderDate);
        OrdersPerCustomer[customerIndex]++;
        if (_customers.IsCollector[customerIndex]) CollectorOrders++;

        byte shippingCity = _random.NextDouble() < Settings.OtherShippingCityRate
            ? _city.Next()
            : _customers.CityIds[customerIndex];

        // ---- Kalemler ----
        int lineCount = _basketSize.Next();
        var picked = new List<ProductRow>(lineCount);
        bool summer = orderDate.Month is >= 6 and <= 8;

        for (int attempt = 0; picked.Count < lineCount && attempt < lineCount * 6; attempt++)
        {
            var product = PickProduct(orderDate, summer);
            if (product is not null && !picked.Contains(product))
                picked.Add(product);
        }

        // Fiyat: bugünkü fiyat geçmişe enflasyonla indirgenir
        double yearsBeforeEnd = (Settings.EndDate - orderDate).TotalDays / 365.25;
        decimal priceFactor = (decimal)(1 / Math.Pow(Settings.YearlyPriceInflation, yearsBeforeEnd));

        decimal discount = 0m;
        if (DayWeights.IsBlackFridayWeek(orderDate) && _random.NextDouble() < Settings.BlackFridayDiscountChance)
            discount = 0.10m + (decimal)_random.NextDouble() * 0.15m;

        decimal totalAmount = 0m;
        DateTime latestRelease = DateTime.MinValue;

        foreach (var product in picked)
        {
            short quantity = PickQuantity(product);
            decimal unitPrice = RoundPrice(product.Price * priceFactor * (1 - discount));
            totalAmount += unitPrice * quantity;
            if (product.ReleaseDate > latestRelease) latestRelease = product.ReleaseDate;

            details.Rows.Add(++orderDetailId, orderId, product.ProductId, quantity, unitPrice);
        }

        // ---- Durum ----
        byte status = PickStatus(orderDate, latestRelease);
        if (latestRelease > orderDate) PreOrderItemOrders++;

        orders.Rows.Add(orderId, customerIndex + 1, shippingCity, orderDate, status, totalAmount);

        // ---- Ödeme (her siparişte tam 1 ödeme) ----
        byte method = _paymentMethod.Next();
        if (method == 4 && totalAmount > Settings.CashOnDeliveryMaxAmount) method = 1; // kapıda ödeme limiti

        byte installment = method == 1 && totalAmount >= Settings.InstallmentMinAmount
            ? _installments.Next()
            : (byte)1;

        DateTime paymentDate = method == 3
            ? orderDate.AddMinutes(_random.Next(30, 60 * 36))      // havale: 0,5-36 saat sonra
            : orderDate.AddSeconds(_random.Next(5, 180));           // kart/cüzdan: hemen
        if (paymentDate > Settings.EndDate) paymentDate = Settings.EndDate;

        payments.Rows.Add(orderId, orderId, method, totalAmount, installment, paymentDate);

        // ---- Sayaçlar ----
        StatusCounts[status]++;
        PaymentCounts[method]++;
        ByYear.TryGetValue(orderDate.Year, out var y);
        ByYear[orderDate.Year] = (y.Orders + 1, y.Revenue + totalAmount);
    }

    private int PickCustomer(DateTime orderDate)
    {
        // Bu tarihe kadar kayıt olan müşteri sayısı (CreatedTicks sıralı)
        int k = Array.BinarySearch(_customers.CreatedTicks, orderDate.Ticks);
        k = k < 0 ? ~k : k + 1;
        if (k == 0) k = 1;

        var cumulative = _customers.CumulativeWeights;
        double dart = _random.NextDouble() * cumulative[k - 1];
        int index = Array.BinarySearch(cumulative, 0, k, dart);
        return index < 0 ? Math.Min(~index, k - 1) : index;
    }

    private ProductRow? PickProduct(DateTime orderDate, bool summer)
    {
        string sub = summer ? _subSummer.Next() : _subNormal.Next();
        var product = _pools[sub].Pick(orderDate, _random);

        // Pasif ürünler kesim tarihinden sonra satılmaz
        if (product is not null && !product.IsActive && orderDate >= Settings.InactiveProductCutoff)
            return null;

        return product;
    }

    private short PickQuantity(ProductRow product)
    {
        if (product.Price >= 3_000m) return 1;
        if (product.SubCategory == "Kart Oyunu (TCG)") return (short)(1 + _random.Next(6)); // booster kutusu
        double r = _random.NextDouble();
        return r < 0.85 ? (short)1 : r < 0.97 ? (short)2 : (short)3;
    }

    private byte PickStatus(DateTime orderDate, DateTime latestRelease)
    {
        if (_random.NextDouble() < Settings.CancelRate) return Settings.StatusCancelled;

        // Ürün henüz çıkmadıysa sipariş ön siparişte bekler
        if (latestRelease > Settings.EndDate) return Settings.StatusPreOrder;

        // Ön siparişli ürün çıktıysa süreç çıkış tarihinden başlar
        DateTime processStart = latestRelease > orderDate ? latestRelease : orderDate;
        double age = (Settings.EndDate - processStart).TotalDays;

        if (age < Settings.PreparingDays)
            return _random.NextDouble() < 0.8 ? Settings.StatusPreparing : Settings.StatusShipped;
        if (age < Settings.ShippingDays)
            return _random.NextDouble() < 0.7 ? Settings.StatusShipped : Settings.StatusDelivered;

        return _random.NextDouble() < Settings.ReturnRate / (1 - Settings.CancelRate)
            ? Settings.StatusReturned
            : Settings.StatusDelivered;
    }

    private static decimal RoundPrice(decimal price) =>
        Math.Max(9.90m, Math.Round(price / 10m) * 10m - 0.10m);

    // =====================================================================
    // DataTable şablonları (kolon adları = tablo kolonları)
    // =====================================================================
    private static DataTable NewOrdersTable()
    {
        var t = new DataTable("Orders");
        t.Columns.Add("OrderId", typeof(int));
        t.Columns.Add("CustomerId", typeof(int));
        t.Columns.Add("ShippingCityId", typeof(byte));
        t.Columns.Add("OrderDate", typeof(DateTime));
        t.Columns.Add("StatusId", typeof(byte));
        t.Columns.Add("TotalAmount", typeof(decimal));
        return t;
    }

    private static DataTable NewDetailsTable()
    {
        var t = new DataTable("OrderDetails");
        t.Columns.Add("OrderDetailId", typeof(int));
        t.Columns.Add("OrderId", typeof(int));
        t.Columns.Add("ProductId", typeof(int));
        t.Columns.Add("Quantity", typeof(short));
        t.Columns.Add("UnitPrice", typeof(decimal));
        return t;
    }

    private static DataTable NewPaymentsTable()
    {
        var t = new DataTable("Payments");
        t.Columns.Add("PaymentId", typeof(int));
        t.Columns.Add("OrderId", typeof(int));
        t.Columns.Add("PaymentMethodId", typeof(byte));
        t.Columns.Add("Amount", typeof(decimal));
        t.Columns.Add("InstallmentCount", typeof(byte));
        t.Columns.Add("PaymentDate", typeof(DateTime));
        return t;
    }

    // =====================================================================
    // Bir alt kategorinin ürün havuzu.
    // Ürünler "satışa açılış" tarihine göre sıralı (çıkıştan PreOrderWindowDays önce).
    // Bir tarihte satılabilen ürünler = listenin başındaki k ürün → kümülatif ağırlıkla seçilir.
    // =====================================================================
    private sealed class SubCategoryPool
    {
        private readonly ProductRow[] _products;
        private readonly long[] _availableTicks;
        private readonly double[] _cumulative;

        public SubCategoryPool(IEnumerable<ProductRow> products)
        {
            _products = products
                .OrderBy(p => p.ReleaseDate)
                .ToArray();

            _availableTicks = _products
                .Select(p => p.ReleaseDate.AddDays(-Settings.PreOrderWindowDays).Ticks)
                .ToArray();

            _cumulative = new double[_products.Length];
            double sum = 0;
            for (int i = 0; i < _products.Length; i++)
            {
                sum += _products[i].Weight;
                _cumulative[i] = sum;
            }
        }

        public ProductRow? Pick(DateTime date, Random random)
        {
            int k = Array.BinarySearch(_availableTicks, date.Ticks);
            if (k < 0) k = ~k;
            else while (k < _availableTicks.Length && _availableTicks[k] <= date.Ticks) k++;
            if (k == 0) return null; // bu tarihte satılabilir ürün yok

            double dart = random.NextDouble() * _cumulative[k - 1];
            int index = Array.BinarySearch(_cumulative, 0, k, dart);
            return _products[index < 0 ? Math.Min(~index, k - 1) : index];
        }
    }
}
