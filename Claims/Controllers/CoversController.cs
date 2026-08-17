using Claims.Application.Dtos;
using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController(ICoverService coverService) : ControllerBase
{
    private readonly ICoverService _coverService = coverService;

    /// <summary>Computes the premium for a hypothetical cover without persisting it.</summary>
    [HttpPost("compute")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public ActionResult<decimal> ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        return Ok(_coverService.ComputePremium(startDate, endDate, coverType));
    }

    /// <summary>Returns all covers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CoverResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CoverResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await _coverService.GetAllAsync(cancellationToken));
    }

    /// <summary>Returns a single cover by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CoverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var cover = await _coverService.GetByIdAsync(id, cancellationToken);
        return cover is null ? NotFound() : Ok(cover);
    }

    /// <summary>Creates a cover and computes its premium.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CoverResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoverResponse>> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken)
    {
        var cover = await _coverService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = cover.Id }, cover);
    }

    /// <summary>Deletes a cover by id.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var deleted = await _coverService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
