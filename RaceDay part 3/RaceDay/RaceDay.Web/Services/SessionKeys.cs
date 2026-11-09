namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string ApiCookie = "ApiCookie";
    public const string UserId = "UserId";
    public const string FullName = "FullName";
    public const string Role = "Role";
}

public static class SessionExtensions
{
    public static bool IsAuthenticated(this ISession s) => !string.IsNullOrEmpty(s.GetString(SessionKeys.ApiCookie));
    public static string? CurrentRole(this ISession s) => s.GetString(SessionKeys.Role);
    public static string? CurrentName(this ISession s) => s.GetString(SessionKeys.FullName);
}
