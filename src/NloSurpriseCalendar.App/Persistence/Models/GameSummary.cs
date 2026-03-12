namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record GameSummary(
    Guid GameId,
    string Slug,
    string Title,
    int Width,
    int Height,
    int JackpotPrizeCount,
    int ConsolationPrizeCount,
    int ScratchClaimCount,
    DateTimeOffset InitializedUtc,
    string SeedVersion);
