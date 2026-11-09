using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class ResultsController : ParticipantBaseController
{
    public ResultsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var r = await Api.GetAsync<List<ResultResponse>>("api/results/mine");
        if (!r.Ok) SetError(r.Error);
        return View(r.Data ?? new List<ResultResponse>());
    }
}
