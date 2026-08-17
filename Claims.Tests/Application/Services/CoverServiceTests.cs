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

public class CoverServiceTests
{
    private readonly ICoverRepository _coverRepository = Substitute.For<ICoverRepository>();
    private readonly IAuditQueue _auditQueue = Substitute.For<IAuditQueue>();
    private readonly FakeTimeProvider _timeProvider = new();

    private CoverService CreateService(IValidator<CreateCoverRequest>? validator = null) =>
        new(_coverRepository, validator ?? new FakeValidator<CreateCoverRequest>(new ValidationResult()), _auditQueue, _timeProvider);

    [Fact]
    public async Task CreateAsync_ValidRequest_ComputesPremiumAndEnqueuesAuditAfterPersisting()
    {
        var request = new CreateCoverRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CoverType.Yacht);

        var result = await CreateService().CreateAsync(request, CancellationToken.None);

        Assert.Equal(41250.00m, result.Premium); // 30 billable days at full Yacht rate
        Received.InOrder(() =>
        {
            _coverRepository.AddAsync(Arg.Any<Cover>(), Arg.Any<CancellationToken>());
            _auditQueue.Enqueue(Arg.Any<AuditEntry>());
        });
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_NeverEnqueuesAudit()
    {
        _coverRepository.AddAsync(Arg.Any<Cover>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("db down")));
        var request = new CreateCoverRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CoverType.Yacht);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CreateAsync(request, CancellationToken.None));

        _auditQueue.DidNotReceive().Enqueue(Arg.Any<AuditEntry>());
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsAndDoesNotPersistOrAudit()
    {
        var invalidValidator = new FakeValidator<CreateCoverRequest>(
            new ValidationResult(new[] { new ValidationFailure("EndDate", "too long") }));
        var request = new CreateCoverRequest(new DateOnly(2026, 1, 1), new DateOnly(2028, 1, 1), CoverType.Yacht);

        await Assert.ThrowsAsync<ValidationException>(() => CreateService(invalidValidator).CreateAsync(request, CancellationToken.None));

        await _coverRepository.DidNotReceive().AddAsync(Arg.Any<Cover>(), Arg.Any<CancellationToken>());
        _auditQueue.DidNotReceive().Enqueue(Arg.Any<AuditEntry>());
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        _coverRepository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((Cover?)null);

        var result = await CreateService().GetByIdAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalseAndDoesNotAudit()
    {
        _coverRepository.DeleteAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateService().DeleteAsync("missing", CancellationToken.None);

        Assert.False(result);
        _auditQueue.DidNotReceive().Enqueue(Arg.Any<AuditEntry>());
    }

    [Fact]
    public void ComputePremium_DelegatesToDomainCalculator()
    {
        var result = CreateService().ComputePremium(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CoverType.Yacht);

        Assert.Equal(41250.00m, result);
    }
}
