using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class ScratchService(
    DatabaseConnectionOptions connectionOptions,
    IOptions<BootstrapGameOptions> gameOptions)
{
    public async Task<ScratchResult> ScratchAsync(ScratchRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var game = await GetGameAsync(connection, gameOptions.Value.Slug, cancellationToken)
            ?? throw new InvalidOperationException($"Bootstrap game '{gameOptions.Value.Slug}' is missing.");

        if (string.IsNullOrWhiteSpace(request.ParticipantId))
        {
            var invalidAttemptId = await InsertAttemptEventAsync(
                connection,
                transaction: null,
                game.Id,
                participantId: string.Empty,
                request.CellIndex,
                ScratchAttemptOutcome.InvalidParticipant,
                claimResult: null,
                prizeType: null,
                cancellationToken);

            return new ScratchResult(ScratchAttemptOutcome.InvalidParticipant, "invalid_participant", null, invalidAttemptId);
        }

        if (request.CellIndex < 0 || request.CellIndex >= game.Width * game.Height)
        {
            var invalidAttemptId = await InsertAttemptEventAsync(
                connection,
                transaction: null,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                ScratchAttemptOutcome.InvalidCell,
                claimResult: null,
                prizeType: null,
                cancellationToken);

            return new ScratchResult(ScratchAttemptOutcome.InvalidCell, "invalid_cell", null, invalidAttemptId);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (await ParticipantHasClaimAsync(connection, transaction, game.Id, request.ParticipantId, cancellationToken))
        {
            var attemptId = await InsertAttemptEventAsync(
                connection,
                transaction,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                ScratchAttemptOutcome.ParticipantAlreadyScratched,
                claimResult: null,
                prizeType: null,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ScratchResult(ScratchAttemptOutcome.ParticipantAlreadyScratched, "participant_already_scratched", null, attemptId);
        }

        if (await CellHasClaimAsync(connection, transaction, game.Id, request.CellIndex, cancellationToken))
        {
            var attemptId = await InsertAttemptEventAsync(
                connection,
                transaction,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                ScratchAttemptOutcome.CellAlreadyScratched,
                claimResult: null,
                prizeType: null,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ScratchResult(ScratchAttemptOutcome.CellAlreadyScratched, "cell_already_scratched", null, attemptId);
        }

        var prizeType = await GetPrizeTypeAsync(connection, transaction, game.Id, request.CellIndex, cancellationToken);
        var claimResult = prizeType switch
        {
            "jackpot" => "jackpot",
            "consolation" => "consolation",
            _ => "empty",
        };

        try
        {
            await InsertScratchClaimAsync(
                connection,
                transaction,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                claimResult,
                prizeType,
                cancellationToken);

            var attemptId = await InsertAttemptEventAsync(
                connection,
                transaction,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                ScratchAttemptOutcome.Accepted,
                claimResult,
                prizeType,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ScratchResult(ScratchAttemptOutcome.Accepted, claimResult, prizeType, attemptId);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);

            var resolvedOutcome = await ResolveConflictOutcomeAsync(
                connection,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                cancellationToken);

            var attemptId = await InsertAttemptEventAsync(
                connection,
                transaction: null,
                game.Id,
                request.ParticipantId,
                request.CellIndex,
                resolvedOutcome,
                claimResult: null,
                prizeType: null,
                cancellationToken);

            var resultCode = resolvedOutcome == ScratchAttemptOutcome.ParticipantAlreadyScratched
                ? "participant_already_scratched"
                : "cell_already_scratched";

            return new ScratchResult(resolvedOutcome, resultCode, null, attemptId);
        }
    }

    private static async Task<GameRecord?> GetGameAsync(
        NpgsqlConnection connection,
        string slug,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT id, width, height
            FROM games
            WHERE slug = @slug;
            """,
            connection);

        command.Parameters.AddWithValue("slug", slug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new GameRecord(reader.GetGuid(0), reader.GetInt32(1), reader.GetInt32(2));
    }

    private static async Task<bool> ParticipantHasClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameId,
        string participantId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM scratch_claims
                WHERE game_id = @gameId AND participant_id = @participantId
            );
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("participantId", participantId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> CellHasClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameId,
        int cellIndex,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM scratch_claims
                WHERE game_id = @gameId AND cell_index = @cellIndex
            );
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("cellIndex", cellIndex);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<string?> GetPrizeTypeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameId,
        int cellIndex,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT prize_type
            FROM prize_allocations
            WHERE game_id = @gameId AND cell_index = @cellIndex;
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("cellIndex", cellIndex);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    private static async Task InsertScratchClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameId,
        string participantId,
        int cellIndex,
        string claimResult,
        string? prizeType,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO scratch_claims (game_id, participant_id, cell_index, claim_result, claimed_prize_type, claimed_utc)
            VALUES (@gameId, @participantId, @cellIndex, @claimResult, @claimedPrizeType, NOW());
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("participantId", participantId);
        command.Parameters.AddWithValue("cellIndex", cellIndex);
        command.Parameters.AddWithValue("claimResult", claimResult);
        command.Parameters.AddWithValue("claimedPrizeType", (object?)prizeType ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> InsertAttemptEventAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid gameId,
        string participantId,
        int cellIndex,
        ScratchAttemptOutcome outcome,
        string? claimResult,
        string? prizeType,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO scratch_attempt_events (
                game_id,
                participant_id,
                requested_cell_index,
                outcome_code,
                claim_result,
                awarded_prize_type,
                occurred_utc
            )
            VALUES (@gameId, @participantId, @cellIndex, @outcomeCode, @claimResult, @awardedPrizeType, NOW())
            RETURNING id;
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("participantId", participantId);
        command.Parameters.AddWithValue("cellIndex", cellIndex);
        command.Parameters.AddWithValue("outcomeCode", ToOutcomeCode(outcome));
        command.Parameters.AddWithValue("claimResult", (object?)claimResult ?? DBNull.Value);
        command.Parameters.AddWithValue("awardedPrizeType", (object?)prizeType ?? DBNull.Value);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<ScratchAttemptOutcome> ResolveConflictOutcomeAsync(
        NpgsqlConnection connection,
        Guid gameId,
        string participantId,
        int cellIndex,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT
                EXISTS (
                    SELECT 1
                    FROM scratch_claims
                    WHERE game_id = @gameId AND participant_id = @participantId
                ) AS participant_has_claim,
                EXISTS (
                    SELECT 1
                    FROM scratch_claims
                    WHERE game_id = @gameId AND cell_index = @cellIndex
                ) AS cell_has_claim;
            """,
            connection);

        command.Parameters.AddWithValue("gameId", gameId);
        command.Parameters.AddWithValue("participantId", participantId);
        command.Parameters.AddWithValue("cellIndex", cellIndex);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var participantHasClaim = reader.GetBoolean(0);
        var cellHasClaim = reader.GetBoolean(1);

        if (participantHasClaim)
        {
            return ScratchAttemptOutcome.ParticipantAlreadyScratched;
        }

        return cellHasClaim
            ? ScratchAttemptOutcome.CellAlreadyScratched
            : ScratchAttemptOutcome.CellAlreadyScratched;
    }

    private static string ToOutcomeCode(ScratchAttemptOutcome outcome) => outcome switch
    {
        ScratchAttemptOutcome.Accepted => "accepted",
        ScratchAttemptOutcome.InvalidCell => "invalid_cell",
        ScratchAttemptOutcome.InvalidParticipant => "invalid_participant",
        ScratchAttemptOutcome.ParticipantAlreadyScratched => "participant_already_scratched",
        ScratchAttemptOutcome.CellAlreadyScratched => "cell_already_scratched",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };

    private sealed record GameRecord(Guid Id, int Width, int Height);
}
