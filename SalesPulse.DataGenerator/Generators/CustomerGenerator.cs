using System.Data;
using System.Text;

namespace SalesPulse.DataGenerator.Generators;

public enum CustomerTier
{
    Collector, // koleksiyoncu: çok sık alışveriş eder
    Regular,   // normal müşteri
    OneTime    // sadece 1 sipariş verir
}

// Sipariş üretirken müşteriden sadece bunlar lazım (isim/e-posta bellekte tutulmaz)
public readonly record struct CustomerRow(int CustomerId, byte CityId, DateTime CreatedDate, CustomerTier Tier);

// Müşterileri üretip veritabanına yazar.
// CustomerId kayıt tarihine göre artar: küçük Id = eski müşteri.
public class CustomerGenerator
{
    private readonly Random _random;
    private readonly BulkWriter _writer;

    // Sipariş üretici bunları kullanacak (hepsi kayıt tarihine göre sıralı)
    public List<CustomerRow> Customers { get; } = new(Settings.CustomerCount);

    public CustomerGenerator(Random random, BulkWriter writer)
    {
        _random = random;
        _writer = writer;
    }

    public void Run()
    {
        var table = new DataTable("Customers");
        table.Columns.Add("CustomerId", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("CityId", typeof(byte));
        table.Columns.Add("CreatedDate", typeof(DateTime));

        var cityPicker = new WeightedRandom<byte>(
            ReferenceData.Cities.Select(c => (c.Id, c.Weight)), _random);

        int id = 0;
        foreach (DateTime created in BuildSignupDates())
        {
            id++;
            string first = Pick(ReferenceData.FirstNames);
            string last = Pick(ReferenceData.LastNames);
            // Id eklendiği için e-posta her zaman benzersiz
            string email = $"{ToAscii(first)}.{ToAscii(last)}{id}@{Pick(ReferenceData.EmailDomains)}";
            byte cityId = cityPicker.Next();

            Customers.Add(new CustomerRow(id, cityId, created, PickTier(created)));
            table.Rows.Add(id, first, last, email, cityId, created);
        }

        _writer.Write(table);
    }

    // Kayıt tarihleri sıralı döner ki Id sırası = kayıt sırası olsun
    private List<DateTime> BuildSignupDates()
    {
        var dates = new List<DateTime>(Settings.CustomerCount);

        for (int i = 0; i < Settings.CustomerCount; i++)
        {
            dates.Add(_random.NextDouble() < Settings.PreStartCustomerShare
                ? RandomDate(Settings.CustomerSignupStart, Settings.StartDate)
                : RandomDate(Settings.StartDate, Settings.EndDate));
        }

        dates.Sort();
        return dates;
    }

    // Tek seferlikler sadece veri döneminde kayıt olanlardan seçilir;
    // eski müşteriler dönem başında sipariş üstüne sipariş vermesin.
    private CustomerTier PickTier(DateTime created)
    {
        double oneTimeChance = Settings.OneTimeRatio / (1 - Settings.PreStartCustomerShare);
        if (created >= Settings.StartDate && _random.NextDouble() < oneTimeChance)
            return CustomerTier.OneTime;

        double collectorChance = Settings.CollectorRatio / (1 - Settings.OneTimeRatio);
        return _random.NextDouble() < collectorChance ? CustomerTier.Collector : CustomerTier.Regular;
    }

    private DateTime RandomDate(DateTime from, DateTime to) =>
        from.AddSeconds(_random.NextInt64((long)(to - from).TotalSeconds));

    private T Pick<T>(T[] items) => items[_random.Next(items.Length)];

    // E-posta için Türkçe karakterleri sadeleştir: "Çağrı" → "cagri"
    private static string ToAscii(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            builder.Append(c switch
            {
                'ç' or 'Ç' => 'c',
                'ğ' or 'Ğ' => 'g',
                'ı' or 'I' or 'İ' => 'i',
                'ö' or 'Ö' => 'o',
                'ş' or 'Ş' => 's',
                'ü' or 'Ü' => 'u',
                _ => char.ToLowerInvariant(c)
            });
        }
        return builder.ToString();
    }
}
