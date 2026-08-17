using Claims.Application.Dtos;
using Claims.Domain;

namespace Claims.Application.Services;

public interface ICoverService
{
    Task<IEnumerable<CoverResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<CoverResponse> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken);

    /// <returns>True if a matching cover was found and removed; false otherwise.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);

    decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType);
}
