using Archive.Core.Repositories;

namespace Archive.Core.Handlers;

public class DocumentHandler : IDocumentHandler
{
    private readonly IDocumentRepository _documentRepository;

    public DocumentHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }
}
