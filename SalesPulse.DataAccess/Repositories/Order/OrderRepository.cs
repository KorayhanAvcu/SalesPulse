using System.Data;
using Dapper;
using SalesPulse.DataAccess.Connection;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.DataAccess.Repositories.Order;

// Order Explorer stored procedure'lerini Dapper ile çağırır
public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchFilter filter)
    {
        using var connection = _connectionFactory.CreateConnection();

        // SP iki sonuç kümesi döndürür: 1) toplam kayıt sayısı 2) istenen sayfanın satırları
        using var results = await connection.QueryMultipleAsync(
            "dbo.sp_Orders_Search",
            new
            {
                filter.Search,
                filter.StartDate,
                filter.EndDate,
                filter.CityId,
                filter.StatusId,
                filter.MinAmount,
                filter.MaxAmount,
                filter.SortColumn,
                filter.SortDir,
                filter.Page,
                filter.PageSize
            },
            commandType: CommandType.StoredProcedure);

        var totalCount = await results.ReadSingleAsync<int>();
        var items = await results.ReadAsync<OrderListItemDto>();

        return new PagedResult<OrderListItemDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDetailDto?> GetDetailAsync(int orderId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1) sipariş başlığı 2) sipariş kalemleri
        using var results = await connection.QueryMultipleAsync(
            "dbo.sp_Orders_GetDetail",
            new { OrderId = orderId },
            commandType: CommandType.StoredProcedure);

        var detail = await results.ReadSingleOrDefaultAsync<OrderDetailDto>();
        if (detail is null)
            return null; // böyle bir sipariş yok

        var lines = await results.ReadAsync<OrderLineDto>();
        detail.Lines = lines.ToList();

        return detail;
    }

    public async Task<List<OrderListItemDto>> ExportAsync(OrderSearchFilter filter, int maxRows)
    {
        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<OrderListItemDto>(
            "dbo.sp_Orders_Export",
            new
            {
                filter.StartDate,
                filter.EndDate,
                filter.CityId,
                filter.StatusId,
                filter.MinAmount,
                filter.MaxAmount,
                filter.Search,
                MaxRows = maxRows
            },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }
}