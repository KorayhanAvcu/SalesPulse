namespace SalesPulse.Entity.Dtos;

// sp_Sales_CategoryMonthly -> ana kategori grubunun bir aydaki performansı
public class CategoryMonthlyDto
{
    public DateTime Month { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}