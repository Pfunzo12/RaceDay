using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// The ONLY way the MVC app touches data: HTTP calls to the Part 2 API. No database access, no business rules.
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private const string Unavailable = "We can't reach the RaceDay service right now. Please try again in a moment.";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    public ApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    private ISession Session => _ctx.HttpContext!.Session;

    // ---------- helpers ----------
    public static string WithQuery(string url, IDictionary<string, string?> query)
    {
        var clean = query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                         .ToDictionary(kv => kv.Key, kv => kv.Value);
        return clean.Count == 0 ? url : QueryHelpers.AddQueryString(url, clean);
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body != null)
            req.Content = JsonContent.Create(body, body.GetType(), null, JsonOpts);
        return await ExecuteAsync<T>(req);
    }

    public async Task<ApiResult<string>> UploadAsync(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return await ExecuteAsync<string>(req);
    }

    /// Fetches a stored image from the API (used by the /media proxy so browsers never need to reach the API directly).
    public async Task<(byte[] Data, string ContentType)?> GetFileAsync(string path)
    {
        try
        {
            using var resp = await _http.GetAsync(path.TrimStart('/'));
            if (!resp.IsSuccessStatusCode) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            return (bytes, resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }

    // ---------- login keeps the API session cookie in the MVC session ----------
    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest(email, password), options: JsonOpts)
            };
            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return Fail<LoginResponse>(401, "Wrong email or password.");
            if (!resp.IsSuccessStatusCode)
                return Fail<LoginResponse>((int)resp.StatusCode, FriendlyError(resp.StatusCode, body));

            var login = JsonSerializer.Deserialize<LoginResponse>(body, JsonOpts);
            var cookie = ExtractSessionCookie(resp);
            if (login == null || cookie == null)
                return Fail<LoginResponse>(500, "We couldn't sign you in. Please try again.");

            Session.Clear();
            Session.SetString(SessionKeys.ApiCookie, cookie);
            Session.SetInt32(SessionKeys.UserId, login.UserId);
            Session.SetString(SessionKeys.FullName, login.FullName);
            Session.SetString(SessionKeys.Role, login.Role);
            return new ApiResult<LoginResponse> { Ok = true, Status = 200, Data = login };
        }
        catch (HttpRequestException) { return Fail<LoginResponse>(0, Unavailable); }
        catch (TaskCanceledException) { return Fail<LoginResponse>(0, Unavailable); }
        catch (JsonException) { return Fail<LoginResponse>(500, "We couldn't sign you in. Please try again."); }
    }

    public async Task LogoutAsync()
    {
        try { await ExecuteAsync<string>(new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")); }
        catch (SessionExpiredException) { /* already logged out on the API side */ }
        finally { Session.Clear(); }
    }

    private static string? ExtractSessionCookie(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var values)) return null;
        foreach (var v in values)
            if (v.StartsWith(".AspNetCore.Session=", StringComparison.Ordinal))
                return v.Split(';')[0];
        return null;
    }

    // ---------- core ----------
    private async Task<ApiResult<T>> ExecuteAsync<T>(HttpRequestMessage req)
    {
        try
        {
            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear();
                throw new SessionExpiredException();
            }
            if (!resp.IsSuccessStatusCode)
                return Fail<T>((int)resp.StatusCode, FriendlyError(resp.StatusCode, body));

            T? data = default;
            if (typeof(T) == typeof(string)) data = (T)(object)body;
            else if (!string.IsNullOrWhiteSpace(body)) data = JsonSerializer.Deserialize<T>(body, JsonOpts);
            return new ApiResult<T> { Ok = true, Status = (int)resp.StatusCode, Data = data };
        }
        catch (HttpRequestException) { return Fail<T>(0, Unavailable); }
        catch (TaskCanceledException) { return Fail<T>(0, Unavailable); }
        catch (JsonException) { return Fail<T>(500, "We got an unexpected answer from the RaceDay service. Please try again."); }
    }

    private static ApiResult<T> Fail<T>(int status, string error) => new() { Ok = false, Status = status, Error = error };

    /// The API's own messages are plain, friendly sentences. Anything else (JSON, HTML, stack traces) is replaced.
    private static string FriendlyError(HttpStatusCode status, string body)
    {
        var text = body?.Trim() ?? "";
        var looksPlain = text.Length is > 0 and < 300 && !text.StartsWith('{') && !text.StartsWith('[') && !text.StartsWith('<');
        if (looksPlain) return text.Trim('"');
        return status switch
        {
            HttpStatusCode.BadRequest => "Please check the details you entered and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            _ => "Something went wrong on our side. Please try again."
        };
    }
}
