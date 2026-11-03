namespace SalesPulse.Entity.Dtos;


public class KpiDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public decimal AverageBasket { get; set; }
}