namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record ScratchResult(
    ScratchAttemptOutcome Outcome,
    string ClaimResult,
    string? PrizeType,
    long AttemptId);
