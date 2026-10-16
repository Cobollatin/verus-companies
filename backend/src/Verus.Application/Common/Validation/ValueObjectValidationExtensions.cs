using ErrorOr;
using FluentValidation;

namespace Verus.Application.Common.Validation;

public static class ValueObjectValidationExtensions
{
    /// <summary>
    /// Validates a raw input by attempting to build the domain value object, so the domain stays the single
    /// source of truth for its invariants while FluentValidation collects every field's errors in one pass.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, string?> MustBeValueObject<T, TValueObject>(
        this IRuleBuilder<T, string?> ruleBuilder,
        Func<string?, ErrorOr<TValueObject>> factory
    ) =>
        ruleBuilder.Custom(
            (value, context) =>
            {
                var result = factory(value);
                if (!result.IsError)
                {
                    return;
                }

                foreach (var error in result.Errors)
                {
                    context.AddFailure(
                        new FluentValidation.Results.ValidationFailure(context.PropertyPath, error.Description)
                        {
                            ErrorCode = error.Code,
                        }
                    );
                }
            }
        );
}
