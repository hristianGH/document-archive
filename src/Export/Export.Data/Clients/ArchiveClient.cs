using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Export.Core.Cabinets;
using Export.Core.Clients;
using Export.Core.Documents;
using Export.Core.Exceptions;
using Export.Core.Paging;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
using Shared.Logging;
using Shared.Models.Cabinets;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Export.Data.Clients;

public class ArchiveClient(HttpClient httpClient, ArchiveClientSettings settings, TimeProvider timeProvider, ILogger<ArchiveClient> logger) : IArchiveClient
{
    // The Archive caps it at its maximum; the response states the size used.
    private const int RequestedPageSize = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<Cabinet> GetCabinetAsync(string cabinet, CancellationToken cancellationToken)
    {
        Cabinet? result = null;
        logger.LogInformation("Start ArchiveClient.GetCabinetAsync with params cabinet={Cabinet}", cabinet);
        try
        {
            var response = await GetJsonAsync<CabinetResponse>($"cabinets/{Uri.EscapeDataString(cabinet)}", cancellationToken);
            result = response.ToCabinet();
            return result;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ArchiveClient.GetCabinetAsync with params cabinet={Cabinet}", cabinet);
            throw;
        }
        finally
        {
            logger.LogInformation("End ArchiveClient.GetCabinetAsync with params cabinet={Cabinet}; fields={FieldCount}", cabinet, result?.Fields.Count);
        }
    }

    public async Task<PagedList<Guid>> GetDocumentIdsAsync(string cabinet, string from, string to, int page, CancellationToken cancellationToken)
    {
        PagedList<Guid>? result = null;
        logger.LogInformation("Start ArchiveClient.GetDocumentIdsAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}",
            cabinet, from, to, page);
        try
        {
            var path = $"cabinets/{Uri.EscapeDataString(cabinet)}/documents?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&page={page}&pageSize={RequestedPageSize}";
            var response = await GetJsonAsync<PagedResponse<DocumentListItemResponse>>(path, cancellationToken);
            result = response.ToIdPage();
            return result;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ArchiveClient.GetDocumentIdsAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}",
                cabinet, from, to, page);
            throw;
        }
        finally
        {
            logger.LogInformation("End ArchiveClient.GetDocumentIdsAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}; items={ItemCount}, pageSize={PageSize}, total={TotalCount}",
                cabinet, from, to, page, result?.Items.Count, result?.PageSize, result?.TotalCount);
        }
    }

    public async Task<Document> GetDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        Document? result = null;
        logger.LogInformation("Start ArchiveClient.GetDocumentAsync with params id={Id}", id);
        try
        {
            var response = await GetJsonAsync<DocumentResponse>($"documents/{id}", cancellationToken);
            result = response.ToDocument();
            return result;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ArchiveClient.GetDocumentAsync with params id={Id}", id);
            throw;
        }
        finally
        {
            logger.LogInformation("End ArchiveClient.GetDocumentAsync with params id={Id}; parts={PartCount}", id, result?.Parts.Count);
        }
    }

    public async Task CopyPartContentAsync(Guid documentId, Guid partId, Stream destination, CancellationToken cancellationToken)
    {
        long? size = null;
        logger.LogInformation("Start ArchiveClient.CopyPartContentAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
        try
        {
            using var response = await SendAsync(HttpMethod.Get, $"documents/{documentId}/parts/{partId}/content", null,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            size = response.Content.Headers.ContentLength;

            // The attempt timeout and HttpClient's own timeout end once the headers arrive; this one caps reading the content.
            using var timeout = new CancellationTokenSource(settings.ContentTimeout, timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            try
            {
                await response.Content.CopyToAsync(destination, linked.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ArchiveTimeoutException($"The Archive did not finish sending a part within {settings.ContentTimeout}.", ex);
            }
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ArchiveClient.CopyPartContentAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
            throw;
        }
        finally
        {
            logger.LogInformation("End ArchiveClient.CopyPartContentAsync with params documentId={DocumentId}, partId={PartId}; size={Size}",
                documentId, partId, size);
        }
    }

    public async Task SetIndexValueAsync(Guid documentId, string field, string value, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start ArchiveClient.SetIndexValueAsync with params documentId={DocumentId}, field={Field}, value={Value}",
            documentId, field, value);
        try
        {
            var content = JsonContent.Create(new SetIndexValueRequest { Value = JsonSerializer.SerializeToElement(value) }, options: JsonOptions);
            using var response = await SendAsync(HttpMethod.Put, $"documents/{documentId}/index-values/{Uri.EscapeDataString(field)}", content,
                HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ArchiveClient.SetIndexValueAsync with params documentId={DocumentId}, field={Field}, value={Value}",
                documentId, field, value);
            throw;
        }
        finally
        {
            logger.LogInformation("End ArchiveClient.SetIndexValueAsync with params documentId={DocumentId}, field={Field}, value={Value}",
                documentId, field, value);
        }
    }

    private async Task<T> GetJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"The Archive sent an empty body for '{path}'.");
    }

    // Retries happen inside httpClient (resilience handler); this maps what is left after them.
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content,
        HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, completionOption, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ArchiveUnavailableException(ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ArchiveUnavailableException(ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new ArchiveTimeoutException($"The Archive did not answer within {ex.Timeout} on any attempt.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            if ((int)response.StatusCode is >= 400 and < 500)
            {
                throw new ArchiveRejectedException(response.StatusCode, await ReadProblemAsync(response, cancellationToken));
            }

            throw new ArchiveFailedException(response.StatusCode);
        }
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<ArchiveProblem>(await response.Content.ReadAsStringAsync(cancellationToken), JsonOptions);
            return problem?.Detail ?? problem?.Title ?? $"The Archive answered {(int)response.StatusCode}.";
        }
        catch (JsonException)
        {
            return $"The Archive answered {(int)response.StatusCode}.";
        }
    }

    private class ArchiveProblem
    {
        public string? Title { get; set; }

        public string? Detail { get; set; }
    }
}