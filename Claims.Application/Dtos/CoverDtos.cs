using Claims.Domain;

namespace Claims.Application.Dtos;

public record CreateCoverRequest(DateOnly StartDate, DateOnly EndDate, CoverType Type);

public record CoverResponse(string Id, DateOnly StartDate, DateOnly EndDate, CoverType Type, decimal Premium);
