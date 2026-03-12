using System.Net;
using NloSurpriseCalendar.App.Tests.Infrastructure;

namespace NloSurpriseCalendar.App.Tests;

[Collection(PostgresCollection.Name)]
public sealed class HomePageShellTests(PostgresContainerFixture postgresFixture) : IAsyncLifetime
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
    public async Task HomePage_Ships_GlobalShellCss_ForVisibleLayoutClasses()
    {
        var homeResponse = await _client.GetAsync("/");
        var cssResponse = await _client.GetAsync("/app.css");
        var homeHtml = await homeResponse.Content.ReadAsStringAsync();
        var css = await cssResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cssResponse.StatusCode);
        Assert.Contains("class=\"page\"", homeHtml);
        Assert.Contains("class=\"sidebar\"", homeHtml);
        Assert.Contains("class=\"content\"", homeHtml);
        Assert.Contains("class=\"nav-subtitle\"", homeHtml);
        Assert.Contains("class=\"home-shell\"", homeHtml);

        Assert.Contains(".page {", css);
        Assert.Contains(".sidebar {", css);
        Assert.Contains(".content {", css);
        Assert.Contains(".nav-item .nav-link {", css);
        Assert.Contains(".home-shell", css);
        Assert.Contains(".calendar-grid {", css);
        Assert.Contains(".calendar-cell {", css);
    }
}
