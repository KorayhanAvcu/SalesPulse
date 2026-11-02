using System.Data;

namespace SalesPulse.DataGenerator.Generators;

// Sipariş, sipariş kalemi ve ödemeleri üretir.
// Siparişler tarih sırasıyla üretilir (OrderId sırası = tarih sırası) ve
// bellek şişmesin diye Settings.BatchSize'lık parçalar halinde yazılır.
public class OrderGenerator
{
    private readonly Random _random;
    private readonly BulkWriter _writer;
    private readonly List<ProductRow> _products;

    // Kayıt tarihine göre sıralı müşteri havuzları
    private readonly CustomerRow[] _collectors;
    private readonly CustomerRow[] _regulars;
    private readonly CustomerRow[] _oneTimers;
    private int _collectorPool;  // o ana kadar kayıt olmuş koleksiyoncu sayısı
    private int _regularPool;    // o ana kadar kayıt olmuş normal müşteri sayısı
    private int _nextOneTimer;   // sıradaki tek seferlik müşteri

    // Ürünün satış ağırlığı: alt kategorinin payı, kategori içindeki popülerliğe bölünür
    private readonly Dictionary<int, double> _baseWeights = new();
    private readonly HashSet<int> _summerProducts = new();

    private readonly WeightedRandom<byte> _cityPicker;
    private readonly WeightedRandom<int> _basketPicker;
    private readonly WeightedRandom<short> _quantityPicker;
    private readonly WeightedRandom<byte> _paymentPicker;
    private readonly WeightedRandom<byte> _installmentPicker;
    private readonly WeightedRandom<int> _hourPicker;

    private readonly DataTable _orders = new("Orders");
    private readonly DataTable _details = new("OrderDetails");
    private readonly DataTable _payments = new("Payments");

    private int _orderId;
    private int _detailId;

    // Özet için sayaçlar
    public int OrderCount => _orderId;
    public int DetailCount => _detailId;
    public decimal TotalRevenue { get; private set; }
    public Dictionary<byte, int> StatusCounts { get; } = new();

    public OrderGenerator(Random random, BulkWriter writer, List<ProductRow> products, List<CustomerRow> customers)
    {
        _random = random;
        _writer = writer;
        _products = products;

        _collectors = customers.Where(c => c.Tier == CustomerTier.Collector).ToArray();
        _regulars = customers.Where(c => c.Tier == CustomerTier.Regular).ToArray();
        _oneTimers = customers.Where(c => c.Tier == CustomerTier.OneTime).ToArray();

        _cityPicker = new(ReferenceData.Cities.Select(c => (c.Id, c.Weight)), random);
        _basketPicker = new(Settings.BasketSizes.Select(b => (b.Lines, b.Weight)), random);
        _quantityPicker = new(Settings.Quantities.Select(q => (q.Quantity, q.Weight)), random);
        _paymentPicker = new(Settings.PaymentMethods.Select(p => (p.PaymentMethodId, p.Weight)), random);
        _installmentPicker = new(Settings.Installments.Select(i => (i.Count, i.Weight)), random);
        _hourPicker = new(Settings.HourWeights.Select((weight, hour) => (hour, weight)), random);

        BuildProductWeights();
        CreateTables();
    }

    public void Run()
    {
        int[] dailyCounts = BuildDailyCounts();
        int days = dailyCounts.Length;
        int nextReport = 500_000;

        for (int i = 0; i < days; i++)
        {
            DateTime day = Settings.StartDate.AddDays(i);
            var productPicker = BuildProductPicker(day);
            decimal priceFactor = PriceFactor(day);

            foreach (int second in BuildOrderSeconds(dailyCounts[i]))
            {
                AddOrder(day.AddSeconds(second), productPicker, priceFactor);

                if (_orders.Rows.Count >= Settings.BatchSize)
                    Flush();

                if (_orderId >= nextReport)
                {
                    Console.WriteLine($"   {_orderId,9:N0} sipariş üretildi...");
                    nextReport += 500_000;
                }
            }
        }

        Flush();
    }

    // =====================================================================
    // 1) TARİH: günlük sipariş sayıları ve saatler
    // =====================================================================

    // Sezon, yıllık büyüme ve hafta sonu etkisiyle her güne düşen sipariş sayısı.
    // Yuvarlama artıkları taşınır; toplam tam olarak Settings.OrderCount olur.
    private static int[] BuildDailyCounts()
    {
        int days = (Settings.EndDate.Date - Settings.StartDate.Date).Days + 1;
        var weights = new double[days];

        for (int i = 0; i < days; i++)
        {
            DateTime day = Settings.StartDate.AddDays(i);
            bool weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            weights[i] = Settings.MonthFactors[day.Month - 1]
                       * Math.Pow(Settings.YearlyGrowth, day.Year - Settings.StartDate.Year)
                       * (weekend ? Settings.WeekendFactor : 1.0);
        }

        double total = weights.Sum();
        var counts = new int[days];
        double cumulative = 0;
        int assigned = 0;

        for (int i = 0; i < days; i++)
        {
            cumulative += weights[i];
            int target = (int)Math.Round(Settings.OrderCount * cumulative / total);
            counts[i] = target - assigned;
            assigned = target;
        }

        return counts;
    }

    // O günün siparişlerinin gün içindeki saniyeleri (sıralı)
    private int[] BuildOrderSeconds(int count)
    {
        var seconds = new int[count];
        for (int i = 0; i < count; i++)
            seconds[i] = _hourPicker.Next() * 3600 + _random.Next(3600);

        Array.Sort(seconds);
        return seconds;
    }

    // =====================================================================
    // 2) ÜRÜN SEÇİMİ: hangi gün hangi ürün ne kadar satılır?
    // =====================================================================

    // Kategori payları ReferenceData'daki Popularity'ye uysun diye
    // ürün ağırlıkları kendi alt kategorisi içinde orantılanır.
    private void BuildProductWeights()
    {
        var subs = ReferenceData.SubCategories.ToDictionary(s => s.Name);

        foreach (var group in _products.GroupBy(p => p.SubCategory))
        {
            var sub = subs[group.Key];
            double total = group.Sum(p => p.Weight);

            foreach (var product in group)
            {
                _baseWeights[product.ProductId] = sub.Popularity * product.Weight / total;
                if (sub.SummerBoost)
                    _summerProducts.Add(product.ProductId);
            }
        }
    }

    // O gün satılabilen ürünler ve o güne özel ağırlıkları
    private WeightedRandom<ProductRow> BuildProductPicker(DateTime day)
    {
        bool summer = day.Month is 6 or 7 or 8;
        var items = new List<(ProductRow, double)>(_products.Count);

        foreach (var product in _products)
        {
            if (!product.IsActive && day >= Settings.InactiveCutoff)
                continue;

            int daysToRelease = (product.ReleaseDate - day).Days;
            if (daysToRelease > Settings.PreOrderWindowDays)
                continue; // henüz ön siparişe bile açılmadı

            double weight = _baseWeights[product.ProductId];

            if (daysToRelease > 0)
                weight *= Settings.PreOrderFactor;
            else if (-daysToRelease <= Settings.NewReleaseDays)
                weight *= Settings.NewReleaseBoost;

            if (summer && _summerProducts.Contains(product.ProductId))
                weight *= Settings.SummerCategoryBoost;

            items.Add((product, weight));
        }

        return new WeightedRandom<ProductRow>(items, _random);
    }

    // Products.Price bugünkü fiyat; geçmiş tarihlerde enflasyon kadar düşük
    private static decimal PriceFactor(DateTime day)
    {
        double years = (Settings.EndDate - day).TotalDays / 365.0;
        double factor = 1 / Math.Pow(Settings.YearlyPriceIncrease, years);

        if (day.Month == 11)
            factor *= 1 - Settings.NovemberDiscount;

        return (decimal)factor;
    }

    // =====================================================================
    // 3) MÜŞTERİ SEÇİMİ
    // =====================================================================
    private CustomerRow PickCustomer(DateTime orderDate)
    {
        // Tarih ilerledikçe yeni kayıt olanlar havuza girer;
        // böylece hiçbir müşteri kayıt olmadan önce sipariş veremez.
        while (_collectorPool < _collectors.Length && _collectors[_collectorPool].CreatedDate <= orderDate)
            _collectorPool++;
        while (_regularPool < _regulars.Length && _regulars[_regularPool].CreatedDate <= orderDate)
            _regularPool++;

        // Tek seferlik müşteri kayıt olduktan hemen sonra siparişini verir, bir daha gelmez
        if (_nextOneTimer < _oneTimers.Length && _oneTimers[_nextOneTimer].CreatedDate <= orderDate)
            return _oneTimers[_nextOneTimer++];

        return _random.NextDouble() < Settings.CollectorOrderShare
            ? _collectors[LoyalIndex(_collectorPool)]
            : _regulars[LoyalIndex(_regularPool)];
    }

    // Havuzdan seçim: küçük index (eski müşteri) daha sık çıkar
    private int LoyalIndex(int pool) =>
        (int)(pool * Math.Pow(_random.NextDouble(), Settings.LoyaltySkew));

    // =====================================================================
    // 4) SİPARİŞ
    // =====================================================================
    private void AddOrder(DateTime orderDate, WeightedRandom<ProductRow> productPicker, decimal priceFactor)
    {
        CustomerRow customer = PickCustomer(orderDate);
        int orderId = ++_orderId;

        byte cityId = _random.NextDouble() < Settings.OtherCityShippingRate
            ? _cityPicker.Next()
            : customer.CityId;

        decimal total = 0;
        DateTime lastRelease = DateTime.MinValue;
        int lines = _basketPicker.Next();
        var picked = new List<ProductRow>(lines);

        for (int i = 0; i < lines; i++)
        {
            // Aynı ürün bir siparişte iki kalem olmasın; çıkarsa kalem eksik kalır
            ProductRow product = productPicker.Next();
            if (picked.Contains(product))
                continue;
            picked.Add(product);

            short quantity = _quantityPicker.Next();
            decimal unitPrice = CatalogGenerator.RoundPrice(product.Price * priceFactor);

            _details.Rows.Add(++_detailId, orderId, product.ProductId, quantity, unitPrice);

            total += unitPrice * quantity;
            if (product.ReleaseDate > lastRelease)
                lastRelease = product.ReleaseDate;
        }

        byte status = PickStatus(orderDate, lastRelease);

        _orders.Rows.Add(orderId, customer.CustomerId, cityId, orderDate, status, total);
        AddPayment(orderId, orderDate, total);

        TotalRevenue += total;
        StatusCounts[status] = StatusCounts.GetValueOrDefault(status) + 1;
    }

    // Durum, veri döneminin sonundaki (EndDate) hali gösterir:
    // eski siparişler teslim edilmiş, son günlerdekiler hâlâ yolda olur.
    private byte PickStatus(DateTime orderDate, DateTime lastRelease)
    {
        if (_random.NextDouble() < Settings.CancelRate)
            return Settings.StatusCancelled;

        // Sepette henüz çıkmamış ürün varsa sipariş bekler
        if (lastRelease > Settings.EndDate)
            return Settings.StatusPreOrder;

        // Hazırlık, sipariş veya ürünün çıkış tarihi (hangisi geçse) başlar
        DateTime start = lastRelease > orderDate ? lastRelease : orderDate;
        double age = (Settings.EndDate - start).TotalDays;

        if (age < _random.Next(1, 4))
            return Settings.StatusPreparing;
        if (age < _random.Next(4, 9))
            return Settings.StatusShipped;

        return _random.NextDouble() < Settings.ReturnRate
            ? Settings.StatusReturned
            : Settings.StatusDelivered;
    }

    // =====================================================================
    // 5) ÖDEME: her siparişin bir ödemesi var (PaymentId = OrderId)
    // =====================================================================
    private void AddPayment(int orderId, DateTime orderDate, decimal amount)
    {
        byte method = _paymentPicker.Next();

        byte installments = method == Settings.PaymentCreditCard && amount >= Settings.InstallmentMinAmount
            ? _installmentPicker.Next()
            : (byte)1;

        DateTime paymentDate = method switch
        {
            Settings.PaymentBankTransfer   => orderDate.AddSeconds(_random.Next(Settings.TransferMaxDelayHours * 3600)),
            Settings.PaymentCashOnDelivery => orderDate.AddDays(Settings.CashOnDeliveryDays),
            _                              => orderDate
        };
        if (paymentDate > Settings.EndDate)
            paymentDate = Settings.EndDate;

        _payments.Rows.Add(orderId, orderId, method, amount, installments, paymentDate);
    }

    // =====================================================================
    // 6) YAZMA: FK sırası önemli, önce Orders
    // =====================================================================
    private void Flush()
    {
        _writer.Write(_orders, quiet: true);
        _writer.Write(_details, quiet: true);
        _writer.Write(_payments, quiet: true);

        _orders.Clear();
        _details.Clear();
        _payments.Clear();
    }

    private void CreateTables()
    {
        _orders.Columns.Add("OrderId", typeof(int));
        _orders.Columns.Add("CustomerId", typeof(int));
        _orders.Columns.Add("ShippingCityId", typeof(byte));
        _orders.Columns.Add("OrderDate", typeof(DateTime));
        _orders.Columns.Add("StatusId", typeof(byte));
        _orders.Columns.Add("TotalAmount", typeof(decimal));

        _details.Columns.Add("OrderDetailId", typeof(int));
        _details.Columns.Add("OrderId", typeof(int));
        _details.Columns.Add("ProductId", typeof(int));
        _details.Columns.Add("Quantity", typeof(short));
        _details.Columns.Add("UnitPrice", typeof(decimal));

        _payments.Columns.Add("PaymentId", typeof(int));
        _payments.Columns.Add("OrderId", typeof(int));
        _payments.Columns.Add("PaymentMethodId", typeof(byte));
        _payments.Columns.Add("Amount", typeof(decimal));
        _payments.Columns.Add("InstallmentCount", typeof(byte));
        _payments.Columns.Add("PaymentDate", typeof(DateTime));
    }
}
