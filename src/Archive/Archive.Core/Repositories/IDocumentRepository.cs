using Archive.Core.Documents;
using Archive.Core.Paging;

namespace Archive.Core.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedList<DocumentSummary>> GetPageAsync(DocumentQuery query, CancellationToken cancellationToken);

    Task<Part?> GetPartAsync(Guid documentId, Guid partId, CancellationToken cancellationToken);

    Task AddAsync(Document document, CancellationToken cancellationToken);
}
