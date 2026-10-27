using Archive.Core.Dates;
using Archive.Core.Documents;
using Archive.Core.Exceptions;
using Archive.Core.Paging;
using Archive.Core.Repositories;
using Archive.Core.Storage;

namespace Archive.Core.Handlers;

public class DocumentHandler(
    IDocumentRepository documentRepository,
    ICabinetHandler cabinetHandler,
    IPartFileStore partFileStore,
    PagingSettings pagingSettings) : IDocumentHandler
{
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly ICabinetHandler _cabinetHandler = cabinetHandler;
    private readonly IPartFileStore _partFileStore = partFileStore;
    private readonly PagingSettings _pagingSettings = pagingSettings;

    public async Task<Document> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await _documentRepository.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document '{id}' was not found.");

    public async Task<PagedList<DocumentSummary>> GetPageAsync(string cabinet, string from, string to, int page, int pageSize, CancellationToken cancellationToken)
    {
        var cabinetName = _cabinetHandler.Get(cabinet).Name;
        var fromUtc = ParseInstant(from, nameof(from));
        var toUtc = ParseInstant(to, nameof(to));

        if (fromUtc >= toUtc)
        {
            throw new InvalidInputException("'from' must be earlier than 'to'.");
        }

        var size = Math.Min(pageSize, _pagingSettings.MaxPageSize);
        if ((long)(page - 1) * size > int.MaxValue)
        {
            throw new InvalidInputException($"'page' {page} is out of range.");
        }

        return await _documentRepository.GetPageAsync(new DocumentQuery
        {
            Cabinet = cabinetName,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = size
        }, cancellationToken);
    }

    public async Task<PartContent> GetPartContentAsync(Guid documentId, Guid partId, CancellationToken cancellationToken)
    {
        var part = await _documentRepository.GetPartAsync(documentId, partId, cancellationToken)
            ?? throw new NotFoundException($"Part '{partId}' of document '{documentId}' was not found.");

        var content = _partFileStore.OpenRead(part.FilePath)
            ?? throw new PartContentMissingException(partId);

        if (content.Length != part.Size)
        {
            await content.DisposeAsync();
            throw new PartContentMissingException(partId);
        }

        return new PartContent { FileName = part.FileName, Content = content };
    }

    private static DateTime ParseInstant(string value, string name) =>
        UtcInstant.TryParse(value, out var utc)
            ? utc
            : throw new InvalidInputException($"'{name}' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).");
}
