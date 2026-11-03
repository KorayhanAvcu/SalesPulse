namespace SalesPulse.Entity.Dtos;

// Sales Intelligence sayfasının ihtiyaç duyduğu her şey tek pakette
public class SalesIntelligenceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<TopCityDto> TopCities { get; set; } = new();
    public List<CategoryMonthlyDto> CategoryMonthly { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public RepeatCustomerRateDto RepeatRate { get; set; } = new();
    public PeriodComparisonDto? Comparison { get; set; }
}