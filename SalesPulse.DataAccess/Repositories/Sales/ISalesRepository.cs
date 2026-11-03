using SalesPulse.Entity.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.DataAccess.Repositories.Sales
{
    public interface ISalesRepository
    {
        Task<List<TopCustomerDto>> GetTopCustomersAsync(DateTime startDate, DateTime endDate, int top);
        Task<List<CategoryMonthlyDto>> GetCategoryMonthlyAsync(DateTime startDate, DateTime endDate);
        Task<RepeatCustomerRateDto> GetRepeatCustomerRateAsync(DateTime startDate, DateTime endDate);
        Task<PeriodComparisonDto?> GetPeriodComparisonAsync(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd);
    }
}
