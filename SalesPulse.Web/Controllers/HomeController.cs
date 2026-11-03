using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SalesPulse.Web.Models;

namespace SalesPulse.Web.Controllers;

public class HomeController : Controller
{
    // Eski /Home adresine gelen olursa Dashboard'a yönlendir
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    // Canlı ortamda beklenmeyen hata olursa gösterilecek sayfa (Program.cs: UseExceptionHandler)
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
