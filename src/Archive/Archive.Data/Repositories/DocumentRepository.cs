using Archive.Core.Documents;
using Archive.Core.Paging;
using Archive.Core.Repositories;
using Archive.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archive.Data.Repositories;

public class DocumentRepository(ArchiveDbContext dbContext, ILogger<DocumentRepository> logger) : IDocumentRepository
{
    public async Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Document? document = null;
        logger.LogInformation("Start DocumentRepository.GetAsync with params id={Id}", id);
        try
        {
            document = await dbContext.Documents
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
            return document;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DocumentRepository.GetAsync with params id={Id}", id);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentRepository.GetAsync with params id={Id}; found={Found}, parts={PartCount}",
                id, document is not null, document?.Parts.Count);
        }
    }

    public async Task<PagedList<DocumentSummary>> GetPageAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        PagedList<DocumentSummary>? result = null;
        logger.LogInformation("Start DocumentRepository.GetPageAsync with params cabinet={Cabinet}, fromUtc={FromUtc:O}, toUtc={ToUtc:O}, page={Page}, pageSize={PageSize}",
            query.Cabinet, query.FromUtc, query.ToUtc, query.Page, query.PageSize);
        try
        {
            var documents = dbContext.Documents
                .AsNoTracking()
                .Where(d => d.Cabinet == query.Cabinet && d.StoredAt >= query.FromUtc && d.StoredAt <= query.ToUtc);

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

            result = new PagedList<DocumentSummary>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DocumentRepository.GetPageAsync with params cabinet={Cabinet}, fromUtc={FromUtc:O}, toUtc={ToUtc:O}, page={Page}, pageSize={PageSize}",
                query.Cabinet, query.FromUtc, query.ToUtc, query.Page, query.PageSize);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentRepository.GetPageAsync with params cabinet={Cabinet}, fromUtc={FromUtc:O}, toUtc={ToUtc:O}, page={Page}, pageSize={PageSize}; items={ItemCount}, total={TotalCount}",
                query.Cabinet, query.FromUtc, query.ToUtc, query.Page, query.PageSize,
                result?.Items.Count, result?.TotalCount);
        }
    }

    public async Task<Part?> GetPartAsync(Guid documentId, Guid partId, CancellationToken cancellationToken)
    {
        Part? part = null;
        logger.LogInformation("Start DocumentRepository.GetPartAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
        try
        {
            part = await dbContext.Parts
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
            return part;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DocumentRepository.GetPartAsync with params documentId={DocumentId}, partId={PartId}", documentId, partId);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentRepository.GetPartAsync with params documentId={DocumentId}, partId={PartId}; found={Found}",
                documentId, partId, part is not null);
        }
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start DocumentRepository.AddAsync with params id={Id}, cabinet={Cabinet}, parts={PartCount}",
            document.Id, document.Cabinet, document.Parts.Count);
        try
        {
            dbContext.Documents.Add(new DocumentEntity
            {
                Id = document.Id,
                Cabinet = document.Cabinet,
                Title = document.Title,
                StoredAt = document.StoredAt,
                ModifiedAt = document.ModifiedAt,
                IndexValues = document.IndexValuesJson,
                Parts = [.. document.Parts.Select(p => new PartEntity
                {
                    Id = p.Id,
                    DocumentId = document.Id,
                    Position = p.Position,
                    FileName = p.FileName,
                    Size = p.Size,
                    FilePath = p.FilePath
                })]
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DocumentRepository.AddAsync with params id={Id}, cabinet={Cabinet}, parts={PartCount}",
                document.Id, document.Cabinet, document.Parts.Count);
            throw;
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
            logger.LogInformation("End DocumentRepository.AddAsync with params id={Id}, cabinet={Cabinet}, parts={PartCount}",
                document.Id, document.Cabinet, document.Parts.Count);
        }
    }

    public async Task<bool> SetIndexValueAsync(Guid id, string fieldName, string valueJson, DateTime modifiedAt, CancellationToken cancellationToken)
    {
        int? rows = null;
        logger.LogInformation("Start DocumentRepository.SetIndexValueAsync with params id={Id}, fieldName={FieldName}, modifiedAt={ModifiedAt:O}", id, fieldName, modifiedAt);
        try
        {
            // One statement merges the key into the jsonb, so concurrent writes to other fields are kept.
            rows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE "Documents"
                SET "IndexValues" = "IndexValues" || jsonb_build_object({fieldName}, {valueJson}::jsonb),
                    "ModifiedAt" = {modifiedAt}
                WHERE "Id" = {id}
                """, cancellationToken);
            return rows > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DocumentRepository.SetIndexValueAsync with params id={Id}, fieldName={FieldName}, modifiedAt={ModifiedAt:O}", id, fieldName, modifiedAt);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentRepository.SetIndexValueAsync with params id={Id}, fieldName={FieldName}, modifiedAt={ModifiedAt:O}; rows={Rows}", id, fieldName, modifiedAt, rows);
        }
    }
}