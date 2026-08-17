using Claims.Application.Dtos;
using Claims.Application.Repositories;
using FluentValidation;

namespace Claims.Application.Validation;

public class CreateClaimRequestValidator : AbstractValidator<CreateClaimRequest>
{
    public CreateClaimRequestValidator(ICoverRepository coverRepository)
    {
        RuleFor(x => x.DamageCost)
            .LessThanOrEqualTo(100_000m)
            .WithMessage("DamageCost cannot exceed 100,000.");

        // Existence and containment share one Cover fetch: a second MustAsync rule would
        // fetch twice, and evaluating containment against a null cover would either throw
        // or silently skip the check. See decision #8.
        RuleFor(x => x).CustomAsync(async (request, context, cancellationToken) =>
        {
            var cover = await coverRepository.GetByIdAsync(request.CoverId, cancellationToken);
            if (cover is null)
            {
                context.AddFailure(nameof(request.CoverId), "Referenced cover does not exist.");
                return;
            }

            if (request.Created < cover.StartDate || request.Created > cover.EndDate)
            {
                context.AddFailure(nameof(request.Created), "Created date must fall within the cover period.");
            }
        });
    }
}
