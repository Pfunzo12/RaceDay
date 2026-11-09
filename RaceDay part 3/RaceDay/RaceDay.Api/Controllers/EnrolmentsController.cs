using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsParticipant) return Forbidden("Only participants have enrolments.");
        var uid = CurrentUserId!.Value;   // always from the session, never from the request

        var list = await _db.EventEnrollments
            .Where(e => e.UserId == uid)
            .OrderBy(e => e.Category!.Event!.EventDate)
            .Select(e => new EnrolmentResponse(e.Id, e.Category!.EventId, e.Category.Event!.Name,
                e.Category.Event.EventDate, e.Category.Event.Location,
                e.CategoryId, e.Category.Name, e.Status, e.EnrolledOn))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetStatus(int id, EnrolmentStatusDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can change enrolment status.");
        if (!EnrolmentStatus.All.Contains(dto.Status)) return BadRequest("Status must be Pending, Confirmed or Cancelled.");

        var e = await _db.EventEnrollments.Include(x => x.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound("Enrolment not found.");
        if (e.Category!.Event!.OrganiserId != CurrentUserId)
            return Forbidden("You can only manage enrolments for your own events.");

        e.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok("Enrolment marked as " + dto.Status + ".");
    }
}
