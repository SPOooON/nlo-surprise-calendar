using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NloSurpriseCalendar.App.Persistence.Models;
using NloSurpriseCalendar.App.Tests.Infrastructure;

namespace NloSurpriseCalendar.App.Tests;

[Collection(PostgresCollection.Name)]
public sealed class ScratchApiTests(PostgresContainerFixture postgresFixture) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

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
    public async Task BootstrapSummary_ReportsExpectedPrizeCounts()
    {
        var summary = await _client.GetFromJsonAsync<GameSummary>("/api/bootstrap/default-game", JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(100, summary.Width);
        Assert.Equal(100, summary.Height);
        Assert.Equal(1, summary.JackpotPrizeCount);
        Assert.Equal(100, summary.ConsolationPrizeCount);
        Assert.Equal(0, summary.ScratchClaimCount);
    }

    [Fact]
    public async Task Scratch_Allows_FirstClaim_AndPersistsSuccessfulAttempt()
    {
        var response = await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("alpha", 42));
        var result = await response.Content.ReadFromJsonAsync<ScratchResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(ScratchAttemptOutcome.Accepted, result.Outcome);
        Assert.Equal("empty", result.ClaimResult);
        Assert.Equal(1, await _factory.GetScratchClaimCountAsync());
        Assert.Equal(new[] { "accepted" }, await _factory.GetAttemptOutcomeCodesAsync());
    }

    [Fact]
    public async Task Scratch_Rejects_SecondClaim_BySameParticipant()
    {
        await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("alpha", 42));

        var response = await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("alpha", 43));
        var result = await response.Content.ReadFromJsonAsync<ScratchResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(ScratchAttemptOutcome.ParticipantAlreadyScratched, result.Outcome);
        Assert.Equal(1, await _factory.GetScratchClaimCountAsync());
        Assert.Equal(
            new[] { "accepted", "participant_already_scratched" },
            await _factory.GetAttemptOutcomeCodesAsync());
    }

    [Fact]
    public async Task Scratch_Rejects_SecondClaim_OnTakenCell()
    {
        await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("alpha", 42));

        var response = await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("beta", 42));
        var result = await response.Content.ReadFromJsonAsync<ScratchResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(ScratchAttemptOutcome.CellAlreadyScratched, result.Outcome);
        Assert.Equal(1, await _factory.GetScratchClaimCountAsync());
        Assert.Equal(
            new[] { "accepted", "cell_already_scratched" },
            await _factory.GetAttemptOutcomeCodesAsync());
    }

    [Fact]
    public async Task Scratch_Rejects_InvalidInput_AndAuditsIt()
    {
        var invalidCellResponse = await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("gamma", -1));
        var invalidCellResult = await invalidCellResponse.Content.ReadFromJsonAsync<ScratchResult>(JsonOptions);

        var invalidParticipantResponse = await _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("   ", 7));
        var invalidParticipantResult = await invalidParticipantResponse.Content.ReadFromJsonAsync<ScratchResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, invalidCellResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidParticipantResponse.StatusCode);
        Assert.NotNull(invalidCellResult);
        Assert.NotNull(invalidParticipantResult);
        Assert.Equal(ScratchAttemptOutcome.InvalidCell, invalidCellResult.Outcome);
        Assert.Equal(ScratchAttemptOutcome.InvalidParticipant, invalidParticipantResult.Outcome);
        Assert.Equal(0, await _factory.GetScratchClaimCountAsync());
        Assert.Equal(
            new[] { "invalid_cell", "invalid_participant" },
            await _factory.GetAttemptOutcomeCodesAsync());
    }

    [Fact]
    public async Task Scratch_SameCellRace_Produces_OneSuccess_AndOneConflict()
    {
        var taskA = _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("race-a", 500));
        var taskB = _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("race-b", 500));

        var responses = await Task.WhenAll(taskA, taskB);

        var statuses = responses.Select(response => response.StatusCode).OrderBy(code => code).ToArray();
        var outcomes = new[]
        {
            await responses[0].Content.ReadFromJsonAsync<ScratchResult>(JsonOptions),
            await responses[1].Content.ReadFromJsonAsync<ScratchResult>(JsonOptions),
        };

        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses);
        Assert.Contains(outcomes, result => result is not null && result.Outcome == ScratchAttemptOutcome.Accepted);
        Assert.Contains(outcomes, result => result is not null && result.Outcome == ScratchAttemptOutcome.CellAlreadyScratched);
        Assert.Equal(1, await _factory.GetScratchClaimCountAsync());
    }

    [Fact]
    public async Task Scratch_SameParticipantRace_Produces_OneSuccess_AndOneConflict()
    {
        var taskA = _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("race-user", 600));
        var taskB = _client.PostAsJsonAsync("/api/games/default-game/scratch", new ScratchRequest("race-user", 601));

        var responses = await Task.WhenAll(taskA, taskB);

        var statuses = responses.Select(response => response.StatusCode).OrderBy(code => code).ToArray();
        var outcomes = new[]
        {
            await responses[0].Content.ReadFromJsonAsync<ScratchResult>(JsonOptions),
            await responses[1].Content.ReadFromJsonAsync<ScratchResult>(JsonOptions),
        };

        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses);
        Assert.Contains(outcomes, result => result is not null && result.Outcome == ScratchAttemptOutcome.Accepted);
        Assert.Contains(outcomes, result => result is not null && result.Outcome == ScratchAttemptOutcome.ParticipantAlreadyScratched);
        Assert.Equal(1, await _factory.GetScratchClaimCountAsync());
    }
}
