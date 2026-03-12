using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class GameSummaryReadService(
    DatabaseConnectionOptions connectionOptions,
    IOptions<BootstrapGameOptions> gameOptions)
{
    public async Task<GameSummary?> GetDefaultGameSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT
                g.id,
                g.slug,
                g.title,
                g.width,
                g.height,
                COUNT(*) FILTER (WHERE pa.prize_type = 'jackpot')::INT AS jackpot_count,
                COUNT(*) FILTER (WHERE pa.prize_type = 'consolation')::INT AS consolation_count,
                (SELECT COUNT(*)::INT FROM scratch_claims sc WHERE sc.game_id = g.id) AS scratch_claim_count,
                g.initialized_utc,
                g.seed_version
            FROM games g
            LEFT JOIN prize_allocations pa ON pa.game_id = g.id
            WHERE g.slug = @slug
            GROUP BY g.id, g.slug, g.title, g.width, g.height, g.initialized_utc, g.seed_version;
            """,
            connection);

        command.Parameters.AddWithValue("slug", gameOptions.Value.Slug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new GameSummary(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7),
            reader.GetFieldValue<DateTimeOffset>(8),
            reader.GetString(9));
    }
}
