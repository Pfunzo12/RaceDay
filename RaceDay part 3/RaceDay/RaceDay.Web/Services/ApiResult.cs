namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Ok { get; init; }
    public int Status { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
}

/// Thrown when the API says "not logged in" (e.g. the session timed out). A global filter redirects to the login page.
public class SessionExpiredException : Exception { }
