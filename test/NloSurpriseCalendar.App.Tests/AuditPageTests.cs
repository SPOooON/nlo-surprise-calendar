using System.Net;
using NloSurpriseCalendar.App.Tests.Infrastructure;

namespace NloSurpriseCalendar.App.Tests;

[Collection(PostgresCollection.Name)]
public sealed class AuditPageTests(PostgresContainerFixture postgresFixture) : IAsyncLifetime
{
    private readonly TestApplicationFactory _factory = new(postgresFixture);
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _client.GetAsync("/health/live");
        await _factory.ResetStateAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AuditPage_RendersReviewerFacingHeading_AndTableColumns()
    {
        var response = await _client.GetAsync("/audit");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Scratch attempt audit log", html);
        Assert.Contains("Refresh log", html);
        Assert.Contains("latest accepted and rejected scratch attempts", html);
    }
}
