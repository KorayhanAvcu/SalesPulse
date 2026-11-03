namespace SalesPulse.Entity.Dtos
{
    public class OrderSearchFilter
    {
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public byte? CityId { get; set; }
        public byte? StatusId { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string SortColumn { get; set; } = "OrderDate";
        public string SortDir { get; set; } = "DESC";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
