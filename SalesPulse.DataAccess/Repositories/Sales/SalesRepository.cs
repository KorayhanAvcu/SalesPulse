using System.Data;      // CommandType için
using Dapper;           // QueryAsync, QueryFirstAsync için
using SalesPulse.DataAccess.Connection;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.DataAccess.Repositories.Sales
{
    public class SalesRepository : ISalesRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SalesRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<TopCustomerDto>> GetTopCustomersAsync(DateTime startDate, DateTime endDate, int top)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<TopCustomerDto>(
                "dbo.sp_Sales_TopCustomers",
                new { StartDate = startDate, EndDate = endDate, Top = top },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<List<CategoryMonthlyDto>> GetCategoryMonthlyAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<CategoryMonthlyDto>(
                "dbo.sp_Sales_CategoryMonthly",
                new { StartDate = startDate, EndDate = endDate },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<RepeatCustomerRateDto> GetRepeatCustomerRateAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryFirstAsync<RepeatCustomerRateDto>(
                "dbo.sp_Sales_RepeatCustomerRate",
                new { StartDate = startDate, EndDate = endDate },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<PeriodComparisonDto?> GetPeriodComparisonAsync(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        {
            using var connection = _connectionFactory.CreateConnection();

            // Dönemlerden birinde hiç sipariş yoksa SP satır döndürmez -> null
            return await connection.QueryFirstOrDefaultAsync<PeriodComparisonDto>(
                "dbo.sp_Sales_PeriodComparison",
                new { AStartDate = aStart, AEndDate = aEnd, BStartDate = bStart, BEndDate = bEnd },
                commandType: CommandType.StoredProcedure);
        }
    }
}
