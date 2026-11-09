using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class DashboardController : OrganiserBaseController
{
    public DashboardController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var r = await Api.GetAsync<OrganiserDashboardResponse>("api/organiser/dashboard");
        if (!r.Ok) SetError(r.Error);
        return View(r.Data ?? new OrganiserDashboardResponse(0, 0, 0, new List<DashboardEventRow>()));
    }
}
