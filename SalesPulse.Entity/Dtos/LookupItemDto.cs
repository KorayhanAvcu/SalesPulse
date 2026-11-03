namespace SalesPulse.Entity.Dtos;

// Açılır listeler (şehir, durum) için basit "id + ad" çifti
public class LookupItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
