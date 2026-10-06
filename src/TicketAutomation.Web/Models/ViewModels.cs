using System.ComponentModel.DataAnnotations;

namespace TicketAutomation.Web.Models;

public sealed class DashboardViewModel
{
    public required AutomationSettings Settings { get; init; }
    public required IReadOnlyList<TicketJob> RecentJobs { get; init; }
    public required IReadOnlyDictionary<JobStatus, int> Counts { get; init; }
    public bool AuthenticationEnabled { get; init; }
}

public sealed class SettingsViewModel
{
    public bool PollingEnabled { get; set; }
    [Range(15, 86400)] public int PollIntervalSeconds { get; set; } = 300;
    [Range(1, 100)] public int PollBatchSize { get; set; } = 20;
    public AiProviderKind SelectedProvider { get; set; }
    public TicketDatabaseKind TicketDatabaseKind { get; set; }
    [DataType(DataType.Password)] public string? TicketConnectionString { get; set; }
    public bool HasSavedConnectionString { get; set; }
    [Required] public string TicketTable { get; set; } = "Tickets";
    [Required] public string IdColumn { get; set; } = "Id";
    [Required] public string TitleColumn { get; set; } = "Title";
    [Required] public string DescriptionColumn { get; set; } = "Description";
    [Required] public string CreatedAtColumn { get; set; } = "CreatedAt";
    [Required] public string StatusColumn { get; set; } = "Status";
    [Required] public string EligibleStatus { get; set; } = "New";
    public string VersionColumn { get; set; } = "Version";
    [Required] public string RepositoryUrl { get; set; } = string.Empty;
    [Required] public string DefaultBaseRef { get; set; } = "main";
    public string VersionBaseRefTemplate { get; set; } = string.Empty;
    [Required] public string BranchPrefix { get; set; } = "ai/ticket-";
    [Required] public string WorkingRoot { get; set; } = "workspaces";
    public bool PushEnabled { get; set; }
    [Range(1, 180)] public int AgentTimeoutMinutes { get; set; } = 45;
    [Required] public string CodexExecutable { get; set; } = "codex";
    [Required] public string CodexArguments { get; set; } = "exec --sandbox workspace-write --ephemeral --json -";
    [Required] public string ClaudeExecutable { get; set; } = "claude";
    [Required] public string ClaudeArguments { get; set; } = "-p --output-format json --permission-mode acceptEdits";
    [Required] public string GitAuthorName { get; set; } = "Ticket Automation";
    [Required, EmailAddress] public string GitAuthorEmail { get; set; } = "ticket-automation@localhost";
}

public sealed class LoginViewModel
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
