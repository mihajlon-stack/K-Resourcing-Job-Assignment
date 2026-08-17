using Claims.Domain;

namespace Claims.Application.Repositories;

public interface IClaimRepository
{
    Task<IEnumerable<Claim>> GetAllAsync(CancellationToken cancellationToken);

    Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task AddAsync(Claim claim, CancellationToken cancellationToken);

    /// <returns>True if a matching claim was found and removed; false otherwise.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
