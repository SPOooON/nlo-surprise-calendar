using System.Net;
using NloSurpriseCalendar.App.Tests.Infrastructure;

namespace NloSurpriseCalendar.App.Tests;

[Collection(PostgresCollection.Name)]
public sealed class OpenApiTests(PostgresContainerFixture postgresFixture) : IAsyncLifetime
{
    private readonly TestApplicationFactory _factory = new(postgresFixture);
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.ResetStateAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task OpenApiDocument_IsAvailable()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"openapi\"", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/bootstrap/default-game", content, StringComparison.Ordinal);
        Assert.Contains("/api/games/default-game/scratch", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocsUi_IsAvailable()
    {
        var response = await _client.GetAsync("/docs");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Scalar", content, StringComparison.OrdinalIgnoreCase);
    }
}
