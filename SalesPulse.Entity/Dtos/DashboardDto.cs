namespace SalesPulse.Entity.Dtos;

public class DashboardDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public KpiDto Kpis { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
    public List<TopCategoryDto> TopCategories { get; set; } = new();
    public List<TopCityDto> TopCities { get; set; } = new();
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
    public List<TopProductDto> TopProductsByQuantity { get; set; } = new();
    public List<TopProductDto> TopProductsByRevenue { get; set; } = new();
}