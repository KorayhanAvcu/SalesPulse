using SalesPulse.Business.Services.Helpers;
using SalesPulse.DataAccess.Repositories.Dashboard;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.Business.Services.Dashboard;

// Dashboard için iş kuralları: tarih aralığını düzeltir, raporları toplar
public class DashboardService : IDashboardService
{
    private const int TopCount = 10;

    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync(DateTime? startDate, DateTime? endDate)
    {
        var (start, end) = DateRange.Normalize(startDate, endDate);

        // 1) HAFİF sorgular: sadece Orders + tarih indeksi. Aynı anda çalışabilirler.
        var kpisTask   = _dashboardRepository.GetKpisAsync(start, end);
        var trendTask  = _dashboardRepository.GetMonthlyTrendAsync(end);
        var citiesTask = _dashboardRepository.GetTopCitiesAsync(start, end, TopCount);
        var statusTask = _dashboardRepository.GetStatusDistributionAsync(start, end);

        await Task.WhenAll(kpisTask, trendTask, citiesTask, statusTask);

        // 2) AĞIR sorgular: Orders + 3,5 milyon satırlık OrderDetails JOIN'i.
        // Aynı anda gönderilirse SQL Server Express'in sınırlı belleği için yarışıp
        // birbirini bekletiyorlar (zaman aşımı). Bu yüzden SIRAYLA çalıştırıyoruz.
        var topCategories = await _dashboardRepository.GetTopCategoriesAsync(start, end, TopCount);
        var byQuantity    = await _dashboardRepository.GetTopProductsAsync(start, end, TopCount, "quantity");
        var byRevenue     = await _dashboardRepository.GetTopProductsAsync(start, end, TopCount, "revenue");

        return new DashboardDto
        {
            StartDate = start,
            EndDate = end,
            Kpis = await kpisTask,
            MonthlyTrend = await trendTask,
            TopCities = await citiesTask,
            StatusDistribution = await statusTask,
            TopCategories = topCategories,
            TopProductsByQuantity = byQuantity,
            TopProductsByRevenue = byRevenue
        };
    }
}
