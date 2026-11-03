namespace SalesPulse.Entity.Dtos;

public class MonthlyTrendDto
{
    public DateTime Month { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}