using Claims.Application.Dtos;
using Claims.Application.Validation;
using Claims.Domain;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.Tests.Application.Validation;

public class CreateCoverRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    // DateOnly.ToDateTime() returns Kind=Unspecified, which the implicit DateTime -> DateTimeOffset
    // conversion treats as local time rather than UTC, silently shifting "now" by the machine's
    // UTC offset. SpecifyKind(..., Utc) makes the conversion exact.
    private static DateTimeOffset AsUtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static CreateCoverRequestValidator CreateValidator()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(AsUtcMidnight(Today));
        return new CreateCoverRequestValidator(timeProvider);
    }

    [Fact]
    public async Task StartDate_EqualToToday_IsValid()
    {
        var request = new CreateCoverRequest(Today, Today.AddDays(10), CoverType.Yacht);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task StartDate_Yesterday_IsInvalid()
    {
        var request = new CreateCoverRequest(Today.AddDays(-1), Today.AddDays(10), CoverType.Yacht);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EndDate_BeforeStartDate_IsInvalid()
    {
        var request = new CreateCoverRequest(Today, Today.AddDays(-1), CoverType.Yacht);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EndDate_ExactlyOneYearOut_IsValid()
    {
        var request = new CreateCoverRequest(Today, Today.AddYears(1), CoverType.Yacht);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EndDate_OneDayPastOneYear_IsInvalid()
    {
        var request = new CreateCoverRequest(Today, Today.AddYears(1).AddDays(1), CoverType.Yacht);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task LeapYearSpan_ExactlyOneYear_IsValid()
    {
        // 1 Jan 2028 -> 31 Dec 2028 spans a leap day (366 days) but is exactly one
        // calendar year, so the AddYears(1) rule must accept it.
        var start = new DateOnly(2028, 1, 1);
        var end = new DateOnly(2028, 12, 31);
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(AsUtcMidnight(start));
        var validator = new CreateCoverRequestValidator(timeProvider);

        var request = new CreateCoverRequest(start, end, CoverType.Yacht);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
