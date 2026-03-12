using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NloSurpriseCalendar.App.Infrastructure;
using NloSurpriseCalendar.App.Persistence;
using NloSurpriseCalendar.App.Persistence.Models;
using Npgsql;

namespace NloSurpriseCalendar.App.Tests.Infrastructure;

public sealed class TestApplicationFactory(PostgresContainerFixture postgresFixture) : WebApplicationFactory<Program>
{
    private string ConnectionString => postgresFixture.Container.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = ConnectionString,
                ["BootstrapGame:Slug"] = "main",
                ["BootstrapGame:Title"] = "Default Surprise Calendar",
                ["BootstrapGame:Width"] = "100",
                ["BootstrapGame:Height"] = "100",
                ["BootstrapGame:SeedVersion"] = "default-v1",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DatabaseConnectionOptions>();
            services.AddSingleton(new DatabaseConnectionOptions(ConnectionString));
            services.RemoveAll<IOptions<BootstrapGameOptions>>();
            services.AddSingleton<IOptions<BootstrapGameOptions>>(Options.Create(new BootstrapGameOptions
            {
                Slug = "main",
                Title = "Default Surprise Calendar",
                Width = 100,
                Height = 100,
                SeedVersion = "default-v1",
            }));
        });
    }

    public async Task ResetStateAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var resetSchemaCommand = new NpgsqlCommand(
                         """
                         DROP SCHEMA IF EXISTS public CASCADE;
                         CREATE SCHEMA public;
                         """,
                         connection))
        {
            await resetSchemaCommand.ExecuteNonQueryAsync();
        }

        await EnsureInitializedAsync();
    }

    private async Task EnsureInitializedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    public async Task<IReadOnlyList<string>> GetAttemptOutcomeCodesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT outcome_code
            FROM scratch_attempt_events
            ORDER BY id;
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        var outcomes = new List<string>();
        while (await reader.ReadAsync())
        {
            outcomes.Add(reader.GetString(0));
        }

        return outcomes;
    }

    public async Task<int> GetScratchClaimCountAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM scratch_claims;", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
