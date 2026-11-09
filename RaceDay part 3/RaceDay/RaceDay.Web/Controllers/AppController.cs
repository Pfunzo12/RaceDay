using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected readonly ApiClient Api;
    protected AppController(ApiClient api) => Api = api;

    protected void SetError(string? message) => TempData["Error"] = message ?? "Something went wrong. Please try again.";
    protected void SetSuccess(string message) => TempData["Success"] = message;

    protected IActionResult RedirectToRoleHome() => HttpContext.Session.CurrentRole() switch
    {
        "organiser" => RedirectToAction("Index", "Dashboard", new { area = "Organiser" }),
        "participant" => RedirectToAction("Index", "Events", new { area = "Participant" }),
        _ => RedirectToAction("Index", "Home", new { area = "" })
    };
}
