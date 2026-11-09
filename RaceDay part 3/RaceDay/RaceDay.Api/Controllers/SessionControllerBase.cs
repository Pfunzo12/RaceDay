using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public abstract class SessionControllerBase : ControllerBase
{
    protected const string NotLoggedIn = "Please log in first.";

    protected int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
    protected string? CurrentRole => HttpContext.Session.GetString("Role");
    protected bool IsLoggedIn => CurrentUserId != null;
    protected bool IsOrganiser => string.Equals(CurrentRole, Roles.Organiser, StringComparison.OrdinalIgnoreCase);
    protected bool IsParticipant => string.Equals(CurrentRole, Roles.Participant, StringComparison.OrdinalIgnoreCase);

    protected IActionResult Forbidden(string message) => StatusCode(403, message);

    /// Events are stored as a date only; an event is "concluded" once its day has arrived.
    protected static bool IsConcluded(DateTime eventDate) => eventDate.Date <= DateTime.Today;
}
