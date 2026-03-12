using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class ScratchAttemptReadService(
    DatabaseConnectionOptions connectionOptions,
    IOptions<BootstrapGameOptions> gameOptions)
{
    public async Task<ScratchAttemptAuditLog> GetDefaultGameAuditLogAsync(int take, CancellationToken cancellationToken = default)
    {
        var boundedTake = Math.Clamp(take, 1, 200);
        await using var connection = new NpgsqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT
                e.id,
                e.participant_id,
                e.requested_cell_index,
                e.outcome_code,
                e.claim_result,
                e.awarded_prize_type,
                e.occurred_utc
            FROM scratch_attempt_events e
            INNER JOIN games g ON g.id = e.game_id
            WHERE g.slug = @slug
            ORDER BY e.occurred_utc DESC, e.id DESC
            LIMIT @take;
            """,
            connection);

        command.Parameters.AddWithValue("slug", gameOptions.Value.Slug);
        command.Parameters.AddWithValue("take", boundedTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var attempts = new List<ScratchAttemptEventSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            attempts.Add(new ScratchAttemptEventSummary(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return new ScratchAttemptAuditLog(gameOptions.Value.Slug, attempts.Count, attempts);
    }
}
