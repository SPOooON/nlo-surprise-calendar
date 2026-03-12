namespace NloSurpriseCalendar.App.Persistence.Models;

public sealed class BootstrapGameOptions
{
    public const string SectionName = "BootstrapGame";

    public string Slug { get; init; } = "main";

    public string Title { get; init; } = "Default Surprise Calendar";

    public int Width { get; init; } = 100;

    public int Height { get; init; } = 100;

    public string SeedVersion { get; init; } = "default-v1";
}
