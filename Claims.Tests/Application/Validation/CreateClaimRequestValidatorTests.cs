using Claims.Application.Dtos;
using Claims.Application.Repositories;
using Claims.Application.Validation;
using Claims.Domain;
using NSubstitute;
using Xunit;

namespace Claims.Tests.Application.Validation;

public class CreateClaimRequestValidatorTests
{
    private static readonly Cover ExistingCover = new()
    {
        Id = "cover-1",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 1, 31),
        Type = CoverType.Yacht,
        Premium = 0m
    };

    private static ICoverRepository CreateRepository()
    {
        var repository = Substitute.For<ICoverRepository>();
        repository.GetByIdAsync(ExistingCover.Id, Arg.Any<CancellationToken>())
            .Returns(ExistingCover);
        repository.GetByIdAsync(Arg.Is<string>(id => id != ExistingCover.Id), Arg.Any<CancellationToken>())
            .Returns((Cover?)null);
        return repository;
    }

    private static CreateClaimRequest RequestWithCreated(DateOnly created) =>
        new(ExistingCover.Id, created, "Storm damage", ClaimType.BadWeather, 1000m);

    [Fact]
    public async Task DamageCost_AboveLimit_IsInvalid()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());
        var request = new CreateClaimRequest(ExistingCover.Id, ExistingCover.StartDate, "x", ClaimType.Fire, 100_001m);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Created_EqualToStartDate_IsValid()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());

        var result = await validator.ValidateAsync(RequestWithCreated(ExistingCover.StartDate));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Created_EqualToEndDate_IsValid()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());

        var result = await validator.ValidateAsync(RequestWithCreated(ExistingCover.EndDate));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Created_OneDayAfterEndDate_IsInvalid()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());

        var result = await validator.ValidateAsync(RequestWithCreated(ExistingCover.EndDate.AddDays(1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Created_OneDayBeforeStartDate_IsInvalid()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());

        var result = await validator.ValidateAsync(RequestWithCreated(ExistingCover.StartDate.AddDays(-1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UnknownCoverId_YieldsExistenceFailure_WithoutContainmentFailure()
    {
        var validator = new CreateClaimRequestValidator(CreateRepository());
        var request = new CreateClaimRequest("missing-cover", new DateOnly(2026, 1, 15), "x", ClaimType.Fire, 500m);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateClaimRequest.CoverId), result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task ValidRequest_FetchesCoverExactlyOnce()
    {
        var repository = CreateRepository();
        var validator = new CreateClaimRequestValidator(repository);

        await validator.ValidateAsync(RequestWithCreated(ExistingCover.StartDate));

        await repository.Received(1).GetByIdAsync(ExistingCover.Id, Arg.Any<CancellationToken>());
    }
}
