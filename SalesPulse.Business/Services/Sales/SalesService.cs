using SalesPulse.Business.Services.Helpers;
using SalesPulse.DataAccess.Repositories.Dashboard;
using SalesPulse.DataAccess.Repositories.Sales;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.Business.Services.Sales;

// Sales Intelligence için iş kuralları: raporları toplar, bir önceki yılla karşılaştırır
public class SalesService : ISalesService
{
    private readonly ISalesRepository _salesRepository;
    private readonly IDashboardRepository _dashboardRepository;

    public SalesService(ISalesRepository salesRepository, IDashboardRepository dashboardRepository)
    {
        _salesRepository = salesRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<SalesIntelligenceDto> GetSalesIntelligenceAsync(DateTime? startDate, DateTime? endDate)
    {
        var (start, end) = DateRange.Normalize(startDate, endDate);

        // Karşılaştırma: seçilen dönem (B) ile bir yıl önceki aynı dönem (A)
        var aStart = start.AddYears(-1);
        var aEnd = end.AddYears(-1);

        // 1) HAFİF sorgular (sadece Orders): aynı anda
        var customersTask  = _salesRepository.GetTopCustomersAsync(start, end, 20);
        var repeatTask     = _salesRepository.GetRepeatCustomerRateAsync(start, end);
        var comparisonTask = _salesRepository.GetPeriodComparisonAsync(aStart, aEnd, start, end);
        var citiesTask     = _dashboardRepository.GetTopCitiesAsync(start, end, 10);

        await Task.WhenAll(customersTask, repeatTask, comparisonTask, citiesTask);

        // 2) AĞIR sorgular (OrderDetails JOIN'i): sırayla
        var categoryMonthly = await _salesRepository.GetCategoryMonthlyAsync(start, end);
        var topProducts     = await _dashboardRepository.GetTopProductsAsync(start, end, 10, "revenue");

        return new SalesIntelligenceDto
        {
            StartDate = start,
            EndDate = end,
            TopCustomers = await customersTask,
            RepeatRate = await repeatTask,
            Comparison = await comparisonTask,
            TopCities = await citiesTask,
            CategoryMonthly = categoryMonthly,
            TopProducts = topProducts
        };
    }
}
