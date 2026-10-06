using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Services;

public sealed class TicketPollingWorker(IServiceScopeFactory scopes, ILogger<TicketPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromMinutes(5);
            try
            {
                using var scope = scopes.CreateScope();
                var settings = await scope.ServiceProvider.GetRequiredService<SettingsService>().GetAsync(stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Max(15, settings.PollIntervalSeconds));
                if (settings.PollingEnabled)
                {
                    var count = await scope.ServiceProvider.GetRequiredService<TicketDiscoveryService>().DiscoverAsync(stoppingToken);
                    if (count > 0) logger.LogInformation("Discovered {Count} new tickets.", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Ticket polling failed."); }
            await Task.Delay(delay, stoppingToken);
        }
    }
}

public sealed class JobProcessingWorker(IServiceScopeFactory scopes, ILogger<JobProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var processed = await scope.ServiceProvider.GetRequiredService<JobProcessor>().ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job worker failed.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = await db.Jobs.Where(x => x.Status == JobStatus.Running).ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = "اجرای قبلی با توقف سرویس قطع شده است؛ برای اجرای دوباره Retry را انتخاب کنید.";
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        if (jobs.Count > 0) await db.SaveChangesAsync(ct);
    }
}
