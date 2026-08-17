using Claims.Application.Dtos;
using FluentValidation;

namespace Claims.Application.Validation;

public class CreateCoverRequestValidator : AbstractValidator<CreateCoverRequest>
{
    public CreateCoverRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("StartDate cannot be in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate cannot precede StartDate.");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(x => x.StartDate.AddYears(1))
            .WithMessage("Total insurance period cannot exceed 1 year.");
    }
}
