using System.Diagnostics;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Verus.Application.Common.Behaviors;

/// <summary>Logs every use case: start, outcome (success or expected errors) and duration.</summary>
public sealed partial class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
    where TResponse : IErrorOr
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        var name = typeof(TMessage).Name;
        var started = Stopwatch.GetTimestamp();
        LogHandling(logger, name);

        try
        {
            var response = await next(message, cancellationToken);
            LogOutcome(name, response, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            LogFailed(logger, ex, name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }

    private void LogOutcome(string name, TResponse response, double elapsedMs)
    {
        if (!response.IsError)
        {
            LogHandled(logger, name, elapsedMs);
            return;
        }

        LogHandledWithErrors(logger, name, elapsedMs, string.Join(", ", response.Errors?.Select(e => e.Code) ?? []));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {Message}")]
    private static partial void LogHandling(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {Message} in {ElapsedMs:F1} ms")]
    private static partial void LogHandled(ILogger logger, string message, double elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Handled {Message} in {ElapsedMs:F1} ms with errors: {ErrorCodes}"
    )]
    private static partial void LogHandledWithErrors(
        ILogger logger,
        string message,
        double elapsedMs,
        string errorCodes
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception in {Message} after {ElapsedMs:F1} ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string message, double elapsedMs);
}
