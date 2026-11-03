using SalesPulse.Entity.Dtos;

namespace SalesPulse.Business.Services.Order;

public interface IOrderService
{
    Task<PagedResult<OrderListItemDto>> SearchAsync(OrderSearchFilter filter);
    Task<OrderDetailDto?> GetDetailAsync(int orderId);
    Task<byte[]> ExportCsvAsync(OrderSearchFilter filter);
}
