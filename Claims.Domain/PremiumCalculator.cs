namespace Claims.Domain;

/// <summary>
/// Computes cover premium from a base day rate, a cover-type multiplier, and three
/// progressive discount bands over the billable period.
/// </summary>
public static class PremiumCalculator
{
    private const decimal BaseDayRate = 1250m;
    private const int Band1Days = 30;
    private const int Band2Days = 150;

    public static decimal Compute(CoverPeriod period, CoverType coverType)
    {
        var multiplier = GetMultiplier(coverType);
        var dayRate = BaseDayRate * multiplier;
        var days = period.TotalDays;

        var band1 = Math.Min(days, Band1Days);
        var band2 = Math.Clamp(days - Band1Days, 0, Band2Days);
        var band3 = Math.Max(days - Band1Days - Band2Days, 0);

        var (band2Discount, band3Discount) = coverType == CoverType.Yacht
            ? (0.05m, 0.08m)
            : (0.02m, 0.03m);

        return band1 * dayRate
            + band2 * dayRate * (1 - band2Discount)
            + band3 * dayRate * (1 - band3Discount);
    }

    private static decimal GetMultiplier(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => 1.1m,
        CoverType.PassengerShip => 1.2m,
        CoverType.Tanker => 1.5m,
        _ => 1.3m
    };
}
