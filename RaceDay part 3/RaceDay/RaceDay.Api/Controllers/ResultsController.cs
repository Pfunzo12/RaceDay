using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private static string? ValidateValues(TimeSpan finish, int position)
    {
        if (finish <= TimeSpan.Zero) return "Finish time must be above 0.";
        if (position <= 0) return "Finishing position must be 1 or higher.";
        return null;
    }

    /// Organiser: list confirmed entrants of an event with their result (if captured).
    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can view result entry.");
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only manage results for your own events.");

        var enrolments = await _db.EventEnrollments
            .Include(e => e.User).Include(e => e.Category)
            .Where(e => e.Category!.EventId == eventId && e.Status == EnrolmentStatus.Confirmed)
            .OrderBy(e => e.Category!.Name).ThenBy(e => e.User!.FullName)
            .ToListAsync();
        var results = await _db.Results
            .Where(r => r.Enrollment!.Category!.EventId == eventId)
            .ToDictionaryAsync(r => r.EnrollmentId);

        var rows = enrolments.Select(e =>
        {
            results.TryGetValue(e.Id, out var r);
            return new ResultEntryRow(e.Id, e.User!.FullName, e.Category!.Name,
                r?.Id, r?.FinishTime, r?.Position, r?.IsPublished ?? false);
        }).ToList();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Capture(CaptureResultDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can capture results.");

        var enrol = await _db.EventEnrollments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId);
        if (enrol == null) return NotFound("Enrolment not found.");
        var ev = enrol.Category!.Event!;
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only capture results for your own events.");
        if (!IsConcluded(ev.EventDate)) return BadRequest("Results can only be captured after the event has concluded.");
        if (enrol.Status != EnrolmentStatus.Confirmed) return BadRequest("Only confirmed participants can have results.");
        var error = ValidateValues(dto.FinishTime, dto.Position);
        if (error != null) return BadRequest(error);
        if (await _db.Results.AnyAsync(r => r.EnrollmentId == dto.EnrollmentId))
            return BadRequest("A result already exists for this enrolment.");

        _db.Results.Add(new RaceResult { EnrollmentId = dto.EnrollmentId, FinishTime = dto.FinishTime, Position = dto.Position });
        await _db.SaveChangesAsync();
        return StatusCode(201, "Result saved.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateResultDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can edit results.");
        var r = await _db.Results.Include(x => x.Enrollment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound("Result not found.");
        if (r.Enrollment!.Category!.Event!.OrganiserId != CurrentUserId)
            return Forbidden("You can only edit results for your own events.");
        var error = ValidateValues(dto.FinishTime, dto.Position);
        if (error != null) return BadRequest(error);

        r.FinishTime = dto.FinishTime;
        r.Position = dto.Position;
        await _db.SaveChangesAsync();
        return Ok("Result updated.");
    }

    /// Organiser: publish every captured result of an event so participants can see them.
    [HttpPost("event/{eventId}/publish")]
    public async Task<IActionResult> Publish(int eventId)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can publish results.");
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only publish results for your own events.");
        if (!IsConcluded(ev.EventDate)) return BadRequest("Results can only be published after the event has concluded.");

        var pending = await _db.Results
            .Where(r => r.Enrollment!.Category!.EventId == eventId && !r.IsPublished).ToListAsync();
        if (pending.Count == 0) return BadRequest("There are no unpublished results for this event.");
        foreach (var r in pending) r.IsPublished = true;
        await _db.SaveChangesAsync();
        return Ok(pending.Count + " result(s) published.");
    }

    /// Participant: own published results, with the number of published finishers in the same event.
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsParticipant) return Forbidden("Only participants have results.");
        var uid = CurrentUserId!.Value;

        var rows = await _db.Results
            .Where(r => r.IsPublished && r.FinishTime != null && r.Position != null && r.Enrollment!.UserId == uid)
            .Select(r => new
            {
                r.Id, r.EnrollmentId,
                EventId = r.Enrollment!.Category!.EventId,
                EventName = r.Enrollment.Category.Event!.Name,
                EventDate = r.Enrollment.Category.Event.EventDate,
                CategoryName = r.Enrollment.Category.Name,
                FinishTime = r.FinishTime!.Value,
                Position = r.Position!.Value
            }).ToListAsync();

        var list = new List<ResultResponse>();
        foreach (var r in rows)
        {
            var total = await _db.Results.CountAsync(x => x.IsPublished && x.Enrollment!.Category!.EventId == r.EventId);
            list.Add(new ResultResponse(r.Id, r.EnrollmentId, r.EventName, r.EventDate, r.CategoryName,
                r.FinishTime, r.Position, total));
        }
        return Ok(list.OrderByDescending(x => x.EventDate).ToList());
    }
}
