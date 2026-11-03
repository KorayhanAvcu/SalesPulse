namespace SalesPulse.Entity.Dtos;

// sp_Sales_PeriodComparison -> A (önceki) ve B (karşılaştırılan) dönem
// Property adları SP'deki kolon adlarıyla birebir aynı olmalı (A_OrderCount gibi)
public class PeriodComparisonDto
{
    public int A_OrderCount { get; set; }
    public int B_OrderCount { get; set; }
    public decimal? OrderChangePercent { get; set; }
    public decimal A_Revenue { get; set; }
    public decimal B_Revenue { get; set; }
    public decimal? RevenueChangePercent { get; set; }
    public int A_CustomerCount { get; set; }
    public int B_CustomerCount { get; set; }
    public decimal? A_AverageBasket { get; set; }
    public decimal? B_AverageBasket { get; set; }
}