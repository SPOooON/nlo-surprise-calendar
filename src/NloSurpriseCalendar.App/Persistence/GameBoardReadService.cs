using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class GameBoardReadService(
    DatabaseConnectionOptions connectionOptions,
    IOptions<BootstrapGameOptions> gameOptions)
{
    public async Task<GameBoardState?> GetDefaultGameBoardStateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT
                g.slug,
                g.width,
                g.height,
                sc.cell_index,
                sc.claim_result,
                sc.claimed_prize_type,
                sc.claimed_utc
            FROM games g
            LEFT JOIN scratch_claims sc ON sc.game_id = g.id
            WHERE g.slug = @slug
            ORDER BY sc.cell_index;
            """,
            connection);

        command.Parameters.AddWithValue("slug", gameOptions.Value.Slug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        string? slug = null;
        var width = 0;
        var height = 0;
        var scratchedCells = new List<GridCellState>();

        while (await reader.ReadAsync(cancellationToken))
        {
            slug ??= reader.GetString(0);
            width = reader.GetInt32(1);
            height = reader.GetInt32(2);

            if (reader.IsDBNull(3))
            {
                continue;
            }

            scratchedCells.Add(new GridCellState(
                reader.GetInt32(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return slug is null
            ? null
            : new GameBoardState(slug, width, height, scratchedCells.Count, scratchedCells);
    }
}
