using SalesPulse.Entity.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.DataAccess.Repositories.Dashboard
{
    public interface IDashboardRepository
    {
        Task<KpiDto> GetKpisAsync(DateTime startDate, DateTime endDate);
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(DateTime endDate);
        Task<List<TopCategoryDto>> GetTopCategoriesAsync(DateTime startDate, DateTime endDate, int top);
        Task<List<TopCityDto>> GetTopCitiesAsync(DateTime startDate, DateTime endDate, int top);
        Task<List<StatusDistributionDto>> GetStatusDistributionAsync(DateTime startDate, DateTime endDate);
        Task<List<TopProductDto>> GetTopProductsAsync(DateTime startDate, DateTime endDate, int top, string orderBy);
    }
}
