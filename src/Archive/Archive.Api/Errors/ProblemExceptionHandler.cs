using Archive.Api.Uploads;
using Archive.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Errors;

public class ProblemExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProblemExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedErrorMessage = "Something went wrong on the server. Please try again later.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            ArchiveException archiveException => ((int)archiveException.StatusCode, archiveException.Message),
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
        var problemDetails = new ProblemDetails { Status = status, Detail = detail };
        if (exception is UploadFailedException uploadFailed)
        {
            problemDetails.Extensions["report"] = uploadFailed.Report.ToResponse();
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
