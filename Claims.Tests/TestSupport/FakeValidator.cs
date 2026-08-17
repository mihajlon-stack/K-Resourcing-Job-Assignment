using FluentValidation;
using FluentValidation.Results;

namespace Claims.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IValidator{T}"/> test double with a fixed outcome.
/// FluentValidation's ValidateAndThrowAsync extension does not call the generic
/// ValidateAsync(T, CancellationToken) overload — it routes through the non-generic
/// IValidator.ValidateAsync(IValidationContext, CancellationToken) member instead, so a plain
/// NSubstitute mock of ValidateAsync(T,...) is never actually invoked and the throw-on-failure
/// behaviour silently never fires. This fake implements that path directly.
/// </summary>
internal class FakeValidator<T> : IValidator<T>
{
    private readonly ValidationResult _result;

    public FakeValidator(ValidationResult result)
    {
        _result = result;
    }

    public ValidationResult Validate(T instance) => _result;

    public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = default) =>
        Task.FromResult(_result);

    ValidationResult IValidator.Validate(IValidationContext context) => ThrowIfInvalid();

    Task<ValidationResult> IValidator.ValidateAsync(IValidationContext context, CancellationToken cancellation) =>
        Task.FromResult(ThrowIfInvalid());

    public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

    public bool CanValidateInstancesOfType(Type type) => typeof(T).IsAssignableFrom(type);

    private ValidationResult ThrowIfInvalid()
    {
        if (!_result.IsValid)
        {
            throw new ValidationException(_result.Errors);
        }

        return _result;
    }
}
