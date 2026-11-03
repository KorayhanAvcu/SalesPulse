using SalesPulse.Entity.Dtos;

namespace SalesPulse.Business.Services.Sales;

public interface ISalesService
{
    Task<SalesIntelligenceDto> GetSalesIntelligenceAsync(DateTime? startDate, DateTime? endDate);
}
