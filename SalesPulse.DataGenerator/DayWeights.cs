namespace SalesPulse.DataGenerator;

// Her günün sipariş yoğunluğu: ay (sezon) × yıllık büyüme × hafta sonu × kampanya günleri
public static class DayWeights
{
    public static List<(DateTime Day, double Weight)> Build(DateTime start, DateTime end)
    {
        var list = new List<(DateTime, double)>();

        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            double weight = Settings.MonthFactors[day.Month - 1];

            // Sürekli büyüme: dönem başından itibaren yılda %20
            double years = (day - start).TotalDays / 365.25;
            weight *= Math.Pow(Settings.YearlyGrowth, years);

            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                weight *= Settings.WeekendFactor;

            weight *= CampaignFactor(day);
            list.Add((day, weight));
        }

        return list;
    }

    // 11.11 ve Black Friday haftası
    public static double CampaignFactor(DateTime day)
    {
        if (day.Month == 11 && day.Day == 11) return 2.0;

        var blackFriday = BlackFriday(day.Year);
        if (day == blackFriday) return 3.0;
        if (day > blackFriday && day <= blackFriday.AddDays(3)) return 1.8; // hafta sonu + Cyber Monday
        if (day >= blackFriday.AddDays(-4) && day < blackFriday) return 1.3; // erken indirimler

        return 1.0;
    }

    public static bool IsBlackFridayWeek(DateTime day)
    {
        var bf = BlackFriday(day.Year);
        return day.Date >= bf.AddDays(-4) && day.Date <= bf.AddDays(3);
    }

    // Kasım'ın 4. cuması
    public static DateTime BlackFriday(int year)
    {
        var first = new DateTime(year, 11, 1);
        int offset = ((int)DayOfWeek.Friday - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + 21);
    }
}
