using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : SessionControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(RaceDayDbContext db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Full name, email and password are required.");
        var role = (dto.Role ?? "").Trim().ToLowerInvariant();
        if (role != Roles.Organiser && role != Roles.Participant)
            return BadRequest("Role must be organiser or participant.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest("That email is already registered.");

        var user = new User { FullName = dto.FullName.Trim(), Email = email, Role = role };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok("Registered.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized("Wrong email or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password ?? "");
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Wrong email or password.");

        var role = user.Role.Trim().ToLowerInvariant();
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", role);
        return Ok(new LoginResponse(user.Id, user.FullName, role));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("Logged out.");
    }
}
