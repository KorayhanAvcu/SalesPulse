using SalesPulse.Entity.Dtos;

namespace SalesPulse.DataAccess.Repositories.Order;

public interface IOrderRepository
{
    Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchFilter filter);
    Task<OrderDetailDto?> GetDetailAsync(int orderId);
    Task<List<OrderListItemDto>> ExportAsync(OrderSearchFilter filter, int maxRows);
}