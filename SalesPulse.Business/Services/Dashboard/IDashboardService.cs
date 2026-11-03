using SalesPulse.Entity.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesPulse.Business.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(DateTime? startDate, DateTime? endDate);
    }
}
