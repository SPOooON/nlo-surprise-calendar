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
        await _factory.ResetStateAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HomePage_IncludesShellMarkup_AndGlobalStyles()
    {
        var homeResponse = await _client.GetAsync("/");
        var homeHtml = await homeResponse.Content.ReadAsStringAsync();

        var cssResponse = await _client.GetAsync("/app.css");
        var css = await cssResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cssResponse.StatusCode);

        Assert.Contains("class=\"page\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("class=\"sidebar\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("class=\"content-frame\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("class=\"nav-subtitle\"", homeHtml, StringComparison.Ordinal);

        Assert.Contains(".page {", css, StringComparison.Ordinal);
        Assert.Contains(".sidebar {", css, StringComparison.Ordinal);
        Assert.Contains(".content-frame {", css, StringComparison.Ordinal);
        Assert.Contains(".nav-item .nav-link {", css, StringComparison.Ordinal);
        Assert.Contains(".home-shell {", css, StringComparison.Ordinal);
    }
}
