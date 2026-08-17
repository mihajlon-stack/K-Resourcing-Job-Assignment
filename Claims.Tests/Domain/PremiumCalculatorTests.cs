using Claims.Domain;
using Xunit;

namespace Claims.Tests.Domain;

public class PremiumCalculatorTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);

    private static CoverPeriod PeriodOf(int days) => new(Start, Start.AddDays(days));

    [Theory]
    [InlineData(29, CoverType.Yacht, 39875.00)]
    [InlineData(30, CoverType.Yacht, 41250.00)]
    [InlineData(31, CoverType.Yacht, 42556.25)]
    [InlineData(179, CoverType.Yacht, 235881.25)]
    [InlineData(180, CoverType.Yacht, 237187.50)]
    [InlineData(181, CoverType.Yacht, 238452.50)]
    [InlineData(29, CoverType.ContainerShip, 47125.00)]
    [InlineData(30, CoverType.ContainerShip, 48750.00)]
    [InlineData(31, CoverType.ContainerShip, 50342.50)]
    [InlineData(179, CoverType.ContainerShip, 286032.50)]
    [InlineData(180, CoverType.ContainerShip, 287625.00)]
    [InlineData(181, CoverType.ContainerShip, 289201.25)]
    public void Compute_MatchesExpectedPremium_AtBandBoundaries(int days, CoverType coverType, double expected)
    {
        var result = PremiumCalculator.Compute(PeriodOf(days), coverType);

        Assert.Equal((decimal)expected, result);
    }

    [Fact]
    public void Compute_LargeDayCount_Band3ContinuesWithoutCliff()
    {
        // Regression guard: the original 365-day cliff is a removed bug, not a boundary.
        // Band 3 must simply keep accumulating past 365 days.
        var result = PremiumCalculator.Compute(PeriodOf(400), CoverType.Yacht);

        Assert.Equal(515487.50m, result);
    }

    [Theory]
    [InlineData(CoverType.ContainerShip)]
    [InlineData(CoverType.BulkCarrier)]
    public void Compute_UnlistedCoverTypes_FallThroughToDefaultMultiplier(CoverType coverType)
    {
        // Both types have no explicit multiplier case, so both must land on the same
        // default (1.3x) arm rather than one of them being silently unhandled.
        var result = PremiumCalculator.Compute(PeriodOf(10), coverType);

        Assert.Equal(16250.00m, result);
    }

    [Fact]
    public void Compute_PassengerShipAndTanker_UseTheirOwnMultipliers()
    {
        var passengerShip = PremiumCalculator.Compute(PeriodOf(10), CoverType.PassengerShip);
        var tanker = PremiumCalculator.Compute(PeriodOf(10), CoverType.Tanker);

        Assert.Equal(15000.00m, passengerShip); // 1250 * 1.2 * 10
        Assert.Equal(18750.00m, tanker);        // 1250 * 1.5 * 10
    }
}
