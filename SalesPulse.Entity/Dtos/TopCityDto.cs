namespace SalesPulse.Entity.Dtos;

// sp_Dashboard_TopCities -> en çok satış yapılan 10 şehir
public class TopCityDto
{
    public byte CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}