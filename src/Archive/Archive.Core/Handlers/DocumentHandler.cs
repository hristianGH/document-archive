using Archive.Core.Dates;
using Archive.Core.Documents;
using Archive.Core.Exceptions;
using Archive.Core.Logging;
using Archive.Core.Paging;
using Archive.Core.Repositories;
using Archive.Core.Storage;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Handlers;

public class DocumentHandler(
    IDocumentRepository documentRepository,
    ICabinetHandler cabinetHandler,
    IPartFileStore partFileStore,
    PagingSettings pagingSettings,
    ILogger<DocumentHandler> logger) : IDocumentHandler
{
    public async Task<Document> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Document? document = null;
        logger.LogInformation("Start DocumentHandler.GetAsync with params id={Id}", id);
        try
        {
            document = await documentRepository.GetAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Document '{id}' was not found.");
            return document;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentHandler.GetAsync with params id={Id}", id);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentHandler.GetAsync with params id={Id}; parts={PartCount}", id, document?.Parts.Count);
        }
    }

    public async Task<PagedList<DocumentSummary>> GetPageAsync(string cabinet, string from, string to, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedList<DocumentSummary>? result = null;
        logger.LogInformation("Start DocumentHandler.GetPageAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize}",
            cabinet, from, to, page, pageSize);
        try
        {
            var cabinetName = cabinetHandler.Get(cabinet).Name;
            var fromUtc = ParseInstant(from, nameof(from));
            var toUtc = ParseInstant(to, nameof(to));

            if (fromUtc >= toUtc)
            {
                throw new InvalidInputException("'from' must be earlier than 'to'.");
            }

            var size = Math.Min(pageSize, pagingSettings.MaxPageSize);
            if ((long)(page - 1) * size > int.MaxValue)
            {
                throw new InvalidInputException($"'page' {page} is out of range.");
            }

            result = await documentRepository.GetPageAsync(new DocumentQuery
            {
                Cabinet = cabinetName,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = page,
                PageSize = size
            }, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentHandler.GetPageAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize}",
                cabinet, from, to, page, pageSize);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentHandler.GetPageAsync with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize}; items={ItemCount}, total={TotalCount}",
                cabinet, from, to, page, pageSize, result?.Items.Count, result?.TotalCount);
        }
    }

    public async Task<PartContent> GetPartContentAsync(Guid documentId, Guid partId, CancellationToken cancellationToken)
    {
        Part? part = null;
        logger.LogInformation("Start DocumentHandler.GetPartContentAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
        try
        {
            part = await documentRepository.GetPartAsync(documentId, partId, cancellationToken)
                ?? throw new NotFoundException($"Part '{partId}' of document '{documentId}' was not found.");

            var content = partFileStore.OpenRead(part.FilePath)
                ?? throw new PartContentMissingException(partId);

            if (content.Length != part.Size)
            {
                await content.DisposeAsync();
                throw new PartContentMissingException(partId);
            }

            return new PartContent { FileName = part.FileName, Content = content };
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentHandler.GetPartContentAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentHandler.GetPartContentAsync with params documentId={DocumentId}, partId={PartId}; fileName={FileName}, size={Size}",
                documentId, partId, part?.FileName, part?.Size);
        }
    }

    private static DateTime ParseInstant(string value, string name) =>
        UtcInstant.TryParse(value, out var utc)
            ? utc
            : throw new InvalidInputException($"'{name}' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).");
}
