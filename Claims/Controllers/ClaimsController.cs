using Claims.Application.Dtos;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    private readonly IClaimService _claimService = claimService;

    /// <summary>Returns all claims.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClaimResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClaimResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await _claimService.GetAllAsync(cancellationToken));
    }

    /// <summary>Returns a single claim by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var claim = await _claimService.GetByIdAsync(id, cancellationToken);
        return claim is null ? NotFound() : Ok(claim);
    }

    /// <summary>Creates a claim against an existing cover.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponse>> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _claimService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = claim.Id }, claim);
    }

    /// <summary>Deletes a claim by id.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var deleted = await _claimService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
