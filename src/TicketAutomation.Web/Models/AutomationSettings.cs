using System.ComponentModel.DataAnnotations;

namespace TicketAutomation.Web.Models;

public enum AiProviderKind { Codex, Claude }
public enum TicketDatabaseKind { SqlServer, PostgreSql, Sqlite }

public sealed class AutomationSettings
{
    public int Id { get; set; } = 1;
    public bool PollingEnabled { get; set; }
    [Range(15, 86400)] public int PollIntervalSeconds { get; set; } = 300;
    [Range(1, 100)] public int PollBatchSize { get; set; } = 20;
    public AiProviderKind SelectedProvider { get; set; } = AiProviderKind.Codex;
    public TicketDatabaseKind TicketDatabaseKind { get; set; } = TicketDatabaseKind.SqlServer;
    public string ProtectedTicketConnectionString { get; set; } = string.Empty;
    [MaxLength(128)] public string TicketTable { get; set; } = "Tickets";
    [MaxLength(64)] public string IdColumn { get; set; } = "Id";
    [MaxLength(64)] public string TitleColumn { get; set; } = "Title";
    [MaxLength(64)] public string DescriptionColumn { get; set; } = "Description";
    [MaxLength(64)] public string CreatedAtColumn { get; set; } = "CreatedAt";
    [MaxLength(64)] public string StatusColumn { get; set; } = "Status";
    [MaxLength(128)] public string EligibleStatus { get; set; } = "New";
    [MaxLength(64)] public string VersionColumn { get; set; } = "Version";
    [MaxLength(1024)] public string RepositoryUrl { get; set; } = string.Empty;
    [MaxLength(256)] public string DefaultBaseRef { get; set; } = "main";
    [MaxLength(256)] public string VersionBaseRefTemplate { get; set; } = string.Empty;
    [MaxLength(64)] public string BranchPrefix { get; set; } = "ai/ticket-";
    // Relative paths are resolved below the OS temp directory, outside this Web project.
    [MaxLength(1024)] public string WorkingRoot { get; set; } = "workspaces";
    public bool PushEnabled { get; set; } = true;
    [Range(1, 180)] public int AgentTimeoutMinutes { get; set; } = 45;
    [MaxLength(512)] public string CodexExecutable { get; set; } = "codex";
    [MaxLength(2048)] public string CodexArguments { get; set; } = "exec --sandbox workspace-write --ephemeral --json -";
    [MaxLength(512)] public string ClaudeExecutable { get; set; } = "claude";
    [MaxLength(2048)] public string ClaudeArguments { get; set; } = "-p --output-format json --permission-mode acceptEdits";
    [MaxLength(256)] public string GitAuthorName { get; set; } = "Ticket Automation";
    [MaxLength(256)] public string GitAuthorEmail { get; set; } = "ticket-automation@localhost";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static AutomationSettings CreateDefaults() => new();
}
