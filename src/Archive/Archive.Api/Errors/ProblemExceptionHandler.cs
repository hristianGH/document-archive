using Archive.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Errors;

public class ProblemExceptionHandler : IExceptionHandler
{
    private const string UnexpectedErrorMessage = "Something went wrong on the server. Please try again later.";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ProblemExceptionHandler> _logger;

    public ProblemExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProblemExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            ArchiveException archiveException => ((int)archiveException.StatusCode, archiveException.Message),
            BadHttpRequestException badRequest => (badRequest.StatusCode, badRequest.Message),
            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail }
        });
    }
}
