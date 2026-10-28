using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Archive.Core.Documents;
using Archive.Core.Paging;
using Archive.Core.Repositories;

namespace Archive.Tests.Infrastructure;

public class FakeDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> documents = new();

    public int AddCount { get; private set; }

    public int SetIndexValueCount { get; private set; }

    // 1-based number of the AddAsync call that throws, to fake a database fault part way through an upload.
    public int? FailOnAdd { get; set; }

    public IReadOnlyList<Document> Documents => [.. documents.Values.Select(Copy)];

    public void Seed(params Document[] seed)
    {
        foreach (var document in seed)
        {
            documents[document.Id] = Copy(document);
        }
    }

    public Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(documents.TryGetValue(id, out var document) ? Copy(document) : null);

    public Task<PagedList<DocumentSummary>> GetPageAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        var matches = documents.Values
            .Where(d => d.Cabinet == query.Cabinet && d.StoredAt >= query.FromUtc && d.StoredAt < query.ToUtc)
            .OrderBy(d => d.StoredAt)
            .ThenBy(d => d.Id)
            .ToList();

        return Task.FromResult(new PagedList<DocumentSummary>
        {
            Items = [.. matches
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(d => new DocumentSummary
                {
                    Id = d.Id,
                    Cabinet = d.Cabinet,
                    Title = d.Title,
                    StoredAt = d.StoredAt,
                    ModifiedAt = d.ModifiedAt,
                    IndexValuesJson = d.IndexValuesJson,
                    PartCount = d.Parts.Count
                })],
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = matches.Count
        });
    }

    public Task<Part?> GetPartAsync(Guid documentId, Guid partId, CancellationToken cancellationToken) =>
        Task.FromResult(documents.TryGetValue(documentId, out var document)
            ? document.Parts.SingleOrDefault(p => p.Id == partId)
            : null);

    public Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        AddCount++;
        if (AddCount == FailOnAdd)
        {
            throw new InvalidOperationException("Simulated database fault.");
        }

        documents[document.Id] = Copy(document);
        return Task.CompletedTask;
    }

    public Task<bool> SetIndexValueAsync(Guid id, string fieldName, string valueJson, DateTime modifiedAt, CancellationToken cancellationToken)
    {
        SetIndexValueCount++;
        if (!documents.TryGetValue(id, out var document))
        {
            return Task.FromResult(false);
        }

        var indexValues = JsonNode.Parse(document.IndexValuesJson)!.AsObject();
        indexValues[fieldName] = JsonNode.Parse(valueJson);
        document.IndexValuesJson = indexValues.ToJsonString();
        document.ModifiedAt = modifiedAt;
        return Task.FromResult(true);
    }

    private static Document Copy(Document document) => new()
    {
        Id = document.Id,
        Cabinet = document.Cabinet,
        Title = document.Title,
        StoredAt = document.StoredAt,
        ModifiedAt = document.ModifiedAt,
        IndexValuesJson = document.IndexValuesJson,
        Parts = [.. document.Parts.OrderBy(p => p.Position)]
    };
}