using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class CategoriesController : OrganiserBaseController
{
    public CategoriesController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId == null)
            return await PickEventAsync("Categories", "Choose an event to manage its age and distance categories.", "Categories");

        var ev = await Api.GetAsync<EventResponse>($"api/events/{eventId}");
        if (!ev.Ok || ev.Data == null)
        {
            SetError(ev.Error);
            return RedirectToAction(nameof(Index), new { eventId = (int?)null });
        }
        var cats = await Api.GetAsync<List<CategoryResponse>>($"api/events/{eventId}/categories");
        if (!cats.Ok) SetError(cats.Error);

        ViewBag.Event = ev.Data;
        ViewBag.Categories = cats.Data ?? new List<CategoryResponse>();
        return View(new CategoryFormViewModel { EventId = eventId.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        var invalid = !ModelState.IsValid;
        if (!invalid)
        {
            var r = await Api.PostAsync<CategoryResponse>($"api/events/{vm.EventId}/categories", vm.ToRequest());
            if (r.Ok) { SetSuccess("Category added."); return RedirectToAction(nameof(Index), new { eventId = vm.EventId }); }
            ModelState.AddModelError("", r.Error ?? "We couldn't add the category.");
        }

        // redisplay the page with the errors
        var ev = await Api.GetAsync<EventResponse>($"api/events/{vm.EventId}");
        if (!ev.Ok || ev.Data == null) { SetError(ev.Error); return RedirectToAction(nameof(Index), new { eventId = (int?)null }); }
        var cats = await Api.GetAsync<List<CategoryResponse>>($"api/events/{vm.EventId}/categories");
        ViewBag.Event = ev.Data;
        ViewBag.Categories = cats.Data ?? new List<CategoryResponse>();
        return View("Index", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var cats = await Api.GetAsync<List<CategoryResponse>>($"api/events/{eventId}/categories");
        var c = cats.Data?.FirstOrDefault(x => x.Id == id);
        if (!cats.Ok || c == null)
        {
            SetError(cats.Error ?? "We couldn't find that category.");
            return RedirectToAction(nameof(Index), new { eventId });
        }
        return View(new CategoryFormViewModel { Id = c.Id, EventId = c.EventId, Name = c.Name, DistanceKm = c.DistanceKm, Price = c.Price, MaxParticipants = c.MaxParticipants, MinAge = c.MinAge, MaxAge = c.MaxAge });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);
        var r = await Api.PutAsync<CategoryResponse>($"api/categories/{id}", vm.ToRequest());
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Error ?? "We couldn't save the category.");
            return View(vm);
        }
        SetSuccess("Category updated.");
        return RedirectToAction(nameof(Index), new { eventId = vm.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var r = await Api.DeleteAsync<string>($"api/categories/{id}");
        if (r.Ok) SetSuccess("Category deleted."); else SetError(r.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
