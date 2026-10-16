using System.Reflection;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Verus.Domain.Common;

namespace Verus.Application.Common.Behaviors;

/// <summary>
/// Runs every FluentValidation validator for the message. Failures short-circuit the pipeline and are
/// returned as <see cref="ErrorType.Validation"/> errors (never thrown), keyed by camelCase field name.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
    where TResponse : IErrorOr
{
    private static readonly Func<List<Error>, TResponse> FromErrors = CreateErrorFactory();

    private readonly IValidator<TMessage>[] _validators = [.. validators];

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length == 0)
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results.SelectMany(r => r.Errors).Where(f => f is not null).Select(ToError).ToList();

        return errors.Count == 0 ? await next(message, cancellationToken) : FromErrors(errors);
    }

    private static Error ToError(ValidationFailure failure) =>
        Error.Validation(
            code: failure.ErrorCode,
            description: failure.ErrorMessage,
            metadata: ErrorMetadata.ForField(ToCamelCase(failure.PropertyName))
        );

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    // ErrorOr<T> exposes `static ErrorOr<T> From(List<Error>)`; bind it once per closed TResponse.
    private static Func<List<Error>, TResponse> CreateErrorFactory()
    {
        var from =
            typeof(TResponse).GetMethod(
                nameof(ErrorOr<object>.From),
                BindingFlags.Public | BindingFlags.Static,
                [typeof(List<Error>)]
            ) ?? throw new InvalidOperationException($"{typeof(TResponse)} has no From(List<Error>) factory.");

        return from.CreateDelegate<Func<List<Error>, TResponse>>();
    }
}
