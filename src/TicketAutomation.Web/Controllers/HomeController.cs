using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;
using TicketAutomation.Web.Services;

namespace TicketAutomation.Web.Controllers;

public class HomeController(AppDbContext db, SettingsService settingsService, IOptions<AdminAuthOptions> auth) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settings = await settingsService.GetAsync(ct);
        var recent = await db.Jobs.OrderByDescending(x => x.Id).Take(10).AsNoTracking().ToListAsync(ct);
        var grouped = await db.Jobs.GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.Count() }).ToListAsync(ct);
        var counts = Enum.GetValues<JobStatus>().ToDictionary(x => x, x => grouped.FirstOrDefault(y => y.Key == x)?.Count ?? 0);
        return View(new DashboardViewModel
        {
            Settings = settings,
            RecentJobs = recent,
            Counts = counts,
            AuthenticationEnabled = auth.Value.Enabled
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
