namespace SalesPulse.Entity.Dtos;

// sp_Sales_RepeatCustomerRate -> tekrar alışveriş oranı
public class RepeatCustomerRateDto
{
    public int Buyers { get; set; }
    public int RepeatBuyers { get; set; }
    public decimal? RepeatRatePercent { get; set; }
}