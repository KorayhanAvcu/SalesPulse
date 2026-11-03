namespace SalesPulse.Entity.Dtos;

// Order Explorer filtrelerindeki açılır listelerin içeriği
public class OrderLookupsDto
{
    public List<LookupItemDto> Cities { get; set; } = new();
    public List<LookupItemDto> Statuses { get; set; } = new();
}
