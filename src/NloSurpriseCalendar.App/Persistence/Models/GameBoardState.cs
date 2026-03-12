namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record GameBoardState(
    string GameSlug,
    int Width,
    int Height,
    int ScratchClaimCount,
    IReadOnlyList<GridCellState> ScratchedCells);
