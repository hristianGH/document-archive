using System.Net;
using Archive.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Logging;

public static class LoggerExtensions
{
    // Client faults (4xx) and cancelled requests are Warning, server faults Error; the exception is always logged.
    public static void LogMethodError(this ILogger logger, Exception exception, string message, params object?[] args) =>
        logger.Log(GetLevel(exception), exception, message, args);

    private static LogLevel GetLevel(Exception exception) => exception switch
    {
        OperationCanceledException or ArchiveException { StatusCode: < HttpStatusCode.InternalServerError } => LogLevel.Warning,
        _ => LogLevel.Error
    };
}
