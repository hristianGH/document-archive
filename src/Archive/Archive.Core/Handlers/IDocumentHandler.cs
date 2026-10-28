using System.Text.Json;
using Archive.Core.Documents;
using Archive.Core.Paging;

namespace Archive.Core.Handlers;

public interface IDocumentHandler
{
    Task<Document> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedList<DocumentSummary>> GetPageAsync(string cabinet, string from, string to, int page, int pageSize, CancellationToken cancellationToken);

    Task<PartContent> GetPartContentAsync(Guid documentId, Guid partId, CancellationToken cancellationToken);

    Task<Document> SetIndexValueAsync(Guid id, string field, JsonElement value, CancellationToken cancellationToken);
}
