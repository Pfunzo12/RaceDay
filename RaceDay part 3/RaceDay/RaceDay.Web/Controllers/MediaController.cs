using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// Streams banner and profile images from the API, so the browser only ever talks to the MVC app.
public class MediaController : Controller
{
    private readonly ApiClient _api;
    public MediaController(ApiClient api) => _api = api;

    [HttpGet("/media")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Image(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/uploads/") || path.Contains(".."))
            return NotFound();
        var file = await _api.GetFileAsync(path);
        if (file == null) return NotFound();
        return File(file.Value.Data, file.Value.ContentType);
    }
}
