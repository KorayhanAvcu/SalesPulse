using SalesPulse.Business.Services.Helpers;
using SalesPulse.DataAccess.Repositories.Dashboard;
using SalesPulse.Entity.Dtos;


namespace SalesPulse.Business.Services.Dashboard
{
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

            // Sorgular birbirinden bağımsız: sırayla beklemek yerine hepsini başlat, sonra bekle
            var kpisTask = _dashboardRepository.GetKpisAsync(start, end);
            var trendTask = _dashboardRepository.GetMonthlyTrendAsync(end);
            var categoriesTask = _dashboardRepository.GetTopCategoriesAsync(start, end, TopCount);
            var citiesTask = _dashboardRepository.GetTopCitiesAsync(start, end, TopCount);
            var statusTask = _dashboardRepository.GetStatusDistributionAsync(start, end);
            var byQuantityTask = _dashboardRepository.GetTopProductsAsync(start, end, TopCount, "quantity");
            var byRevenueTask = _dashboardRepository.GetTopProductsAsync(start, end, TopCount, "revenue");

            await Task.WhenAll(kpisTask, trendTask, categoriesTask, citiesTask, statusTask, byQuantityTask, byRevenueTask);

            return new DashboardDto
            {
                StartDate = start,
                EndDate = end,
                Kpis = await kpisTask,
                MonthlyTrend = await trendTask,
                TopCategories = await categoriesTask,
                TopCities = await citiesTask,
                StatusDistribution = await statusTask,
                TopProductsByQuantity = await byQuantityTask,
                TopProductsByRevenue = await byRevenueTask
            };
        }
    }
}