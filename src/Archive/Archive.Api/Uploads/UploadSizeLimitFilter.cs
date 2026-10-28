using Archive.Core.Exceptions;
using Archive.Core.Uploads;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Archive.Api.Uploads;

public class UploadSizeLimitFilter(UploadSettings uploadSettings) : IAsyncResourceFilter
{
    private const long MultipartEnvelopeBytes = 64 * 1024;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var limit = uploadSettings.MaxZipBytes + MultipartEnvelopeBytes;
        if (request.ContentLength > limit)
        {
            throw TooLarge(null);
        }

        var bodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = limit;
        }

        if (request.HasFormContentType)
        {
            try
            {
                await request.ReadFormAsync(context.HttpContext.RequestAborted);
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

        await next();
    }

    private PayloadTooLargeException TooLarge(Exception? innerException) =>
        new($"The upload is over the {uploadSettings.MaxZipBytes} byte limit.", innerException);
}
