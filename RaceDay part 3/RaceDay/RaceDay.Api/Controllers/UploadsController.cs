using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class UploadsController : SessionControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

    private readonly RaceDayDbContext _db;
    private readonly IWebHostEnvironment _env;
    public UploadsController(RaceDayDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    private static string? ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose an image file.";
        if (file.Length > MaxBytes) return "The image must be 2 MB or smaller.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext) || !file.ContentType.StartsWith("image/"))
            return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }

    private string WebRoot => Path.Combine(_env.ContentRootPath, "wwwroot");

    private async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var dir = Path.Combine(WebRoot, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var fs = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(fs);
        return $"/uploads/{folder}/{name}";
    }

    private void TryDelete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || !relativePath.StartsWith("/uploads/") || relativePath.Contains("..")) return;
        try
        {
            var full = Path.Combine(WebRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch { /* old file cleanup is best effort */ }
    }

    [HttpPost("events/{id}/banner")]
    public async Task<IActionResult> Banner(int id, IFormFile file)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsOrganiser) return Forbidden("Only organisers can upload event banners.");
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only change your own events.");
        var error = ValidateImage(file);
        if (error != null) return BadRequest(error);

        TryDelete(ev.BannerImagePath);
        ev.BannerImagePath = await SaveAsync(file, "banners");
        await _db.SaveChangesAsync();
        return Ok(ev.BannerImagePath);
    }

    [HttpPost("profile/picture")]
    public async Task<IActionResult> ProfilePicture(IFormFile file)
    {
        if (!IsLoggedIn) return Unauthorized(NotLoggedIn);
        if (!IsParticipant) return Forbidden("Only participants have a profile picture.");
        var error = ValidateImage(file);
        if (error != null) return BadRequest(error);

        var user = await _db.Users.FindAsync(CurrentUserId!.Value);
        if (user == null) return NotFound("User not found.");
        TryDelete(user.ProfilePicturePath);
        user.ProfilePicturePath = await SaveAsync(file, "profiles");
        await _db.SaveChangesAsync();
        return Ok(user.ProfilePicturePath);
    }
}
