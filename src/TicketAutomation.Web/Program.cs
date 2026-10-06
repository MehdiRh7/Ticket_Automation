using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Services;

var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "data"));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys"));

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDatabase")));
var dataProtection = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys")))
    .SetApplicationName("TicketAutomation");
if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "TicketAutomation.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<SecretProtector>();
builder.Services.AddScoped<TicketDiscoveryService>();
builder.Services.AddScoped<JobProcessor>();
builder.Services.AddSingleton<ProcessRunner>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<BranchNameFactory>();
builder.Services.AddSingleton<GitWorkspaceService>();
builder.Services.AddSingleton<IAgentProvider, CodexCliAgentProvider>();
builder.Services.AddSingleton<IAgentProvider, ClaudeCliAgentProvider>();
builder.Services.AddSingleton<AgentProviderResolver>();
builder.Services.AddHostedService<TicketPollingWorker>();
builder.Services.AddHostedService<JobProcessingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await AppDbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OptionalAdminAuthenticationMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
