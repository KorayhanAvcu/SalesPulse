namespace SalesPulse.Entity.Dtos;

// sp_Dashboard_TopCategories -> ciroya göre ilk 10 kategori
public class TopCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}