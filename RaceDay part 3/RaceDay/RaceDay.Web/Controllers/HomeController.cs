using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public HomeController(ApiClient api) : base(api) { }

    public IActionResult Index()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        return View();
    }

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
