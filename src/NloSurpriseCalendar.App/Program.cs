using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NloSurpriseCalendar.App.Components;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence;
using NloSurpriseCalendar.App.Persistence.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddSingleton(new DatabaseConnectionOptions(connectionString));
builder.Services.Configure<BootstrapGameOptions>(builder.Configuration.GetSection(BootstrapGameOptions.SectionName));
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
builder.Services.AddSingleton<PrizeAllocationPlanner>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<GameSummaryReadService>();

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
app.MapRazorComponents<App>();

app.Run();
