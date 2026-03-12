using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using NloSurpriseCalendar.App.Components;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence;
using NloSurpriseCalendar.App.Persistence.Models;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "NLO Surprise Calendar API",
            Version = "v1",
            Description = "Demo API for the Nederlandse Loterij surprise calendar assignment. It exposes the bootstrap game summary and the transactional scratch operation.",
        };

        return Task.CompletedTask;
    });
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton(new DatabaseConnectionOptions(connectionString));
builder.Services.Configure<BootstrapGameOptions>(builder.Configuration.GetSection(BootstrapGameOptions.SectionName));
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
builder.Services.AddSingleton<PrizeAllocationPlanner>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<GameSummaryReadService>();
builder.Services.AddSingleton<GameBoardReadService>();
builder.Services.AddSingleton<ScratchService>();
builder.Services.AddSingleton<ScratchAttemptReadService>();

var app = builder.Build();
await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
})
    .WithName("LiveHealth")
    .WithSummary("Check whether the app process is alive.")
    .WithDescription("Returns 200 when the application is running, without checking external dependencies.");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
})
    .WithName("ReadyHealth")
    .WithSummary("Check whether the app is ready to serve requests.")
    .WithDescription("Returns 200 only when the PostgreSQL dependency passes the readiness check.");
app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference("/docs", options => options.WithTitle("NLO Surprise Calendar API"));
app.MapGet("/api/bootstrap/default-game", async (GameSummaryReadService readService, CancellationToken cancellationToken) =>
{
    var summary = await readService.GetDefaultGameSummaryAsync(cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
})
    .WithName("GetDefaultGameSummary")
    .WithSummary("Get the configured default game's summary.")
    .WithDescription("Returns the default game's dimensions, seeded prize counts, current scratch-claim count, initialization timestamp, and seed version.");
app.MapGet("/api/games/default-game/grid-state", async (GameBoardReadService readService, CancellationToken cancellationToken) =>
{
    var boardState = await readService.GetDefaultGameBoardStateAsync(cancellationToken);
    return boardState is null ? Results.NotFound() : Results.Ok(boardState);
})
    .WithName("GetDefaultGameBoardState")
    .WithSummary("Get the current board state for the default game.")
    .WithDescription("Returns the default game's dimensions and the set of scratched cells with their revealed outcomes. Unclaimed cells remain unrevealed.");
app.MapPost("/api/games/default-game/scratch", async (ScratchRequest request, ScratchService scratchService, CancellationToken cancellationToken) =>
{
    var result = await scratchService.ScratchAsync(request, cancellationToken);

    return result.Outcome switch
    {
        ScratchAttemptOutcome.Accepted => Results.Ok(result),
        ScratchAttemptOutcome.InvalidCell => Results.BadRequest(result),
        ScratchAttemptOutcome.InvalidParticipant => Results.BadRequest(result),
        ScratchAttemptOutcome.ParticipantAlreadyScratched => Results.Conflict(result),
        ScratchAttemptOutcome.CellAlreadyScratched => Results.Conflict(result),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
})
    .WithName("ScratchDefaultGameCell")
    .WithSummary("Attempt to scratch a cell in the default game.")
    .WithDescription("Accepts a self-declared participant identifier and a cell index. The API enforces one scratch per participant and one scratch per cell, with accepted and rejected attempts recorded for auditability.");
app.MapGet("/api/games/default-game/audit-attempts", async (int? take, ScratchAttemptReadService readService, CancellationToken cancellationToken) =>
{
    var log = await readService.GetDefaultGameAuditLogAsync(take ?? 50, cancellationToken);
    return Results.Ok(log);
})
    .WithName("GetDefaultGameAuditAttempts")
    .WithSummary("List recent audit log entries for the bootstrap game.")
    .WithDescription("Returns the most recent accepted and rejected scratch attempts recorded for the default game.");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
