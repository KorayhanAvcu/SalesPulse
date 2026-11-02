using System.Data;
using System.Text;

namespace SalesPulse.DataGenerator.Generators;

// Müşterileri üretir ve veritabanına yazar.
//
// Müşteriler kayıt tarihine göre sıralı üretilir (CustomerId küçük = eski müşteri).
// Sipariş üretici bu sıralamayı kullanır: bir sipariş tarihinde sadece o tarihe kadar
// kayıt olmuş müşteriler seçilebilir. Böylece hiçbir müşteri kayıt olmadan sipariş vermez.
public class CustomerGenerator
{
    private readonly Random _random;
    private readonly BulkWriter _writer;

    // Sipariş üretici için bellekte tutulanlar (index = CustomerId - 1)
    public long[] CreatedTicks { get; private set; } = Array.Empty<long>();
    public byte[] CityIds { get; private set; } = Array.Empty<byte>();
    public bool[] IsCollector { get; private set; } = Array.Empty<bool>();

    // Kümülatif sipariş ağırlığı: CumulativeWeights[i] = 0..i arası müşterilerin ağırlık toplamı.
    // "Şu tarihe kadar kayıt olmuş ilk k müşteri" içinden ağırlıklı seçim için.
    public double[] CumulativeWeights { get; private set; } = Array.Empty<double>();

    public CustomerGenerator(Random random, BulkWriter writer)
    {
        _random = random;
        _writer = writer;
    }

    public void Run()
    {
        int n = Settings.CustomerCount;
        var cityPicker = new WeightedRandom<byte>(ReferenceData.Cities.Select(c => (c.Id, c.Weight)), _random);

        // Yeni müşteriler dönem boyunca sipariş yoğunluğuyla aynı ritimde katılır
        var days = DayWeights.Build(Settings.StartDate, Settings.EndDate);
        var dayPicker = new WeightedRandom<DateTime>(days, _random);
        int historyDays = (Settings.StartDate - Settings.CustomerHistoryStart).Days;

        // 1) Kayıt tarihleri
        var created = new long[n];
        for (int i = 0; i < n; i++)
        {
            DateTime day = _random.NextDouble() < Settings.ExistingCustomerRatio
                ? Settings.CustomerHistoryStart.AddDays(_random.Next(historyDays))
                : dayPicker.Next();
            created[i] = day.AddSeconds(_random.Next(86_400)).Ticks;
        }
        Array.Sort(created); // CustomerId sırası = kayıt sırası

        // 2) Şehir, koleksiyoncu bayrağı, temel ağırlık
        var cities = new byte[n];
        var collector = new bool[n];
        var weights = new double[n];
        double collectorSum = 0, normalSum = 0;

        for (int i = 0; i < n; i++)
        {
            cities[i] = cityPicker.Next();
            collector[i] = _random.NextDouble() < Settings.CollectorRatio;

            // Log-uniform 1..~55: çoğu müşteri az, bazıları sık alışveriş yapar
            weights[i] = Math.Exp(_random.NextDouble() * 4);
            if (collector[i]) collectorSum += weights[i];
            else normalSum += weights[i];
        }

        // 3) Koleksiyoncuların toplam ağırlığı siparişlerin ~%60'ını alacak şekilde ölçekle
        double collectorFactor = (Settings.CollectorOrderShare / (1 - Settings.CollectorOrderShare))
                                 * normalSum / collectorSum;
        var cumulative = new double[n];
        double running = 0;
        for (int i = 0; i < n; i++)
        {
            if (collector[i]) weights[i] *= collectorFactor;
            running += weights[i];
            cumulative[i] = running;
        }

        CreatedTicks = created;
        CityIds = cities;
        IsCollector = collector;
        CumulativeWeights = cumulative;

        Write();
    }

    private void Write()
    {
        var table = new DataTable("Customers");
        table.Columns.Add("CustomerId", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("CityId", typeof(byte));
        table.Columns.Add("CreatedDate", typeof(DateTime));

        for (int i = 0; i < CreatedTicks.Length; i++)
        {
            int id = i + 1;
            string first = ReferenceData.FirstNames[_random.Next(ReferenceData.FirstNames.Length)];
            string last = ReferenceData.LastNames[_random.Next(ReferenceData.LastNames.Length)];
            string domain = ReferenceData.EmailDomains[_random.Next(ReferenceData.EmailDomains.Length)];

            // Id eklendiği için e-posta her zaman benzersiz (UQ_Customers_Email)
            string email = _random.Next(3) switch
            {
                0 => $"{Ascii(first)}.{Ascii(last)}{id}@{domain}",
                1 => $"{Ascii(first)}{Ascii(last)}{id}@{domain}",
                _ => $"{Ascii(first)[0]}{Ascii(last)}{id}@{domain}"
            };

            table.Rows.Add(id, first, last, email, CityIds[i], new DateTime(CreatedTicks[i]));
        }

        _writer.Write(table);
    }

    // "Çağrı Güneş" → "cagri" / "gunes"
    private static string Ascii(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s.ToLowerInvariant())
        {
            sb.Append(c switch
            {
                'ç' => 'c', 'ğ' => 'g', 'ı' => 'i', 'i' => 'i', 'ö' => 'o', 'ş' => 's', 'ü' => 'u',
                'â' => 'a', 'î' => 'i', 'û' => 'u',
                _ => c
            });
        }
        return sb.Replace("i̇", "i").Replace(" ", "").ToString();
    }
}
