using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    private static string? ValidateEvent(EventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description) ||
            string.IsNullOrWhiteSpace(dto.Location) || string.IsNullOrWhiteSpace(dto.EventType))
            return "Name, description, location and event type are required.";
        if (dto.Name.Trim().Length > 150 || dto.Location.Trim().Length > 150) return "Name and location can be at most 150 characters.";
        if (dto.EventType.Trim().Length > 20) return "Event type can be at most 20 characters.";
        if (dto.DistanceKm <= 0) return "Distance must be above 0.";
        return null;
    }

    // ---------- Events ----------
    /// Filters: search (name/description), type, location, from/to (dates), upcoming (today onwards), mine (organiser's own).
    [HttpGet]
    public async Task<IActionResult> GetAll(string? search = null, string? type = null, string? location = null,
        DateTime? from = null, DateTime? to = null, bool upcoming = false, bool mine = false)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);

        var q = _db.Events.AsQueryable();
        if (mine)
        {
            if (!IsOrganiser) return Forbidden("Only organisers have their own events.");
            var uid = CurrentUserId!.Value;
            q = q.Where(e => e.OrganiserId == uid);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e => e.Name.Contains(s) || (e.Description != null && e.Description.Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EventType == type);
        if (!string.IsNullOrWhiteSpace(location))
        {
            var l = location.Trim();
            q = q.Where(e => e.Location.Contains(l));
        }
        if (from.HasValue) { var f = from.Value.Date; q = q.Where(e => e.EventDate >= f); }
        if (to.HasValue) { var t = to.Value.Date; q = q.Where(e => e.EventDate <= t); }
        if (upcoming) { var today = DateTime.Today; q = q.Where(e => e.EventDate >= today); }

        var list = await q.OrderBy(e => e.EventDate)
            .Select(e => new EventResponse(e.Id, e.Name, e.Description ?? "", e.EventDate, e.Location,
                e.DistanceKm, e.EventType, e.BannerImagePath, e.OrganiserId))
            .ToListAsync();
        return Ok(list);
    }

    /// Distinct event types already stored (used to fill filter / form drop-downs).
    [HttpGet("types")]
    public async Task<IActionResult> Types()
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        var list = await _db.Events.Select(e => e.EventType).Distinct().OrderBy(t => t).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound("Event not found.");
        return Ok(EventResponse.From(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can create events.");
        var error = ValidateEvent(dto);
        if (error != null) return BadRequest(error);

        var ev = new RaceEvent
        {
            Name = dto.Name.Trim(), Description = dto.Description.Trim(), EventDate = dto.EventDate.Date,
            Location = dto.Location.Trim(), DistanceKm = dto.DistanceKm, EventType = dto.EventType.Trim(),
            OrganiserId = CurrentUserId!.Value
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return StatusCode(201, EventResponse.From(ev));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can edit events.");
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only edit your own events.");
        var error = ValidateEvent(dto);
        if (error != null) return BadRequest(error);

        ev.Name = dto.Name.Trim();
        ev.Description = dto.Description.Trim();
        ev.EventDate = dto.EventDate.Date;
        ev.Location = dto.Location.Trim();
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType.Trim();
        await _db.SaveChangesAsync();
        return Ok(EventResponse.From(ev));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can delete events.");
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only delete your own events.");
        await _db.DeleteEventCascadeAsync(ev);
        return Ok("Event deleted.");
    }

    // ---------- Categories of an event ----------
    [HttpGet("{eventId}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound("Event not found.");
        var list = await _db.Categories.Where(c => c.EventId == eventId)
            .Select(c => new CategoryResponse(c.Id, c.EventId, c.Name, c.DistanceKm, c.Price, c.MaxParticipants,
                _db.EventEnrollments.Count(en => en.CategoryId == c.Id && en.Status != EnrolmentStatus.Cancelled),
                c.MinAge, c.MaxAge))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("{eventId}/categories")]
    public async Task<IActionResult> AddCategory(int eventId, CategoryDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can create categories.");
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only add categories to your own events.");
        var error = CategoriesController.Validate(dto);
        if (error != null) return BadRequest(error);

        var c = new Category
        {
            EventId = eventId, Name = dto.Name.Trim(), DistanceKm = dto.DistanceKm, Price = dto.Price,
            MaxParticipants = dto.MaxParticipants, MinAge = dto.MinAge, MaxAge = dto.MaxAge
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return StatusCode(201, new CategoryResponse(c.Id, c.EventId, c.Name, c.DistanceKm, c.Price, c.MaxParticipants, 0, c.MinAge, c.MaxAge));
    }

    // ---------- Enrolments ----------
    [HttpPost("{eventId}/enrol")]
    public async Task<IActionResult> Enrol(int eventId, EnrolDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsParticipant) return Forbidden("Only participants can enter events.");

        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.EventId == eventId);
        if (category == null) return NotFound("That category does not exist for this event.");
        if (category.Event!.EventDate.Date < DateTime.Today) return BadRequest("This event has already taken place.");

        var uid = CurrentUserId!.Value;
        var existing = await _db.EventEnrollments
            .FirstOrDefaultAsync(e => e.UserId == uid && e.CategoryId == dto.CategoryId);
        if (existing != null && existing.Status != EnrolmentStatus.Cancelled)
            return BadRequest("You have already entered this category.");

        var taken = await _db.EventEnrollments
            .CountAsync(e => e.CategoryId == dto.CategoryId && e.Status != EnrolmentStatus.Cancelled);
        if (taken >= category.MaxParticipants) return BadRequest("Sorry, this category is full.");

        if (existing != null)
        {
            existing.Status = EnrolmentStatus.Pending;   // re-enrol after a cancellation
            existing.EnrolledOn = DateTime.UtcNow;
        }
        else
        {
            _db.EventEnrollments.Add(new EventEnrollment { UserId = uid, CategoryId = dto.CategoryId });
        }
        await _db.SaveChangesAsync();
        return Ok("You are entered. Your enrolment is pending until the organiser confirms it.");
    }

    [HttpGet("{eventId}/enrolments")]
    public async Task<IActionResult> GetEnrolments(int eventId)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can view enrolments.");
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var list = await _db.EventEnrollments
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.EnrolledOn)
            .Select(e => new EventEnrolmentResponse(e.Id, e.UserId, e.User!.FullName, e.User.Email,
                e.CategoryId, e.Category!.Name, e.Status, e.EnrolledOn))
            .ToListAsync();
        return Ok(list);
    }
}
