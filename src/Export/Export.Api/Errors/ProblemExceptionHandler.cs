using Export.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Export.Api.Errors;

public class ProblemExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProblemExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedErrorMessage = "Something went wrong on the server. Please try again later.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            ExportException exportException => ((int)exportException.StatusCode, exportException.Message),
            BadHttpRequestException badRequest => (badRequest.StatusCode, badRequest.Message),
            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage)
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request {Method} {Path} failed with {Status}", httpContext.Request.Method, httpContext.Request.Path, status);
        }
        else
        {
            logger.LogWarning(exception, "Request {Method} {Path} failed with {Status}", httpContext.Request.Method, httpContext.Request.Path, status);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail }
        });
    }
}