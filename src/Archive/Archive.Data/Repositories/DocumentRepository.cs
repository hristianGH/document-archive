using Archive.Core.Documents;
using Archive.Core.Paging;
using Archive.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Archive.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ArchiveDbContext _dbContext;

    public DocumentRepository(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new Document
            {
                Id = d.Id,
                Cabinet = d.Cabinet,
                Title = d.Title,
                StoredAt = d.StoredAt,
                ModifiedAt = d.ModifiedAt,
                IndexValuesJson = d.IndexValues,
                Parts = d.Parts
                    .OrderBy(p => p.Position)
                    .Select(p => new Part
                    {
                        Id = p.Id,
                        Position = p.Position,
                        FileName = p.FileName,
                        Size = p.Size,
                        FilePath = p.FilePath
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedList<DocumentSummary>> GetPageAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        var documents = _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Cabinet == query.Cabinet && d.StoredAt >= query.FromUtc && d.StoredAt < query.ToUtc);

        var totalCount = await documents.CountAsync(cancellationToken);
        var items = await documents
            .OrderBy(d => d.StoredAt)
            .ThenBy(d => d.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DocumentSummary
            {
                Id = d.Id,
                Cabinet = d.Cabinet,
                Title = d.Title,
                StoredAt = d.StoredAt,
                ModifiedAt = d.ModifiedAt,
                IndexValuesJson = d.IndexValues,
                PartCount = d.Parts.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedList<DocumentSummary>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Part?> GetPartAsync(Guid documentId, Guid partId, CancellationToken cancellationToken) =>
        await _dbContext.Parts
            .AsNoTracking()
            .Where(p => p.DocumentId == documentId && p.Id == partId)
            .Select(p => new Part
            {
                Id = p.Id,
                Position = p.Position,
                FileName = p.FileName,
                Size = p.Size,
                FilePath = p.FilePath
            })
            .SingleOrDefaultAsync(cancellationToken);
}
