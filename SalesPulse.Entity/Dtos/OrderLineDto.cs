namespace SalesPulse.Entity.Dtos;

// sp_Orders_GetDetail 2. sonuç kümesi -> siparişin kalemleri
public class OrderLineDto
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}