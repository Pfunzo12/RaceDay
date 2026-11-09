using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    public static string? Validate(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Category name is required.";
        if (dto.Name.Trim().Length > 50) return "Category name can be at most 50 characters.";
        if (dto.DistanceKm <= 0) return "Distance must be above 0.";
        if (dto.Price < 0) return "Price cannot be negative.";
        if (dto.MaxParticipants < 1) return "Maximum participants must be at least 1.";
        if (dto.MinAge is < 0 || dto.MaxAge is < 0) return "Ages cannot be negative.";
        if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MinAge > dto.MaxAge)
            return "Minimum age cannot be above maximum age.";
        return null;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can edit categories.");
        var c = await _db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound("Category not found.");
        if (c.Event!.OrganiserId != CurrentUserId) return Forbidden("You can only edit categories of your own events.");
        var error = Validate(dto);
        if (error != null) return BadRequest(error);

        c.Name = dto.Name.Trim();
        c.DistanceKm = dto.DistanceKm;
        c.Price = dto.Price;
        c.MaxParticipants = dto.MaxParticipants;
        c.MinAge = dto.MinAge;
        c.MaxAge = dto.MaxAge;
        await _db.SaveChangesAsync();
        var taken = await _db.EventEnrollments.CountAsync(e => e.CategoryId == c.Id && e.Status != EnrolmentStatus.Cancelled);
        return Ok(new CategoryResponse(c.Id, c.EventId, c.Name, c.DistanceKm, c.Price, c.MaxParticipants, taken, c.MinAge, c.MaxAge));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can delete categories.");
        var c = await _db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound("Category not found.");
        if (c.Event!.OrganiserId != CurrentUserId) return Forbidden("You can only delete categories of your own events.");
        await _db.DeleteCategoryCascadeAsync(c);
        return Ok("Category deleted.");
    }
}
