using Claims.Application.Auditing;
using Claims.Application.Dtos;
using Claims.Application.Repositories;
using Claims.Domain;
using FluentValidation;

namespace Claims.Application.Services;

public class CoverService : ICoverService
{
    private readonly ICoverRepository _coverRepository;
    private readonly IValidator<CreateCoverRequest> _validator;
    private readonly IAuditQueue _auditQueue;
    private readonly TimeProvider _timeProvider;

    public CoverService(
        ICoverRepository coverRepository,
        IValidator<CreateCoverRequest> validator,
        IAuditQueue auditQueue,
        TimeProvider timeProvider)
    {
        _coverRepository = coverRepository;
        _validator = validator;
        _auditQueue = auditQueue;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<CoverResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var covers = await _coverRepository.GetAllAsync(cancellationToken);
        return covers.Select(ToResponse);
    }

    public async Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var cover = await _coverRepository.GetByIdAsync(id, cancellationToken);
        return cover is null ? null : ToResponse(cover);
    }

    public async Task<CoverResponse> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var cover = new Cover
        {
            Id = Guid.NewGuid().ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = request.Type,
            Premium = ComputePremium(request.StartDate, request.EndDate, request.Type)
        };

        await _coverRepository.AddAsync(cover, cancellationToken);

        _auditQueue.Enqueue(new AuditEntry(AuditedEntityType.Cover, cover.Id, "POST", _timeProvider.GetUtcNow()));

        return ToResponse(cover);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var deleted = await _coverRepository.DeleteAsync(id, cancellationToken);

        if (deleted)
        {
            _auditQueue.Enqueue(new AuditEntry(AuditedEntityType.Cover, id, "DELETE", _timeProvider.GetUtcNow()));
        }

        return deleted;
    }

    public decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        var period = new CoverPeriod(startDate, endDate);
        return PremiumCalculator.Compute(period, coverType);
    }

    private static CoverResponse ToResponse(Cover cover) =>
        new(cover.Id, cover.StartDate, cover.EndDate, cover.Type, cover.Premium);
}
