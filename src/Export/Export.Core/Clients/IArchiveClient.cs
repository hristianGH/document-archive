using Export.Core.Cabinets;
using Export.Core.Documents;
using Export.Core.Paging;

namespace Export.Core.Clients;

public interface IArchiveClient
{
    Task<Cabinet> GetCabinetAsync(string cabinet, CancellationToken cancellationToken);

    Task<PagedList<Guid>> GetDocumentIdsAsync(string cabinet, string from, string to, int page, CancellationToken cancellationToken);

    Task<Document> GetDocumentAsync(Guid id, CancellationToken cancellationToken);

    Task CopyPartContentAsync(Guid documentId, Guid partId, Stream destination, CancellationToken cancellationToken);

    Task SetIndexValueAsync(Guid documentId, string field, string value, CancellationToken cancellationToken);
}