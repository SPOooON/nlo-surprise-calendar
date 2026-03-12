using NloSurpriseCalendar.App.Persistence;
using NloSurpriseCalendar.App.Persistence.Models;

namespace NloSurpriseCalendar.App.Tests;

public sealed class PrizeAllocationPlannerTests
{
    [Fact]
    public void BuildPlan_Returns_ExpectedPrizeCounts_AndUniqueCells()
    {
        var options = new BootstrapGameOptions
        {
            Slug = "main",
            Width = 100,
            Height = 100,
            SeedVersion = "default-v1",
        };

        var planner = new PrizeAllocationPlanner();

        var plan = planner.BuildPlan(options);

        Assert.Equal(101, plan.Count);
        Assert.Equal(1, plan.Count(allocation => allocation.PrizeType == "jackpot"));
        Assert.Equal(100, plan.Count(allocation => allocation.PrizeType == "consolation"));
        Assert.Equal(plan.Count, plan.Select(allocation => allocation.CellIndex).Distinct().Count());
    }

    [Fact]
    public void BuildPlan_IsDeterministic_ForTheSameConfiguration()
    {
        var options = new BootstrapGameOptions
        {
            Slug = "main",
            Width = 100,
            Height = 100,
            SeedVersion = "default-v1",
        };

        var planner = new PrizeAllocationPlanner();

        var first = planner.BuildPlan(options);
        var second = planner.BuildPlan(options);

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildPlan_Rejects_Configurations_TooSmall_ForRequiredPrizes()
    {
        var options = new BootstrapGameOptions
        {
            Slug = "small",
            Width = 10,
            Height = 10,
            SeedVersion = "default-v1",
        };

        var planner = new PrizeAllocationPlanner();

        var exception = Assert.Throws<InvalidOperationException>(() => planner.BuildPlan(options));

        Assert.Contains("at least 101 cells", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
