using Claims.Application.Auditing;
using Claims.Application.Dtos;
using Claims.Application.Repositories;
using Claims.Application.Services;
using Claims.Domain;
using Claims.Tests.TestSupport;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Claims.Tests.Application.Services;

public class ClaimServiceTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IAuditQueue _auditQueue = Substitute.For<IAuditQueue>();
    private readonly FakeTimeProvider _timeProvider = new();

    private ClaimService CreateService(IValidator<CreateClaimRequest>? validator = null) =>
        new(_claimRepository, validator ?? new FakeValidator<CreateClaimRequest>(new ValidationResult()), _auditQueue, _timeProvider);

    [Fact]
    public async Task CreateAsync_ValidRequest_AssignsIdAndEnqueuesAudit()
    {
        var request = new CreateClaimRequest("cover-1", new DateOnly(2026, 1, 5), "Storm", ClaimType.BadWeather, 500m);

        var result = await CreateService().CreateAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        await _claimRepository.Received(1).AddAsync(Arg.Is<Claim>(c => c.Id == result.Id), Arg.Any<CancellationToken>());
        _auditQueue.Received(1).Enqueue(Arg.Is<AuditEntry>(e =>
            e.EntityType == AuditedEntityType.Claim && e.EntityId == result.Id && e.HttpRequestType == "POST"));
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsAndDoesNotPersistOrAudit()
    {
        var invalidValidator = new FakeValidator<CreateClaimRequest>(
            new ValidationResult(new[] { new ValidationFailure("DamageCost", "too high") }));
        var request = new CreateClaimRequest("cover-1", new DateOnly(2026, 1, 5), "Storm", ClaimType.BadWeather, 999_999m);

        await Assert.ThrowsAsync<ValidationException>(() => CreateService(invalidValidator).CreateAsync(request, CancellationToken.None));

        await _claimRepository.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
        _auditQueue.DidNotReceive().Enqueue(Arg.Any<AuditEntry>());
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        _claimRepository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((Claim?)null);

        var result = await CreateService().GetByIdAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_EnqueuesAuditAndReturnsTrue()
    {
        _claimRepository.DeleteAsync("claim-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateService().DeleteAsync("claim-1", CancellationToken.None);

        Assert.True(result);
        _auditQueue.Received(1).Enqueue(Arg.Is<AuditEntry>(e =>
            e.EntityType == AuditedEntityType.Claim && e.EntityId == "claim-1" && e.HttpRequestType == "DELETE"));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalseAndDoesNotAudit()
    {
        _claimRepository.DeleteAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateService().DeleteAsync("missing", CancellationToken.None);

        Assert.False(result);
        _auditQueue.DidNotReceive().Enqueue(Arg.Any<AuditEntry>());
    }
}
