using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;
using TicketAutomation.Web.Services;

namespace TicketAutomation.Web.Controllers;

public sealed class JobsController(AppDbContext db, TicketDiscoveryService discovery) : Controller
{
    public async Task<IActionResult> Index(JobStatus? status, CancellationToken ct)
    {
        var query = db.Jobs.AsNoTracking().OrderByDescending(x => x.Id).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value).OrderByDescending(x => x.Id);
        ViewBag.Status = status;
        return View(await query.Take(200).ToListAsync(ct));
    }

    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return job is null ? NotFound() : View(job);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PollNow(CancellationToken ct)
    {
        try
        {
            var count = await discovery.DiscoverAsync(ct);
            TempData["Success"] = $"بررسی انجام شد؛ {count} تیکت جدید به صف اضافه شد.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(long id, CancellationToken ct)
    {
        var job = await db.Jobs.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return NotFound();
        if (job.Status is JobStatus.Running or JobStatus.Pending) return BadRequest();
        job.Status = JobStatus.Pending;
        job.ErrorMessage = null;
        job.CompletedAt = null;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "کار دوباره به صف اضافه شد.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
