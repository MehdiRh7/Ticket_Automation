using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Services;

public sealed class SecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("TicketAutomation.TicketDb.v1");
    public string Protect(string value) => _protector.Protect(value);
    public string Unprotect(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : _protector.Unprotect(value);
}

public sealed class SettingsService(AppDbContext db, SecretProtector secrets)
{
    public Task<AutomationSettings> GetAsync(CancellationToken ct = default) =>
        db.Settings.SingleAsync(x => x.Id == 1, ct);

    public async Task<SettingsViewModel> GetViewModelAsync(CancellationToken ct = default)
    {
        var x = await GetAsync(ct);
        return new SettingsViewModel
        {
            PollingEnabled = x.PollingEnabled,
            PollIntervalSeconds = x.PollIntervalSeconds,
            PollBatchSize = x.PollBatchSize,
            SelectedProvider = x.SelectedProvider,
            TicketDatabaseKind = x.TicketDatabaseKind,
            HasSavedConnectionString = !string.IsNullOrWhiteSpace(x.ProtectedTicketConnectionString),
            TicketTable = x.TicketTable,
            IdColumn = x.IdColumn,
            TitleColumn = x.TitleColumn,
            DescriptionColumn = x.DescriptionColumn,
            CreatedAtColumn = x.CreatedAtColumn,
            StatusColumn = x.StatusColumn,
            EligibleStatus = x.EligibleStatus,
            VersionColumn = x.VersionColumn,
            RepositoryUrl = x.RepositoryUrl,
            DefaultBaseRef = x.DefaultBaseRef,
            VersionBaseRefTemplate = x.VersionBaseRefTemplate,
            BranchPrefix = x.BranchPrefix,
            WorkingRoot = x.WorkingRoot,
            PushEnabled = x.PushEnabled,
            AgentTimeoutMinutes = x.AgentTimeoutMinutes,
            CodexExecutable = x.CodexExecutable,
            CodexArguments = x.CodexArguments,
            ClaudeExecutable = x.ClaudeExecutable,
            ClaudeArguments = x.ClaudeArguments,
            GitAuthorName = x.GitAuthorName,
            GitAuthorEmail = x.GitAuthorEmail
        };
    }

    public async Task UpdateAsync(SettingsViewModel input, CancellationToken ct = default)
    {
        SqlIdentifier.ValidateMultipart(input.TicketTable);
        foreach (var identifier in new[] { input.IdColumn, input.TitleColumn, input.DescriptionColumn, input.CreatedAtColumn, input.StatusColumn })
            SqlIdentifier.Validate(identifier);
        if (!string.IsNullOrWhiteSpace(input.VersionColumn)) SqlIdentifier.Validate(input.VersionColumn);
        if (!input.BranchPrefix.StartsWith("ai/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("پیشوند شاخه باید با ai/ شروع شود.");

        var x = await GetAsync(ct);
        x.PollingEnabled = input.PollingEnabled;
        x.PollIntervalSeconds = input.PollIntervalSeconds;
        x.PollBatchSize = input.PollBatchSize;
        x.SelectedProvider = input.SelectedProvider;
        x.TicketDatabaseKind = input.TicketDatabaseKind;
        if (!string.IsNullOrWhiteSpace(input.TicketConnectionString))
            x.ProtectedTicketConnectionString = secrets.Protect(input.TicketConnectionString.Trim());
        x.TicketTable = input.TicketTable.Trim();
        x.IdColumn = input.IdColumn.Trim();
        x.TitleColumn = input.TitleColumn.Trim();
        x.DescriptionColumn = input.DescriptionColumn.Trim();
        x.CreatedAtColumn = input.CreatedAtColumn.Trim();
        x.StatusColumn = input.StatusColumn.Trim();
        x.EligibleStatus = input.EligibleStatus.Trim();
        x.VersionColumn = input.VersionColumn?.Trim() ?? string.Empty;
        x.RepositoryUrl = input.RepositoryUrl.Trim();
        x.DefaultBaseRef = input.DefaultBaseRef.Trim();
        x.VersionBaseRefTemplate = input.VersionBaseRefTemplate?.Trim() ?? string.Empty;
        x.BranchPrefix = input.BranchPrefix.Trim();
        x.WorkingRoot = input.WorkingRoot.Trim();
        x.PushEnabled = input.PushEnabled;
        x.AgentTimeoutMinutes = input.AgentTimeoutMinutes;
        x.CodexExecutable = input.CodexExecutable.Trim();
        x.CodexArguments = input.CodexArguments.Trim();
        x.ClaudeExecutable = input.ClaudeExecutable.Trim();
        x.ClaudeArguments = input.ClaudeArguments.Trim();
        x.GitAuthorName = input.GitAuthorName.Trim();
        x.GitAuthorEmail = input.GitAuthorEmail.Trim();
        x.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public string GetConnectionString(AutomationSettings settings) => secrets.Unprotect(settings.ProtectedTicketConnectionString);
}

public static class SqlIdentifier
{
    public static string Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z_][A-Za-z0-9_]*$"))
            throw new InvalidOperationException($"نام جدول یا ستون نامعتبر است: {value}");
        return value;
    }

    public static string ValidateMultipart(string value)
    {
        foreach (var part in value.Split('.')) Validate(part);
        return value;
    }
}
