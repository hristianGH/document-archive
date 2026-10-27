using Archive.Core.Repositories;

namespace Archive.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ArchiveDbContext _dbContext;

    public DocumentRepository(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
