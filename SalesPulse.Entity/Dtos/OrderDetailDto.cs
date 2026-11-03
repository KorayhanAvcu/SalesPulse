namespace SalesPulse.Entity.Dtos;

// sp_Orders_GetDetail 1. sonuç kümesi -> sipariş başlığı (detay modalı)
public class OrderDetailDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public byte? InstallmentCount { get; set; }
    public DateTime? PaymentDate { get; set; }

    // 2. sonuç kümesi buraya eklenir
    public List<OrderLineDto> Lines { get; set; } = new();
}