using System.Globalization;
using System.Text;
using SalesPulse.DataAccess.Repositories.Lookup;
using SalesPulse.DataAccess.Repositories.Order;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.Business.Services.Order;

// Order Explorer için iş kuralları: filtre sınırları ve CSV üretimi
public class OrderService : IOrderService
{
    private const int MaxPageSize = 100;
    private const int MaxExportRows = 10_000;

    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "OrderId", "Customer", "City", "OrderDate", "TotalAmount", "Status"
    };

    private readonly IOrderRepository _orderRepository;
    private readonly ILookupRepository _lookupRepository;

    public OrderService(IOrderRepository orderRepository, ILookupRepository lookupRepository)
    {
        _orderRepository = orderRepository;
        _lookupRepository = lookupRepository;
    }

    // Filtre açılır listeleri (şehirler, durumlar)
    public Task<OrderLookupsDto> GetLookupsAsync()
    {
        return _lookupRepository.GetOrderLookupsAsync();
    }

    public Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchFilter filter)
    {
        Sanitize(filter);
        return _orderRepository.SearchAsync(filter);
    }

    public Task<OrderDetailDto?> GetDetailAsync(int orderId)
    {
        return _orderRepository.GetDetailAsync(orderId);
    }

    public async Task<byte[]> ExportCsvAsync(OrderSearchFilter filter)
    {
        Sanitize(filter);
        var rows = await _orderRepository.ExportAsync(filter, MaxExportRows);

        var tr = new CultureInfo("tr-TR");
        var csv = new StringBuilder();

        // Türkçe Excel ayırıcı olarak ';' bekler (',' ondalık ayırıcı)
        csv.AppendLine("Sipariş No;Müşteri;Şehir;Tarih;Tutar;Ödeme Yöntemi;Durum");

        foreach (var r in rows)
        {
            csv.AppendLine(string.Join(';',
                r.OrderId,
                Escape(r.CustomerName),
                Escape(r.CityName),
                r.OrderDate.ToString("dd.MM.yyyy HH:mm", tr),
                r.TotalAmount.ToString("0.00", tr),
                Escape(r.PaymentMethod ?? ""),
                Escape(r.StatusName)));
        }

        // UTF-8 + BOM: Excel Türkçe karakterleri (ş, ğ, ı) doğru göstersin
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(csv.ToString());
        return preamble.Concat(body).ToArray();
    }

    // Dışarıdan gelen değerleri güvenli sınırlara çek
    private static void Sanitize(OrderSearchFilter filter)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1 || filter.PageSize > MaxPageSize) filter.PageSize = 25;
        if (!AllowedSortColumns.Contains(filter.SortColumn)) filter.SortColumn = "OrderDate";
        filter.SortDir = string.Equals(filter.SortDir, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        if (filter.MinAmount > filter.MaxAmount)
            (filter.MinAmount, filter.MaxAmount) = (filter.MaxAmount, filter.MinAmount);

        if (filter.StartDate > filter.EndDate)
            (filter.StartDate, filter.EndDate) = (filter.EndDate, filter.StartDate);
    }

    // CSV'de ';' veya '"' içeren değerleri tırnak içine al
    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
