using RaceDay.Web.Filters;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.Filters.Add<SessionExpiredFilter>());
builder.Services.AddHttpContextAccessor();

// MVC session: remembers who is logged in, their role and the API's session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// All data comes from the Part 2 API. The URL is configurable (appsettings or the ApiBaseUrl environment variable).
var apiBase = (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080").TrimEnd('/') + "/";
builder.Services.AddTransient<ApiCookieHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBase);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // UseCookies = false: each user's API cookie is attached from their own MVC session, never shared.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
    .AddHttpMessageHandler<ApiCookieHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute("organiser", "Organiser/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Organiser" }, constraints: new { area = "Organiser" });
app.MapControllerRoute("participant", "Participant/{controller=Events}/{action=Index}/{id?}",
    defaults: new { area = "Participant" }, constraints: new { area = "Participant" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
