using Dapper;
using SalesPulse.DataAccess.Connection;
using SalesPulse.Entity.Dtos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.DataAccess.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DashboardRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<KpiDto> GetKpisAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstAsync<KpiDto>(
            "dbo.sp_Dashboard_Kpis",
            new { StartDate = startDate, EndDate = endDate },
            commandType: CommandType.StoredProcedure);
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(DateTime endDate)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<MonthlyTrendDto>(
                "dbo.sp_Dashboard_MonthlyTrend",
                new { EndDate = endDate },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<List<StatusDistributionDto>> GetStatusDistributionAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<StatusDistributionDto>(
                "dbo.sp_Dashboard_StatusDistribution",
                new { StartDate = startDate, EndDate = endDate },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<List<TopCategoryDto>> GetTopCategoriesAsync(DateTime startDate, DateTime endDate, int top)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<TopCategoryDto>(
                "dbo.sp_Dashboard_TopCategories",
                new { StartDate = startDate, EndDate = endDate, Top = top },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }


        public async Task<List<TopCityDto>> GetTopCitiesAsync(DateTime startDate, DateTime endDate, int top)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<TopCityDto>(
                "dbo.sp_Dashboard_TopCities",
                new { StartDate = startDate, EndDate = endDate, Top = top },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime startDate, DateTime endDate, int top, string orderBy)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<TopProductDto>(
                "dbo.sp_Dashboard_TopProducts",
                new { StartDate = startDate, EndDate = endDate, Top = top, OrderBy = orderBy },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }
    }
}
