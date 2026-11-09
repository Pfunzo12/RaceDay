using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EventsController : ParticipantBaseController
{
    public EventsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(EventFilterViewModel filter)
    {
        var url = ApiClient.WithQuery("api/events", new Dictionary<string, string?>
        {
            ["search"] = filter.Search,
            ["type"] = filter.Type,
            ["location"] = filter.Location,
            ["from"] = filter.From?.ToString("yyyy-MM-dd"),
            ["to"] = filter.To?.ToString("yyyy-MM-dd"),
            ["upcoming"] = filter.IncludePast ? null : "true"
        });
        var r = await Api.GetAsync<List<EventResponse>>(url);
        if (!r.Ok) SetError(r.Error);

        var types = await Api.GetAsync<List<string>>("api/events/types");
        ViewBag.Types = (types.Ok && types.Data is { Count: > 0 })
            ? types.Data
            : EventTypes.All.ToList();
        ViewBag.Filter = filter;
        return View(r.Data ?? new List<EventResponse>());
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetAsync<EventResponse>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null)
        {
            SetError(ev.Error);
            return RedirectToAction(nameof(Index));
        }
        var cats = await Api.GetAsync<List<CategoryResponse>>($"api/events/{id}/categories");
        if (!cats.Ok) SetError(cats.Error);

        ViewBag.Categories = cats.Data ?? new List<CategoryResponse>();
        return View(ev.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int? categoryId)
    {
        if (categoryId == null)
        {
            SetError("Please choose a category before you enrol.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var r = await Api.PostAsync<string>($"api/events/{id}/enrol", new EnrolRequest(categoryId.Value));
        if (!r.Ok)
        {
            SetError(r.Error);
            return RedirectToAction(nameof(Details), new { id });
        }
        SetSuccess(r.Data ?? "You are entered.");
        return RedirectToAction("Index", "Enrolments");
    }
}
