using Claims.Domain;

namespace Claims.Application.Dtos;

public record CreateClaimRequest(string CoverId, DateOnly Created, string Name, ClaimType Type, decimal DamageCost);

public record ClaimResponse(string Id, string CoverId, DateOnly Created, string Name, ClaimType Type, decimal DamageCost);
