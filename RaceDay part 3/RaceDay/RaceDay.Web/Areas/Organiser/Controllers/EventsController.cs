using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EventsController : OrganiserBaseController
{
    public EventsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index() => View(await LoadOwnEventsAsync());

    /// Existing event types from the database first, then the default choices.
    private async Task<IActionResult> FormViewAsync(EventFormViewModel vm)
    {
        var existing = await Api.GetAsync<List<string>>("api/events/types");
        var types = (existing.Data ?? new List<string>()).Concat(EventTypes.All).Distinct().ToList();
        if (!string.IsNullOrEmpty(vm.EventType) && !types.Contains(vm.EventType)) types.Add(vm.EventType);
        ViewBag.Types = types;
        return View("Form", vm);
    }

    [HttpGet]
    public Task<IActionResult> Create() =>
        FormViewAsync(new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel vm)
    {
        if (!ModelState.IsValid) return await FormViewAsync(vm);

        var r = await Api.PostAsync<EventResponse>("api/events", vm.ToRequest());
        if (!r.Ok || r.Data == null)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't create the event.");
            return await FormViewAsync(vm);
        }

        await UploadBannerIfAnyAsync(r.Data.Id, vm.BannerFile, "Event created");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await Api.GetAsync<EventResponse>($"api/events/{id}");
        if (!r.Ok || r.Data == null)
        {
            SetError(r.Error);
            return RedirectToAction(nameof(Index));
        }
        var e = r.Data;
        return await FormViewAsync(new EventFormViewModel
        {
            Id = e.Id, Name = e.Name, Description = e.Description, EventDate = e.EventDate, Location = e.Location,
            DistanceKm = e.DistanceKm, EventType = e.EventType, BannerImagePath = e.BannerImagePath
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return await FormViewAsync(vm);

        var r = await Api.PutAsync<EventResponse>($"api/events/{id}", vm.ToRequest());
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't save the event.");
            return await FormViewAsync(vm);
        }

        await UploadBannerIfAnyAsync(id, vm.BannerFile, "Event updated");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await Api.GetAsync<EventResponse>($"api/events/{id}");
        if (!r.Ok || r.Data == null)
        {
            SetError(r.Error);
            return RedirectToAction(nameof(Index));
        }
        return View(r.Data);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var r = await Api.DeleteAsync<string>($"api/events/{id}");
        if (r.Ok) SetSuccess("Event deleted.");
        else SetError(r.Error);
        return RedirectToAction(nameof(Index));
    }

    private async Task UploadBannerIfAnyAsync(int eventId, IFormFile? file, string successMessage)
    {
        if (file is { Length: > 0 })
        {
            var up = await Api.UploadAsync($"api/events/{eventId}/banner", file);
            if (!up.Ok)
            {
                SetError($"{successMessage}, but the banner was not saved: {up.Error}");
                return;
            }
        }
        SetSuccess($"{successMessage}.");
    }
}
