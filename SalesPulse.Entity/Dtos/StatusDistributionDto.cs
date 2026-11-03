namespace SalesPulse.Entity.Dtos;

public class StatusDistributionDto
{
    public byte StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}