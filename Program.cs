using System.Text.Json;
using StarLink;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.Name = "starlink.admin"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict; options.SlidingExpiration = true; options.ExpireTimeSpan = TimeSpan.FromHours(4); });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<PlatformStore>();
var app = builder.Build();
var fileTypes = new FileExtensionContentTypeProvider();
fileTypes.Mappings[".geojson"] = "application/geo+json";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = fileTypes });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/api/data", async (PlatformStore store) => Results.Json(await store.ReadAsync()));
app.MapPost("/api/profile", async (ProfileInput input, PlatformStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Region) || string.IsNullOrWhiteSpace(input.School))
        return Results.BadRequest(new { message = "Name, school, and region are required." });
    var data = await store.ReadAsync();
    var profile = data.Profiles.FirstOrDefault(p => p.Id == "you");
    if (profile is null) return Results.NotFound();
    profile.Name = input.Name.Trim(); profile.School = input.School.Trim(); profile.Region = input.Region.Trim(); profile.Subject = input.Subject.Trim(); profile.Bio = input.Bio.Trim();
    await store.WriteAsync(data); return Results.Json(profile);
});
app.MapPost("/api/resources", async (ResourceInput input, PlatformStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Subject) || string.IsNullOrWhiteSpace(input.Region))
        return Results.BadRequest(new { message = "Title, subject, and region are required." });
    var data = await store.ReadAsync();
    var item = new Resource { Id = Guid.NewGuid().ToString("N"), Title = input.Title.Trim(), Subject = input.Subject.Trim(), Region = input.Region.Trim(), Type = string.IsNullOrWhiteSpace(input.Type) ? "Teaching resource" : input.Type.Trim(), Author = data.Profiles.First(p => p.Id == "you").Name, Summary = input.Summary.Trim(), CreatedAt = DateTimeOffset.UtcNow };
    data.Resources.Insert(0, item); await store.WriteAsync(data); return Results.Json(item, statusCode: 201);
});
app.MapPost("/api/topics", async (TopicInput input, PlatformStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Body) || string.IsNullOrWhiteSpace(input.Region))
        return Results.BadRequest(new { message = "Title, message, and region are required." });
    var data = await store.ReadAsync();
    var topic = new Topic { Id = Guid.NewGuid().ToString("N"), Title = input.Title.Trim(), Body = input.Body.Trim(), Region = input.Region.Trim(), Author = data.Profiles.First(p => p.Id == "you").Name, CreatedAt = DateTimeOffset.UtcNow };
    data.Topics.Insert(0, topic); await store.WriteAsync(data); return Results.Json(topic, statusCode: 201);
});
app.MapPost("/api/topics/{id}/replies", async (string id, ReplyInput input, PlatformStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Body)) return Results.BadRequest(new { message = "Write a reply first." });
    var data = await store.ReadAsync(); var topic = data.Topics.FirstOrDefault(t => t.Id == id);
    if (topic is null) return Results.NotFound();
    topic.Replies.Add(new Reply { Id = Guid.NewGuid().ToString("N"), Author = data.Profiles.First(p => p.Id == "you").Name, Body = input.Body.Trim(), CreatedAt = DateTimeOffset.UtcNow });
    await store.WriteAsync(data); return Results.Json(topic);
});
var adminPasscode = builder.Configuration["Admin:Passcode"] ?? Environment.GetEnvironmentVariable("STARLINK_ADMIN_PASSCODE") ?? "demo-admin";
app.MapPost("/api/admin/login", async (AdminLoginInput input, HttpContext context) =>
{
    var submitted = Encoding.UTF8.GetBytes(input.Passcode ?? ""); var expected = Encoding.UTF8.GetBytes(adminPasscode);
    if (submitted.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(submitted, expected)) return Results.Unauthorized();
    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "STAR-LINK Demo Admin"), new Claim(ClaimTypes.Role, "Admin")], CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { authenticated = true });
});
app.MapPost("/api/admin/logout", async (HttpContext context) => { await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return Results.Ok(new { authenticated = false }); });
app.MapGet("/api/admin/summary", async (PlatformStore store) =>
{
    var data = await store.ReadAsync();
    var byRegion = data.Profiles.GroupBy(p => p.Region).ToDictionary(g => g.Key, g => g.Count());
    var subjectCounts = data.Resources.GroupBy(r => r.Subject).ToDictionary(g => g.Key, g => g.Count());
    return Results.Json(new { educatorCount = data.Profiles.Count, resourceCount = data.Resources.Count, replyCount = data.Topics.Sum(t => t.Replies.Count), regions = byRegion.Count, byRegion, subjectCounts, topics = data.Topics.Count });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "STAR-LINK Sprint Hub" }));
app.Run();
