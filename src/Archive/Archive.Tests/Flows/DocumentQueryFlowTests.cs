using System.Net;
using Archive.Core.Documents;
using Archive.Tests.Infrastructure;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Archive.Tests.Flows;

public class DocumentQueryFlowTests : FlowTestBase
{
    private readonly List<Document> invoices;

    public DocumentQueryFlowTests()
    {
        invoices = [.. Enumerable.Range(0, 12).Select(i => NewDocument("Invoices", new DateTime(2026, 10, 1 + i / 4, i % 4 * 6, 0, 0, DateTimeKind.Utc)))];
        Factory.Repository.Seed([.. invoices, NewDocument("Contracts", new DateTime(2026, 10, 2, 6, 0, 0, DateTimeKind.Utc))]);
    }

    [Theory]
    [Trait("Rule", "R14")]
    [InlineData("2026-10-02", "2026-10-02", 4, 7)]
    [InlineData("2026-10-01T18:00:00Z", "2026-10-02", 3, 7)]
    [InlineData("2026-10-02T06:00:00Z", "2026-10-02T18:00:00Z", 5, 7)]
    [InlineData("2026-10-02T06:00:00Z", "2026-10-02T06:00:00Z", 5, 5)]
    public async Task From_and_to_are_both_inclusive(string from, string to, int first, int last)
    {
        var page = await GetPageAsync("Invoices", from, to);

        Assert.Equal(invoices[first..(last + 1)].Select(d => d.Id), page.Items.Select(i => i.Id));
        Assert.All(page.Items, i => Assert.Equal("Invoices", i.Cabinet));
    }

    [Fact]
    [Trait("Rule", "R14")]
    public async Task A_date_only_to_ends_at_the_last_second_of_the_day()
    {
        var lastSecond = NewDocument("Invoices", new DateTime(2026, 10, 5, 23, 59, 59, DateTimeKind.Utc));
        Factory.Repository.Seed(lastSecond, NewDocument("Invoices", new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc)));

        var page = await GetPageAsync("Invoices", "2026-10-05", "2026-10-05");

        Assert.Equal([lastSecond.Id], page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Pages_are_capped_and_stable_across_requests()
    {
        var pages = new List<PagedResponse<DocumentListItemResponse>>();
        for (var page = 1; page <= 3; page++)
        {
            pages.Add(await GetPageAsync("invoices", "2026-10-01", "2026-10-04", $"&page={page}&pageSize=50"));
        }

        Assert.All(pages, p => Assert.Equal((5, 12), (p.PageSize, p.TotalCount)));
        Assert.Equal([5, 5, 2], pages.Select(p => p.Items.Count));
        Assert.Equal(invoices.Select(d => d.Id), pages.SelectMany(p => p.Items).Select(i => i.Id));
    }

    [Theory]
    [Trait("Rule", "R14")]
    [InlineData("2026-10-04", "2026-10-03", "'from' must not be later than 'to'.")]
    [InlineData("2026-10-03T12:00:01Z", "2026-10-03T12:00:00Z", "'from' must not be later than 'to'.")]
    [InlineData("02-10-2026", "2026-10-03", "'from' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).")]
    [InlineData("2026-10-02", "2026-10-03T00:00:00", "'to' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).")]
    [InlineData("2026-10-02", "2026-10-03T02:00:00+02:00", "'to' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).")]
    public async Task Bad_ranges_are_rejected(string from, string to, string message)
    {
        await ProblemAssert.IsProblemAsync(await Client.GetAsync($"cabinets/Invoices/documents?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}"),
            HttpStatusCode.BadRequest, message);
    }

    [Fact]
    public async Task An_unknown_cabinet_is_not_found()
    {
        await ProblemAssert.IsProblemAsync(await Client.GetAsync("cabinets/Nope/documents?from=2026-10-01&to=2026-10-02"),
            HttpStatusCode.NotFound, "Cabinet 'Nope' was not found.");
    }

    private async Task<PagedResponse<DocumentListItemResponse>> GetPageAsync(string cabinet, string from, string to, string extra = "") =>
        await (await Client.GetAsync($"cabinets/{cabinet}/documents?from={from}&to={to}{extra}")).ReadJsonAsync<PagedResponse<DocumentListItemResponse>>();

    private static Document NewDocument(string cabinet, DateTime storedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Cabinet = cabinet,
        Title = $"doc {storedAt:O}",
        StoredAt = storedAt,
        ModifiedAt = storedAt,
        IndexValuesJson = "{}"
    };
}