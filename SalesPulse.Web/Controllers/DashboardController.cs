using Microsoft.AspNetCore.Mvc;
using SalesPulse.Business.Services.Dashboard;

namespace SalesPulse.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // GET /Dashboard -> sayfanın kendisi (ilk açılışta son 12 ay)
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        var model = await _dashboardService.GetDashboardAsync(startDate, endDate);
        return View(model);
    }

    // GET /Dashboard/Data?startDate=2026-01-01&endDate=2026-10-31 -> tarih filtresi değişince JSON
    [HttpGet]
    public async Task<IActionResult> Data(DateTime? startDate, DateTime? endDate)
    {
        var model = await _dashboardService.GetDashboardAsync(startDate, endDate);
        return Json(model);
    }
}
