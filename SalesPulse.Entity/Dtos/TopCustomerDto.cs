using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.Entity.Dtos
{
    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageBasket { get; set; }
        public DateTime LastOrderDate { get; set; }
    }
}
