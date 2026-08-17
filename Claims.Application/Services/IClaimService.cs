using Claims.Application.Dtos;

namespace Claims.Application.Services;

public interface IClaimService
{
    Task<IEnumerable<ClaimResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClaimResponse?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<ClaimResponse> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken);

    /// <returns>True if a matching claim was found and removed; false otherwise.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
