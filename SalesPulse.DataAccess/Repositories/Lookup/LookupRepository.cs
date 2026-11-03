using System.Data;
using Dapper;
using SalesPulse.DataAccess.Connection;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.DataAccess.Repositories.Lookup;

// Küçük sabit tablolar (81 şehir, 6 durum) için basit SQL.
// Stored procedure yerine düz SQL de Dapper ile böyle çalıştırılır (CommandType.Text varsayılandır).
public class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LookupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrderLookupsDto> GetOrderLookupsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        // İki SELECT tek seferde gider, iki sonuç kümesi döner
        const string sql = @"
            SELECT CAST(CityId AS INT) AS Id, Name FROM dbo.Cities ORDER BY Name;
            SELECT CAST(StatusId AS INT) AS Id, Name FROM dbo.OrderStatuses ORDER BY StatusId;";

        using var results = await connection.QueryMultipleAsync(sql);

        return new OrderLookupsDto
        {
            Cities = (await results.ReadAsync<LookupItemDto>()).ToList(),
            Statuses = (await results.ReadAsync<LookupItemDto>()).ToList()
        };
    }
}
