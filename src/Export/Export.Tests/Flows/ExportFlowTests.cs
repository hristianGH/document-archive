using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Export.Tests.Infrastructure;
using Shared.Models.Exports;

namespace Export.Tests.Flows;

public class ExportFlowTests : FlowTestBase
{
    [Fact]
    [Trait("Rule", "X2")]
    [Trait("Rule", "X3")]
    [Trait("Rule", "X4")]
    [Trait("Rule", "X5")]
    [Trait("Rule", "X14")]
    public async Task Each_document_becomes_a_file_a_folder_or_an_empty_folder_with_its_original_content()
    {
        Archive.SeedStandard();

        var zip = await (await ExportStandardAsync()).ReadZipAsync();

        Assert.Equal(StandardZip, zip);
    }

    [Fact]
    [Trait("Rule", "X1")]
    public async Task The_dates_reach_the_Archive_unchanged_and_both_end_days_are_whole()
    {
        Archive.SeedStandard();

        var zip = await (await ExportStandardAsync()).ReadZipAsync();

        Assert.Contains($"GET /cabinets/Invoices/documents?from={From}&to={To}&page=1&pageSize=1000", Archive.Requests);
        Assert.Equal(("first", "Ünï 名前"), (zip[0].Content, zip[^1].Content));
        Assert.DoesNotContain(zip, e => e.Content is "before" or "after" or "contract");
    }

    [Fact]
    [Trait("Rule", "X6")]
    [Trait("Rule", "X13")]
    public async Task Every_exported_document_is_marked_under_the_real_field_name()
    {
        var documents = Archive.SeedStandard();

        using var response = await Client.ExportAsync("invoices", "batch", From, To);
        await response.ReadZipAsync();

        Assert.Equal("Invoices-export-2026-10-28.zip", FileName(response));
        Assert.Equal(documents.Select(d => (d.Id, "Batch", Marker)), Archive.Marks);
        Assert.All(documents, d => Assert.Equal(Marker, d.IndexValues["Batch"]));
    }

    [Fact]
    [Trait("Rule", "X7")]
    public async Task Marked_documents_are_exported_again_and_their_marker_is_overwritten()
    {
        var documents = Archive.SeedStandard();
        documents[0].IndexValues["Batch"] = "Exported on 2026-10-01";

        var first = await (await ExportStandardAsync()).ReadZipAsync();
        var second = await (await ExportStandardAsync()).ReadZipAsync();

        Assert.Equal(StandardZip, first);
        Assert.Equal(StandardZip, second);
        Assert.Equal(Marker, documents[0].IndexValues["Batch"]);
        Assert.Equal(2 * documents.Count, Archive.Marks.Count);
    }

    [Theory]
    [Trait("Rule", "X6")]
    [Trait("Rule", "X11")]
    [InlineData("2026-10-28T23:59:59Z", "2026-10-28")]
    [InlineData("2026-10-29T00:00:00Z", "2026-10-29")]
    public async Task The_marker_and_the_file_name_use_the_UTC_date_of_the_export(string now, string date)
    {
        Archive.SeedStandard();
        Factory.Clock.SetUtcNow(DateTimeOffset.Parse(now, CultureInfo.InvariantCulture));

        using var response = await ExportStandardAsync();
        await response.ReadZipAsync();

        Assert.Equal($"Invoices-export-{date}.zip", FileName(response));
        Assert.All(Archive.Marks, m => Assert.Equal($"Exported on {date}", m.Value));
    }

    [Fact]
    [Trait("Rule", "X8")]
    public async Task The_zip_is_an_attachment_a_client_on_another_host_can_name()
    {
        Archive.SeedStandard();
        using var request = new HttpRequestMessage(HttpMethod.Post, "exports")
        {
            Content = JsonContent.Create(new ExportRequest { Cabinet = "Invoices", Field = "Batch", From = From, To = To })
        };
        request.Headers.Add("Origin", "http://client.test");

        using var response = await Client.SendAsync(request);

        Assert.Equal(StandardZip, await response.ReadZipAsync());
        Assert.Equal(("attachment", "Invoices-export-2026-10-28.zip"), (response.Content.Headers.ContentDisposition?.DispositionType, FileName(response)));
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("Content-Disposition", Assert.Single(response.Headers.GetValues("Access-Control-Expose-Headers")));
    }

    [Theory]
    [Trait("Rule", "X9")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(1000)]
    public async Task Every_page_is_exported_whatever_the_Archive_page_size(int maxPageSize)
    {
        Archive.MaxPageSize = maxPageSize;
        var documents = Enumerable.Range(1, 12)
            .Select(i => Archive.Add("Invoices", $"doc-{i:00}.txt", FakeArchive.Utc(2026, 10, i, 12, 0, 0), ("scan.txt", $"{i}")))
            .ToList();

        var zip = await (await ExportStandardAsync()).ReadZipAsync();

        Assert.Equal(documents.Select(d => d.Title), zip.Select(e => e.Name));
        var pages = (documents.Count + maxPageSize - 1) / maxPageSize;
        Assert.Equal(Enumerable.Range(1, pages).Select(p => $"GET /cabinets/Invoices/documents?from={From}&to={To}&page={p}&pageSize=1000"),
            Archive.Requests.Where(r => r.StartsWith("GET /cabinets/Invoices/documents", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("Rule", "X5")]
    [Trait("Rule", "X9")]
    public async Task Duplicate_titles_on_different_pages_are_still_numbered()
    {
        Archive.Add("Invoices", "report.txt", FakeArchive.Utc(2026, 10, 1, 12, 0, 0), ("scan.txt", "first report"));
        Archive.Add("Invoices", "a.txt", FakeArchive.Utc(2026, 10, 2, 12, 0, 0), ("scan.txt", "a"));
        Archive.Add("Invoices", "b.txt", FakeArchive.Utc(2026, 10, 3, 12, 0, 0), ("scan.txt", "b"));
        Archive.Add("Invoices", "c.txt", FakeArchive.Utc(2026, 10, 4, 12, 0, 0), ("scan.txt", "c"));
        Archive.Add("Invoices", "report.txt", FakeArchive.Utc(2026, 10, 5, 12, 0, 0), ("scan.txt", "second report"));

        var zip = await (await ExportStandardAsync()).ReadZipAsync();

        ZipItem[] expected = [new("report.txt", "first report"), new("a.txt", "a"), new("b.txt", "b"), new("c.txt", "c"), new("report (2).txt", "second report")];
        Assert.Equal(expected, zip);
    }

    [Fact]
    [Trait("Rule", "X10")]
    public async Task The_zip_is_sent_while_it_is_built()
    {
        var documents = Archive.SeedStandard();
        var release = new TaskCompletionSource();
        Archive.Hold = r => r.Method == HttpMethod.Get && r.RequestUri!.AbsolutePath == $"/documents/{documents[1].Id}" ? release.Task : null;

        var sending = StartStandardExportAsync();
        await Archive.WaitingAsync();
        using var response = await sending.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        release.SetResult();
        Assert.Equal(StandardZip, await response.ReadZipAsync());
    }

    private static string? FileName(HttpResponseMessage response) => response.Content.Headers.ContentDisposition?.FileNameStar;
}