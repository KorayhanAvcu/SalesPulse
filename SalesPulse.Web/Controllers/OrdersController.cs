using Microsoft.AspNetCore.Mvc;
using SalesPulse.Business.Services.Order;
using SalesPulse.Entity.Dtos;

namespace SalesPulse.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET /Orders -> Order Explorer sayfası (açılır listeler için şehir/durum listesini gönderir)
    public async Task<IActionResult> Index()
    {
        var lookups = await _orderService.GetLookupsAsync();
        return View(lookups);
    }

    // GET /Orders/List?cityId=34&statusId=4&page=2 -> tablo verisi (JSON)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OrderSearchFilter filter)
    {
        var result = await _orderService.SearchAsync(filter);
        return Json(result);
    }

    // GET /Orders/Detail/1500000 -> detay modalı (JSON)
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await _orderService.GetDetailAsync(id);
        if (detail is null)
            return NotFound();

        return Json(detail);
    }

    // GET /Orders/Export?cityId=34 -> CSV dosyası indir
    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] OrderSearchFilter filter)
    {
        var bytes = await _orderService.ExportCsvAsync(filter);
        var fileName = $"siparisler_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
