using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EnrolmentsController : ParticipantBaseController
{
    public EnrolmentsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var r = await Api.GetAsync<List<EnrolmentResponse>>("api/enrolments/mine");
        if (!r.Ok) SetError(r.Error);
        return View(r.Data ?? new List<EnrolmentResponse>());
    }
}
