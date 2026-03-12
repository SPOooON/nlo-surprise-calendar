namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record ScratchAttemptEventSummary(
    long AttemptId,
    string ParticipantId,
    int RequestedCellIndex,
    string OutcomeCode,
    string? ClaimResult,
    string? AwardedPrizeType,
    DateTimeOffset OccurredUtc);
