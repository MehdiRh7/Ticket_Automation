using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketAutomation.Web.Data;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Services;

public sealed class TicketDiscoveryService(AppDbContext db, SettingsService settingsService, ILogger<TicketDiscoveryService> logger)
{
    public async Task<int> DiscoverAsync(CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        var connectionString = settingsService.GetConnectionString(settings);
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("رشته اتصال دیتابیس تیکت تنظیم نشده است.");

        var tickets = await ReadTicketsAsync(settings, connectionString, ct);
        var created = 0;
        foreach (var ticket in tickets)
        {
            if (await db.Jobs.AnyAsync(x => x.ExternalTicketId == ticket.Id, ct)) continue;
            db.Jobs.Add(new TicketJob
            {
                ExternalTicketId = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                ApplicationVersion = ticket.Version,
                TicketCreatedAt = ticket.CreatedAt,
                Provider = settings.SelectedProvider,
                Status = JobStatus.Pending
            });
            try
            {
                await db.SaveChangesAsync(ct);
                created++;
            }
            catch (DbUpdateException ex)
            {
                logger.LogInformation(ex, "Ticket {TicketId} was already discovered by another worker.", ticket.Id);
                db.ChangeTracker.Clear();
            }
        }
        return created;
    }

    private static async Task<IReadOnlyList<ExternalTicket>> ReadTicketsAsync(AutomationSettings settings, string connectionString, CancellationToken ct)
    {
        await using var connection = CreateConnection(settings.TicketDatabaseKind, connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = BuildQuery(settings);
        AddParameter(command, "status", settings.EligibleStatus);
        AddParameter(command, "limit", settings.PollBatchSize);

        var results = new List<ExternalTicket>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var id = Convert.ToString(reader["TicketId"], CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id)) continue;
            var title = Convert.ToString(reader["TicketTitle"], CultureInfo.InvariantCulture) ?? "بدون عنوان";
            var description = Convert.ToString(reader["TicketDescription"], CultureInfo.InvariantCulture) ?? string.Empty;
            DateTimeOffset? createdAt = null;
            if (reader["TicketCreatedAt"] is not DBNull)
            {
                if (reader["TicketCreatedAt"] is DateTimeOffset dto) createdAt = dto;
                else if (reader["TicketCreatedAt"] is DateTime dt) createdAt = new DateTimeOffset(dt);
                else if (DateTimeOffset.TryParse(Convert.ToString(reader["TicketCreatedAt"], CultureInfo.InvariantCulture), out var parsed)) createdAt = parsed;
            }
            var version = reader["TicketVersion"] is DBNull ? null : Convert.ToString(reader["TicketVersion"], CultureInfo.InvariantCulture);
            results.Add(new ExternalTicket(id, title, description, createdAt, version));
        }
        return results;
    }

    private static DbConnection CreateConnection(TicketDatabaseKind kind, string connectionString) => kind switch
    {
        TicketDatabaseKind.SqlServer => new SqlConnection(connectionString),
        TicketDatabaseKind.PostgreSql => new NpgsqlConnection(connectionString),
        TicketDatabaseKind.Sqlite => new SqliteConnection(connectionString),
        _ => throw new NotSupportedException($"Database provider {kind} is not supported.")
    };

    public static string BuildQuery(AutomationSettings settings)
    {
        SqlIdentifier.ValidateMultipart(settings.TicketTable);
        foreach (var identifier in new[] { settings.IdColumn, settings.TitleColumn, settings.DescriptionColumn, settings.CreatedAtColumn, settings.StatusColumn })
            SqlIdentifier.Validate(identifier);
        if (!string.IsNullOrWhiteSpace(settings.VersionColumn)) SqlIdentifier.Validate(settings.VersionColumn);
        var quote = settings.TicketDatabaseKind == TicketDatabaseKind.SqlServer
            ? (Func<string, string>)(x => string.Join('.', x.Split('.').Select(p => $"[{p}]")))
            : x => string.Join('.', x.Split('.').Select(p => $"\"{p}\""));
        var top = settings.TicketDatabaseKind == TicketDatabaseKind.SqlServer ? "TOP (@limit) " : string.Empty;
        var limit = settings.TicketDatabaseKind == TicketDatabaseKind.SqlServer ? string.Empty : " LIMIT @limit";
        var version = string.IsNullOrWhiteSpace(settings.VersionColumn) ? "NULL" : quote(settings.VersionColumn);
        return $"SELECT {top}{quote(settings.IdColumn)} AS TicketId, {quote(settings.TitleColumn)} AS TicketTitle, " +
               $"{quote(settings.DescriptionColumn)} AS TicketDescription, {quote(settings.CreatedAtColumn)} AS TicketCreatedAt, " +
               $"{version} AS TicketVersion FROM {quote(settings.TicketTable)} " +
               $"WHERE {quote(settings.StatusColumn)} = @status ORDER BY {quote(settings.CreatedAtColumn)} DESC{limit};";
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
