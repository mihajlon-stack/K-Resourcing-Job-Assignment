using Claims.Application.Repositories;
using Claims.Domain;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsContext _context;

    public ClaimRepository(ClaimsContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Claim>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Claims.ToListAsync(cancellationToken);
    }

    public async Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await _context.Claims.SingleOrDefaultAsync(claim => claim.Id == id, cancellationToken);
    }

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken)
    {
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var claim = await GetByIdAsync(id, cancellationToken);
        if (claim is null)
        {
            return false;
        }

        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
