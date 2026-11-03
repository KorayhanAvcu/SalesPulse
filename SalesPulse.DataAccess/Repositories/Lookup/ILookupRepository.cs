using SalesPulse.Entity.Dtos;

namespace SalesPulse.DataAccess.Repositories.Lookup;

public interface ILookupRepository
{
    Task<OrderLookupsDto> GetOrderLookupsAsync();
}
