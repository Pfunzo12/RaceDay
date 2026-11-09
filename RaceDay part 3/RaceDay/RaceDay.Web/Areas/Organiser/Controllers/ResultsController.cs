using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class ResultsController : OrganiserBaseController
{
    public ResultsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null)
            return await PickEventAsync("Results", "Choose an event that has finished to capture and publish results.", "Results");

        var ev = await Api.GetAsync<EventResponse>($"api/events/{eventId}");
        if (!ev.Ok || ev.Data == null)
        {
            SetError(ev.Error);
            return RedirectToAction(nameof(Index), new { eventId = (int?)null });
        }
        var rows = await Api.GetAsync<List<ResultEntryRow>>($"api/results/event/{eventId}");
        if (!rows.Ok) SetError(rows.Error);

        ViewBag.Event = ev.Data;
        ViewBag.Concluded = ev.Data.EventDate.Date <= DateTime.Today;
        return View(rows.Data ?? new List<ResultEntryRow>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResultFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            SetError(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Index), new { eventId = vm.EventId });
        }

        ApiResult<string> r = vm.ResultId.HasValue
            ? await Api.PutAsync<string>($"api/results/{vm.ResultId}", new UpdateResultRequest(vm.FinishTime!.Value, vm.Position!.Value))
            : await Api.PostAsync<string>("api/results", new CaptureResultRequest(vm.EnrollmentId, vm.FinishTime!.Value, vm.Position!.Value));

        if (r.Ok) SetSuccess(r.Data ?? "Result saved."); else SetError(r.Error);
        return RedirectToAction(nameof(Index), new { eventId = vm.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int eventId)
    {
        var r = await Api.PostAsync<string>($"api/results/event/{eventId}/publish");
        if (r.Ok) SetSuccess(r.Data ?? "Results published."); else SetError(r.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
