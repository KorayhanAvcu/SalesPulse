using Microsoft.AspNetCore.Mvc;
using SalesPulse.Business.Services.Sales;

namespace SalesPulse.Web.Controllers;

public class SalesController : Controller
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    // GET /Sales -> Sales Intelligence sayfası
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        var model = await _salesService.GetSalesIntelligenceAsync(startDate, endDate);
        return View(model);
    }

    // GET /Sales/Data?startDate=2026-01-01&endDate=2026-10-31 -> JSON
    [HttpGet]
    public async Task<IActionResult> Data(DateTime? startDate, DateTime? endDate)
    {
        var model = await _salesService.GetSalesIntelligenceAsync(startDate, endDate);
        return Json(model);
    }
}
