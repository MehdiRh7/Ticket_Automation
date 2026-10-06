using System.ComponentModel.DataAnnotations;

namespace TicketAutomation.Web.Models;

public enum JobStatus { Pending, Running, NeedsInformation, NoChanges, Completed, Failed }

public sealed class TicketJob
{
    public long Id { get; set; }
    [MaxLength(256)] public string ExternalTicketId { get; set; } = string.Empty;
    [MaxLength(1000)] public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [MaxLength(256)] public string? ApplicationVersion { get; set; }
    public DateTimeOffset? TicketCreatedAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public AiProviderKind Provider { get; set; }
    [MaxLength(512)] public string? BaseRef { get; set; }
    [MaxLength(512)] public string? BranchName { get; set; }
    [MaxLength(128)] public string? CommitSha { get; set; }
    [MaxLength(1024)] public string? WorkspacePath { get; set; }
    public string? AgentSummary { get; set; }
    public string? ChangedFiles { get; set; }
    public string? ExecutionLog { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed record ExternalTicket(string Id, string Title, string Description, DateTimeOffset? CreatedAt, string? Version);

public sealed record AgentExecutionResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0;
}
