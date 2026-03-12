using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NloSurpriseCalendar.App.Components;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence;
using NloSurpriseCalendar.App.Persistence.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

// Add services to the container.
builder.Services.AddRazorComponents();
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
builder.Services.AddSingleton<ScratchService>();

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
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
app.MapGet("/api/bootstrap/default-game", async (GameSummaryReadService readService, CancellationToken cancellationToken) =>
{
    var summary = await readService.GetDefaultGameSummaryAsync(cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});
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
});
app.MapRazorComponents<App>();

app.Run();

public partial class Program;
