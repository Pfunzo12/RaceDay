using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Controllers;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

[Area("Organiser")]
[RoleRequired("organiser")]
public abstract class OrganiserBaseController : AppController
{
    protected OrganiserBaseController(ApiClient api) : base(api) { }

    protected async Task<List<EventResponse>> LoadOwnEventsAsync()
    {
        var r = await Api.GetAsync<List<EventResponse>>("api/events?mine=true");
        if (!r.Ok) SetError(r.Error);
        return r.Data ?? new List<EventResponse>();
    }

    /// Shown by Categories / Enrolments / Results when no event has been chosen yet.
    protected async Task<IActionResult> PickEventAsync(string title, string intro, string controller)
    {
        var vm = new EventPickerViewModel { Title = title, Intro = intro, Controller = controller, Events = await LoadOwnEventsAsync() };
        return View("~/Areas/Organiser/Views/Shared/EventPicker.cshtml", vm);
    }
}
