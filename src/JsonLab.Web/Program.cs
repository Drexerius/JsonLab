using System.Security.Claims;
using JsonLab.Application;
using JsonLab.Infrastructure;
using JsonLab.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataPath);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, "keys")));
builder.Services.AddRazorComponents().AddInteractiveServerComponents(options =>
    options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/account";
    options.Cookie.Name = "JsonLab.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
});
builder.Services.AddAuthorization();
builder.Services.AddDbContextFactory<WorkspaceDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataPath, "jsonlab.db")};Foreign Keys=True"));
builder.Services.AddScoped<IWorkspaceSessionFactory, WorkspaceSessionFactory>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<AccountService>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapPost("/auth/{action}", async (string action, HttpContext context, IAntiforgery antiforgery, AccountService accounts) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest("Обновите страницу и повторите отправку формы."); }
    if (action == "logout")
    {
        await context.SignOutAsync();
        return Results.LocalRedirect("/");
    }
    if (action is not ("login" or "register")) return Results.NotFound();
    var form = await context.Request.ReadFormAsync();
    try
    {
        var user = action == "register"
            ? await accounts.RegisterAsync(form["username"].ToString(), form["password"].ToString())
            : await accounts.LoginAsync(form["username"].ToString(), form["password"].ToString())
                ?? throw new WorkspaceException("Неверный логин или пароль.");
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(new ClaimsPrincipal(identity));
        return Results.LocalRedirect("/");
    }
    catch (WorkspaceException ex)
    {
        return Results.LocalRedirect($"/account?mode={action}&error={Uri.EscapeDataString(ex.Message)}");
    }
});
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WorkspaceDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    if (builder.Configuration.GetValue<bool>("SeedDemo")) await DemoData.SeedAsync(db);
}
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
public partial class Program { }
