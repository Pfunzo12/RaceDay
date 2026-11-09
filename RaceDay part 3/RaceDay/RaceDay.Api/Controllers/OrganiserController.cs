using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/organiser")]
public class OrganiserController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public OrganiserController(RaceDayDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers have a dashboard.");
        var uid = CurrentUserId!.Value;
        var today = DateTime.Today;

        var events = await _db.Events.Where(e => e.OrganiserId == uid).OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.Id, e.Name, e.EventDate, e.Location,
                Total = _db.EventEnrollments.Count(en => en.Category!.EventId == e.Id && en.Status != EnrolmentStatus.Cancelled),
                Confirmed = _db.EventEnrollments.Count(en => en.Category!.EventId == e.Id && en.Status == EnrolmentStatus.Confirmed)
            }).ToListAsync();

        var rows = events.Select(e => new DashboardEventRow(e.Id, e.Name, e.EventDate, e.Location,
            e.Total, e.Confirmed, e.EventDate >= today)).ToList();
        return Ok(new OrganiserDashboardResponse(rows.Count, rows.Sum(r => r.EnrolmentCount),
            rows.Count(r => r.IsUpcoming), rows));
    }
}
