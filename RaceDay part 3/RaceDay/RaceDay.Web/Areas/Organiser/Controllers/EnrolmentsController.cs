using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EnrolmentsController : OrganiserBaseController
{
    public EnrolmentsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null)
            return await PickEventAsync("Enrolments", "Choose an event to see who has entered and confirm their place.", "Enrolments");

        var ev = await Api.GetAsync<EventResponse>($"api/events/{eventId}");
        if (!ev.Ok || ev.Data == null)
        {
            SetError(ev.Error);
            return RedirectToAction(nameof(Index), new { eventId = (int?)null });
        }
        var list = await Api.GetAsync<List<EventEnrolmentResponse>>($"api/events/{eventId}/enrolments");
        if (!list.Ok) SetError(list.Error);

        ViewBag.Event = ev.Data;
        return View(list.Data ?? new List<EventEnrolmentResponse>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, int eventId, string status)
    {
        var r = await Api.PutAsync<string>($"api/enrolments/{id}/status", new EnrolmentStatusRequest(status));
        if (r.Ok) SetSuccess(r.Data ?? "Status updated."); else SetError(r.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
