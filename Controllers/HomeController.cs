using Microsoft.AspNetCore.Mvc;

namespace YTDownloader.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
