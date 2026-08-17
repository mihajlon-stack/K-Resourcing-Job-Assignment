using Claims.Domain;
using Xunit;

namespace Claims.Tests.Domain;

public class CoverPeriodTests
{
    [Fact]
    public void TotalDays_IsExclusiveOfEndDate()
    {
        var period = new CoverPeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(30, period.TotalDays);
    }

    [Fact]
    public void Constructor_EndDateBeforeStartDate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CoverPeriod(new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Constructor_EndDateEqualToStartDate_IsAllowed_WithZeroDays()
    {
        var date = new DateOnly(2026, 1, 1);
        var period = new CoverPeriod(date, date);

        Assert.Equal(0, period.TotalDays);
    }
}
