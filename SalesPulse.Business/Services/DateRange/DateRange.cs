using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.Business.Services.Helpers
{
    // Tarih aralığı kuralları: tek yerde, bütün servisler kullanır
    public static class DateRange
    {
        // Verimizin son günü (veri üretici 31.10.2026'ya kadar sipariş üretti)
        public static readonly DateTime DataEndDate = new(2026, 10, 31);

        // Tarih gelmezse: son 12 ay. Başlangıç bitişten büyükse yer değiştir.
        public static (DateTime Start, DateTime End) Normalize(DateTime? startDate, DateTime? endDate)
        {
            DateTime end = (endDate ?? DataEndDate).Date;
            DateTime start = (startDate ?? end.AddMonths(-12).AddDays(1)).Date;

            if (start > end)
                (start, end) = (end, start);

            return (start, end);
        }
    }
}
