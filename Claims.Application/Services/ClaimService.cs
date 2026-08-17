using Claims.Application.Auditing;
using Claims.Application.Dtos;
using Claims.Application.Repositories;
using Claims.Domain;
using FluentValidation;

namespace Claims.Application.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IValidator<CreateClaimRequest> _validator;
    private readonly IAuditQueue _auditQueue;
    private readonly TimeProvider _timeProvider;

    public ClaimService(
        IClaimRepository claimRepository,
        IValidator<CreateClaimRequest> validator,
        IAuditQueue auditQueue,
        TimeProvider timeProvider)
    {
        _claimRepository = claimRepository;
        _validator = validator;
        _auditQueue = auditQueue;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<ClaimResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var claims = await _claimRepository.GetAllAsync(cancellationToken);
        return claims.Select(ToResponse);
    }

    public async Task<ClaimResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(id, cancellationToken);
        return claim is null ? null : ToResponse(claim);
    }

    public async Task<ClaimResponse> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var claim = new Claim
        {
            Id = Guid.NewGuid().ToString(),
            CoverId = request.CoverId,
            Created = request.Created,
            Name = request.Name,
            Type = request.Type,
            DamageCost = request.DamageCost
        };

        await _claimRepository.AddAsync(claim, cancellationToken);

        _auditQueue.Enqueue(new AuditEntry(AuditedEntityType.Claim, claim.Id, "POST", _timeProvider.GetUtcNow()));

        return ToResponse(claim);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var deleted = await _claimRepository.DeleteAsync(id, cancellationToken);

        if (deleted)
        {
            _auditQueue.Enqueue(new AuditEntry(AuditedEntityType.Claim, id, "DELETE", _timeProvider.GetUtcNow()));
        }

        return deleted;
    }

    private static ClaimResponse ToResponse(Claim claim) =>
        new(claim.Id, claim.CoverId, claim.Created, claim.Name, claim.Type, claim.DamageCost);
}
