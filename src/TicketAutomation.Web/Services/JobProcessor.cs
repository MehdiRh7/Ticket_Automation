using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Services;

public sealed class JobProcessor(
    AppDbContext db,
    SettingsService settingsService,
    BranchNameFactory branches,
    GitWorkspaceService git,
    AgentProviderResolver providers,
    ILogger<JobProcessor> logger)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct = default)
    {
        var job = await db.Jobs.Where(x => x.Status == JobStatus.Pending).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (job is null) return false;
        job.Status = JobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.AttemptCount++;
        await db.SaveChangesAsync(ct);

        try
        {
            var settings = await settingsService.GetAsync(ct);
            var retrySuffix = job.AttemptCount > 1 ? $" retry {job.AttemptCount}" : string.Empty;
            var branch = branches.Create(settings.BranchPrefix, job.ExternalTicketId, job.Title + retrySuffix);
            job.BranchName = branch;
            job.BaseRef = git.ResolveBaseRef(settings, job);
            job.WorkspacePath = await git.CreateAsync(settings, job, branch, ct);
            var baselineHead = await git.GetHeadAsync(job.WorkspacePath, ct);
            await db.SaveChangesAsync(ct);

            var agent = providers.Resolve(job.Provider);
            var result = await agent.ExecuteAsync(job, settings, job.WorkspacePath, ct);
            job.AgentSummary = result.StandardOutput;
            job.ExecutionLog = $"Provider: {job.Provider}\nExit code: {result.ExitCode}\nDuration: {result.Duration}\n\nSTDERR:\n{result.StandardError}";
            if (!result.Succeeded)
                throw new InvalidOperationException($"Agent با کد {result.ExitCode} خاتمه یافت. {result.StandardError}");
            var headAfterAgent = await git.GetHeadAsync(job.WorkspacePath, ct);
            if (!string.Equals(baselineHead, headAfterAgent, StringComparison.Ordinal))
                throw new InvalidOperationException("Agent تاریخچه Git را تغییر داده است. اجرای Job برای جلوگیری از Push ناامن متوقف شد.");

            var changes = await git.GetChangesAsync(job.WorkspacePath, ct);
            job.ChangedFiles = changes.ChangedFiles;
            if (!changes.HasChanges)
            {
                job.Status = LooksLikeMissingInformation(result.StandardOutput) ? JobStatus.NeedsInformation : JobStatus.NoChanges;
            }
            else
            {
                job.CommitSha = await git.CommitAndPushAsync(settings, job, job.WorkspacePath, branch, ct);
                job.Status = JobStatus.Completed;
            }
            job.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Processing ticket job {JobId} failed.", job.Id);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            return true;
        }
    }

    private static bool LooksLikeMissingInformation(string text) =>
        text.Contains("insufficient", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("missing information", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("اطلاعات کافی", StringComparison.OrdinalIgnoreCase);
}
