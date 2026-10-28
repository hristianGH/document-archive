using Archive.Core.Exceptions;
using Archive.Core.Logging;
using Archive.Core.Uploads;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Archive.Api.Uploads;

public class UploadSizeLimitFilter(UploadSettings uploadSettings, ILogger<UploadSizeLimitFilter> logger) : IAsyncResourceFilter
{
    private const long MultipartEnvelopeBytes = 64 * 1024;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        try
        {
            await ReadFormWithinLimitAsync(context.HttpContext);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error UploadSizeLimitFilter.OnResourceExecutionAsync with params path={Path}, contentLength={ContentLength}",
                request.Path, request.ContentLength);
            throw;
        }

        await next();
    }

    private async Task ReadFormWithinLimitAsync(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var limit = uploadSettings.MaxZipBytes + MultipartEnvelopeBytes;
        if (request.ContentLength > limit)
        {
            throw TooLarge(null);
        }

        var bodySizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = limit;
        }

        if (request.HasFormContentType)
        {
            try
            {
                await request.ReadFormAsync(httpContext.RequestAborted);
            }
            catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
            {
                throw TooLarge(ex);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidInputException("The upload is not a valid multipart form.", ex);
            }
        }
    }

    private PayloadTooLargeException TooLarge(Exception? innerException) =>
        new($"The upload is over the {uploadSettings.MaxZipBytes} byte limit.", innerException);
}
