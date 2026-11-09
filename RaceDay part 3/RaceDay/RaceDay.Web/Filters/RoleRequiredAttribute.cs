using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// Blocks a controller unless the MVC session says the user is logged in with the required role.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RoleRequiredAttribute : ActionFilterAttribute
{
    private readonly string _role;
    public RoleRequiredAttribute(string role) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsAuthenticated())
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
            return;
        }
        if (session.CurrentRole() != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", new { area = "" });
        }
    }
}

/// If the API reports the session is over, send the user to the login page instead of showing an error.
public class SessionExpiredFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is SessionExpiredException)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", expired = true });
            context.ExceptionHandled = true;
        }
    }
}
