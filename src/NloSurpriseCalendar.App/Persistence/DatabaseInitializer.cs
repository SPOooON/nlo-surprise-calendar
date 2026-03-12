using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class DatabaseInitializer(
    DatabaseConnectionOptions connectionOptions,
    IOptions<BootstrapGameOptions> gameOptions,
    PrizeAllocationPlanner planner,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureSchemaAsync(connection, cancellationToken);
        await EnsureDefaultGameAsync(connection, gameOptions.Value, cancellationToken);
        await ValidateSeedStateAsync(connection, gameOptions.Value, cancellationToken);

        logger.LogInformation("Database schema and bootstrap game are initialized.");
    }

    private static async Task EnsureSchemaAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS schema_versions (
                version INTEGER PRIMARY KEY,
                description TEXT NOT NULL,
                applied_utc TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS games (
                id UUID PRIMARY KEY,
                slug TEXT NOT NULL UNIQUE,
                title TEXT NOT NULL,
                width INTEGER NOT NULL CHECK (width > 0),
                height INTEGER NOT NULL CHECK (height > 0),
                seed_version TEXT NOT NULL,
                created_utc TIMESTAMPTZ NOT NULL,
                initialized_utc TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS prize_allocations (
                id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                game_id UUID NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                cell_index INTEGER NOT NULL CHECK (cell_index >= 0),
                prize_type TEXT NOT NULL CHECK (prize_type IN ('jackpot', 'consolation')),
                assigned_utc TIMESTAMPTZ NOT NULL,
                UNIQUE (game_id, cell_index)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_prize_allocations_jackpot_per_game
                ON prize_allocations (game_id)
                WHERE prize_type = 'jackpot';

            CREATE TABLE IF NOT EXISTS scratch_claims (
                id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                game_id UUID NOT NULL REFERENCES games(id) ON DELETE CASCADE,
                participant_id TEXT NOT NULL,
                cell_index INTEGER NOT NULL CHECK (cell_index >= 0),
                claim_result TEXT NOT NULL CHECK (claim_result IN ('empty', 'consolation', 'jackpot')),
                claimed_prize_type TEXT NULL CHECK (claimed_prize_type IS NULL OR claimed_prize_type IN ('jackpot', 'consolation')),
                claimed_utc TIMESTAMPTZ NOT NULL,
                UNIQUE (game_id, participant_id),
                UNIQUE (game_id, cell_index)
            );

            INSERT INTO schema_versions (version, description, applied_utc)
            VALUES (1, 'bootstrap game schema', NOW())
            ON CONFLICT (version) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDefaultGameAsync(
        NpgsqlConnection connection,
        BootstrapGameOptions options,
        CancellationToken cancellationToken)
    {
        var gameId = await TryGetGameIdAsync(connection, options.Slug, cancellationToken);
        if (gameId is not null)
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var insertGameCommand = new NpgsqlCommand(
            """
            INSERT INTO games (id, slug, title, width, height, seed_version, created_utc, initialized_utc)
            VALUES (@id, @slug, @title, @width, @height, @seedVersion, NOW(), NOW());
            """,
            connection,
            transaction);

        var newGameId = Guid.NewGuid();
        insertGameCommand.Parameters.AddWithValue("id", newGameId);
        insertGameCommand.Parameters.AddWithValue("slug", options.Slug);
        insertGameCommand.Parameters.AddWithValue("title", options.Title);
        insertGameCommand.Parameters.AddWithValue("width", options.Width);
        insertGameCommand.Parameters.AddWithValue("height", options.Height);
        insertGameCommand.Parameters.AddWithValue("seedVersion", options.SeedVersion);
        await insertGameCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var prizeAllocation in planner.BuildPlan(options))
        {
            await using var insertPrizeCommand = new NpgsqlCommand(
                """
                INSERT INTO prize_allocations (game_id, cell_index, prize_type, assigned_utc)
                VALUES (@gameId, @cellIndex, @prizeType, NOW());
                """,
                connection,
                transaction);

            insertPrizeCommand.Parameters.AddWithValue("gameId", newGameId);
            insertPrizeCommand.Parameters.AddWithValue("cellIndex", prizeAllocation.CellIndex);
            insertPrizeCommand.Parameters.AddWithValue("prizeType", prizeAllocation.PrizeType);
            await insertPrizeCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Guid?> TryGetGameIdAsync(
        NpgsqlConnection connection,
        string slug,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT id FROM games WHERE slug = @slug;",
            connection);

        command.Parameters.AddWithValue("slug", slug);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid gameId ? gameId : null;
    }

    private static async Task ValidateSeedStateAsync(
        NpgsqlConnection connection,
        BootstrapGameOptions options,
        CancellationToken cancellationToken)
    {
        await using var metadataCommand = new NpgsqlCommand(
            """
            SELECT width, height, seed_version
            FROM games
            WHERE slug = @slug;
            """,
            connection);

        metadataCommand.Parameters.AddWithValue("slug", options.Slug);
        await using var metadataReader = await metadataCommand.ExecuteReaderAsync(cancellationToken);
        if (!await metadataReader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Bootstrap game '{options.Slug}' was not found after initialization.");
        }

        var width = metadataReader.GetInt32(0);
        var height = metadataReader.GetInt32(1);
        var seedVersion = metadataReader.GetString(2);
        await metadataReader.CloseAsync();

        if (width != options.Width || height != options.Height || !string.Equals(seedVersion, options.SeedVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Bootstrap game '{options.Slug}' does not match the configured dimensions or seed version.");
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT
                COUNT(*)::INT AS total_count,
                COUNT(*) FILTER (WHERE prize_type = 'jackpot')::INT AS jackpot_count,
                COUNT(*) FILTER (WHERE prize_type = 'consolation')::INT AS consolation_count
            FROM prize_allocations pa
            INNER JOIN games g ON g.id = pa.game_id
            WHERE g.slug = @slug;
            """,
            connection);

        command.Parameters.AddWithValue("slug", options.Slug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var totalCount = reader.GetInt32(0);
        var jackpotCount = reader.GetInt32(1);
        var consolationCount = reader.GetInt32(2);

        if (totalCount != 101 || jackpotCount != 1 || consolationCount != 100)
        {
            throw new InvalidOperationException(
                $"Bootstrap game seed is invalid. Expected 101 prizes (1 jackpot, 100 consolation) but found {totalCount} total, {jackpotCount} jackpot, and {consolationCount} consolation.");
        }
    }
}
