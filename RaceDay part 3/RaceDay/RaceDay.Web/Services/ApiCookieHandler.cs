namespace RaceDay.Web.Services;

/// Adds the logged-in user's API session cookie (kept in the MVC session) to every outgoing API request.
public class ApiCookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public ApiCookieHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookie = _ctx.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return base.SendAsync(request, cancellationToken);
    }
}
