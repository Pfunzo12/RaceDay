using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    public ProfileController(RaceDayDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound("User not found.");
        return Ok(new ProfileResponse(user.Id, user.FullName, user.Email, user.Role.ToLowerInvariant(), user.Phone,
            user.DateOfBirth, user.EmergencyContact, user.ClubName, user.ProfilePicturePath));
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdateDto dto)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (string.IsNullOrWhiteSpace(dto.FullName)) return BadRequest("Full name is required.");
        if (dto.FullName.Trim().Length > 100) return BadRequest("Full name is too long.");
        if ((dto.Phone ?? "").Length > 20) return BadRequest("Phone number is too long.");
        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound("User not found.");

        user.FullName = dto.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        user.DateOfBirth = dto.DateOfBirth;
        user.EmergencyContact = dto.EmergencyContact;
        user.ClubName = dto.ClubName;
        await _db.SaveChangesAsync();
        return Ok("Profile updated.");
    }
}
