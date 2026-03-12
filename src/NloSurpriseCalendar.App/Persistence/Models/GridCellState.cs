namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record GridCellState(
    int CellIndex,
    string ClaimResult,
    string? PrizeType,
    DateTimeOffset ClaimedUtc);
