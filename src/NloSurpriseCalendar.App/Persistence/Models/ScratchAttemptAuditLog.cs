namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed record ScratchAttemptAuditLog(
    string GameSlug,
    int ReturnedCount,
    IReadOnlyList<ScratchAttemptEventSummary> Attempts);
