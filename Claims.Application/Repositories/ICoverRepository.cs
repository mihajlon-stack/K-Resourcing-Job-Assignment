using Claims.Domain;

namespace Claims.Application.Repositories;

public interface ICoverRepository
{
    Task<IEnumerable<Cover>> GetAllAsync(CancellationToken cancellationToken);

    Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task AddAsync(Cover cover, CancellationToken cancellationToken);

    /// <returns>True if a matching cover was found and removed; false otherwise.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
