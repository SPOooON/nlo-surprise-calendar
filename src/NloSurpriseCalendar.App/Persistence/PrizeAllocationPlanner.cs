using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using NloSurpriseCalendar.App.Persistence.Models;

namespace NloSurpriseCalendar.App.Persistence;

public sealed class PrizeAllocationPlanner
{
    public IReadOnlyList<PrizeAllocation> BuildPlan(BootstrapGameOptions options)
    {
        var totalCells = checked(options.Width * options.Height);
        if (totalCells < 101)
        {
            throw new InvalidOperationException("Bootstrap game must contain at least 101 cells to fit the required prize allocation.");
        }

        var rankedCells = Enumerable.Range(0, totalCells)
            .Select(cellIndex => new RankedCell(cellIndex, ComputeRank(options, cellIndex)))
            .OrderBy(rankedCell => rankedCell.Score)
            .ThenBy(rankedCell => rankedCell.CellIndex)
            .Take(101)
            .ToArray();

        return rankedCells
            .Select((rankedCell, index) => new PrizeAllocation(
                rankedCell.CellIndex,
                index == 0 ? "jackpot" : "consolation"))
            .ToArray();
    }

    private static ulong ComputeRank(BootstrapGameOptions options, int cellIndex)
    {
        var payload = $"{options.Slug}:{options.Width}:{options.Height}:{options.SeedVersion}:{cellIndex}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, sizeof(ulong)));
    }

    private sealed record RankedCell(int CellIndex, ulong Score);
}
