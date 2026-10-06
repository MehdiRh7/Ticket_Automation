using System.Text;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Services;

public sealed class BranchNameFactory
{
    public string Create(string prefix, string ticketId, string title)
    {
        if (!prefix.StartsWith("ai/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only branches under ai/ are permitted.");
        var id = Slug(ticketId, 40);
        var titleSlug = Slug(title, 50);
        return $"{prefix}{id}-{titleSlug}".TrimEnd('-');
    }

    private static string Slug(string value, int maxLength)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = normalized.Select(c => char.IsLetterOrDigit(c) && c <= 127 ? c : '-').ToArray();
        var slug = System.Text.RegularExpressions.Regex.Replace(new string(chars), "-+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug)) slug = "item";
        return slug[..Math.Min(slug.Length, maxLength)];
    }
}

public sealed class GitWorkspaceService(ProcessRunner runner, IWebHostEnvironment environment)
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes(10);

    public async Task<string> CreateAsync(AutomationSettings settings, TicketJob job, string branchName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.RepositoryUrl)) throw new InvalidOperationException("آدرس Repository تنظیم نشده است.");
        var root = Path.GetFullPath(settings.WorkingRoot, environment.ContentRootPath);
        Directory.CreateDirectory(root);
        var workspace = Path.Combine(root, $"job-{job.Id}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        EnsureSuccess(await GitAsync(root, ct, "clone", "--no-tags", settings.RepositoryUrl, workspace), "Clone مخزن");
        var baseRef = ResolveBaseRef(settings, job);
        var checkout = await GitAsync(workspace, ct, "checkout", "-b", branchName, $"origin/{baseRef}");
        if (!checkout.Succeeded)
        {
            checkout = await GitAsync(workspace, ct, "checkout", "-b", branchName, baseRef);
            EnsureSuccess(checkout, "ساخت شاخه از مرجع مبنا");
        }
        await GitAsync(workspace, ct, "config", "user.name", settings.GitAuthorName);
        await GitAsync(workspace, ct, "config", "user.email", settings.GitAuthorEmail);
        EnsureSuccess(await GitAsync(workspace, ct, "remote", "remove", "origin"), "جداسازی Remote پیش از اجرای Agent");
        return workspace;
    }

    public async Task<string> GetHeadAsync(string workspace, CancellationToken ct)
    {
        var result = await GitAsync(workspace, ct, "rev-parse", "HEAD");
        EnsureSuccess(result, "خواندن HEAD");
        return result.StandardOutput.Trim();
    }

    public string ResolveBaseRef(AutomationSettings settings, TicketJob job)
    {
        if (!string.IsNullOrWhiteSpace(settings.VersionBaseRefTemplate) && !string.IsNullOrWhiteSpace(job.ApplicationVersion))
            return settings.VersionBaseRefTemplate.Replace("{version}", job.ApplicationVersion, StringComparison.OrdinalIgnoreCase);
        return settings.DefaultBaseRef;
    }

    public async Task<(bool HasChanges, string ChangedFiles)> GetChangesAsync(string workspace, CancellationToken ct)
    {
        var status = await GitAsync(workspace, ct, "status", "--porcelain");
        EnsureSuccess(status, "خواندن وضعیت Git");
        return (!string.IsNullOrWhiteSpace(status.StandardOutput), status.StandardOutput.Trim());
    }

    public async Task<string> CommitAndPushAsync(AutomationSettings settings, TicketJob job, string workspace, string branchName, CancellationToken ct)
    {
        EnsureBranchAllowed(settings, branchName);
        EnsureSuccess(await GitAsync(workspace, ct, "add", "--all"), "افزودن تغییرات");
        var message = $"fix(ticket-{job.ExternalTicketId}): AI-assisted remediation";
        EnsureSuccess(await GitAsync(workspace, ct, "commit", "-m", message), "ثبت Commit");
        var sha = await GitAsync(workspace, ct, "rev-parse", "HEAD");
        EnsureSuccess(sha, "خواندن Commit SHA");
        if (settings.PushEnabled)
        {
            EnsureSuccess(await GitAsync(workspace, ct, "remote", "add", "origin", settings.RepositoryUrl), "اتصال دوباره Remote");
            EnsureSuccess(await GitAsync(workspace, ct, "push", "--set-upstream", "origin", branchName), "Push شاخه");
        }
        return sha.StandardOutput.Trim();
    }

    private static void EnsureBranchAllowed(AutomationSettings settings, string branchName)
    {
        if (!branchName.StartsWith(settings.BranchPrefix, StringComparison.Ordinal) || !branchName.StartsWith("ai/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("تلاش برای Push به شاخه‌ای خارج از محدوده مجاز متوقف شد.");
    }

    private async Task<ProcessResult> GitAsync(string workingDirectory, CancellationToken ct, params string[] args) =>
        await runner.RunAsync("git", args, workingDirectory, null, GitTimeout, ct);

    private static void EnsureSuccess(ProcessResult result, string operation)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"{operation} ناموفق بود: {result.StandardError}");
    }
}
