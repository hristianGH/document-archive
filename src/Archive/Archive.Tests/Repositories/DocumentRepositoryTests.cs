using Archive.Core.Documents;
using Archive.Data;
using Archive.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Archive.Tests.Repositories;

public class DocumentRepositoryTests : IDisposable
{
    private static readonly DateTime Day = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ArchiveDbContext dbContext = new(new DbContextOptionsBuilder<ArchiveDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private DocumentRepository Repository => new(dbContext, NullLogger<DocumentRepository>.Instance);

    [Fact]
    public async Task A_stored_document_is_read_back_with_parts_in_order()
    {
        var document = NewDocument("Invoices", Day, partCount: 3);
        document.Parts = [.. document.Parts.Reverse()];

        await Repository.AddAsync(document, CancellationToken.None);
        var stored = await Repository.GetAsync(document.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal((document.Title, document.IndexValuesJson, Day), (stored.Title, stored.IndexValuesJson, stored.StoredAt));
        Assert.Equal([0, 1, 2], stored.Parts.Select(p => p.Position));
        Assert.Equal(["p0.pdf", "p1.pdf", "p2.pdf"], stored.Parts.Select(p => p.FileName));
    }

    [Fact]
    public async Task A_page_is_filtered_ordered_and_counted()
    {
        var invoices = Enumerable.Range(0, 4).Select(i => NewDocument("Invoices", Day.AddHours(i), partCount: i)).ToList();
        foreach (var document in invoices.AsEnumerable().Reverse().Append(NewDocument("Contracts", Day, 1)).Append(NewDocument("Invoices", Day.AddDays(1), 1)))
        {
            await Repository.AddAsync(document, CancellationToken.None);
        }

        var page = await Repository.GetPageAsync(new DocumentQuery
        {
            Cabinet = "Invoices",
            FromUtc = Day,
            ToUtc = Day.AddDays(1).AddSeconds(-1),
            Page = 2,
            PageSize = 3
        }, CancellationToken.None);

        Assert.Equal(4, page.TotalCount);
        var item = Assert.Single(page.Items);
        Assert.Equal((invoices[3].Id, 3), (item.Id, item.PartCount));
    }

    [Fact]
    public async Task A_part_is_only_found_through_its_own_document()
    {
        var first = NewDocument("Invoices", Day, 1);
        var second = NewDocument("Invoices", Day, 1);
        await Repository.AddAsync(first, CancellationToken.None);
        await Repository.AddAsync(second, CancellationToken.None);
        var partId = first.Parts[0].Id;

        Assert.Equal("p0.pdf", (await Repository.GetPartAsync(first.Id, partId, CancellationToken.None))?.FileName);
        Assert.Null(await Repository.GetPartAsync(second.Id, partId, CancellationToken.None));
    }

    public void Dispose()
    {
        dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Document NewDocument(string cabinet, DateTime storedAt, int partCount)
    {
        var id = Guid.CreateVersion7();
        return new Document
        {
            Id = id,
            Cabinet = cabinet,
            Title = $"doc {storedAt:O}",
            StoredAt = storedAt,
            ModifiedAt = storedAt,
            IndexValuesJson = """{"Batch":"B"}""",
            Parts = [.. Enumerable.Range(0, partCount).Select(i => new Part
            {
                Id = Guid.CreateVersion7(),
                Position = i,
                FileName = $"p{i}.pdf",
                Size = 10,
                FilePath = $"{id}/{i}"
            })]
        };
    }
}