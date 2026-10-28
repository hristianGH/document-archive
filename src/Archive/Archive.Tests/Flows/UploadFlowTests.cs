using System.Net;
using System.Text;
using System.Text.Json;
using Archive.Api.Uploads;
using Archive.Tests.Infrastructure;
using Shared.Models.Documents;

namespace Archive.Tests.Flows;

public class UploadFlowTests : FlowTestBase
{
    private const string UnicodeTitle = "\u00DCn\u00EF \u540D\u524D.txt";

    [Fact]
    [Trait("Rule", "R2")]
    [Trait("Rule", "R3")]
    [Trait("Rule", "R4")]
    [Trait("Rule", "R5")]
    [Trait("Rule", "R6")]
    [Trait("Rule", "R7")]
    [Trait("Rule", "R8")]
    [Trait("Rule", "R11")]
    public async Task Sample_folder_is_stored_as_documents_with_ordered_parts()
    {
        var report = await (await UploadSampleAsync()).ReadJsonAsync<UploadResponse>();

        Assert.Equal(("Batch-01", "Batch"), (report.FolderName, report.Field));
        Assert.Equal(["invoice.pdf", "Scans", "Empty", UnicodeTitle], report.Stored.Select(s => s.Title));
        Assert.Equal([1, 3, 0, 1], report.Stored.Select(s => s.PartCount));
        var skipped = Assert.Single(report.Skipped);
        Assert.Equal(("Batch-01/Scans/deep/x.pdf", "Nested deeper than one level."), (skipped.Path, skipped.Reason));
        Assert.Empty(report.Failed);

        foreach (var stored in report.Stored)
        {
            var document = await GetDocumentAsync(stored.DocumentId);
            Assert.Equal(("Invoices", stored.Title), (document.Cabinet, document.Title));
            Assert.Equal("Batch-01", document.IndexValues.GetProperty("Batch").GetString());
            Assert.Equal(ArchiveApiFactory.StartTime.UtcDateTime, document.StoredAt);
            Assert.Equal(document.StoredAt, document.ModifiedAt);
        }

        var scans = await GetDocumentAsync(report.Stored[1].DocumentId);
        Assert.Equal(["p1.pdf", "p2.pdf", "p10.pdf"], scans.Parts.Select(p => p.FileName));
        Assert.Equal([0, 1, 2], scans.Parts.Select(p => p.Position));
        foreach (var part in scans.Parts)
        {
            var content = await Client.GetAsync($"documents/{scans.Id}/parts/{part.Id}/content");
            Assert.Equal("application/octet-stream", content.Content.Headers.ContentType?.MediaType);
            Assert.Equal($"%PDF-1.4 page {part.FileName[1..^4]}", await content.Content.ReadAsStringAsync());
        }

        Assert.Equal(5, Factory.StoredFileCount);
    }

    [Fact]
    [Trait("Rule", "R8")]
    public async Task Without_a_root_folder_the_zip_name_is_the_field_value()
    {
        var zip = ZipBuilder.Create(("a.txt", "A"), ("b.txt", "B"));

        var report = await (await Client.UploadAsync("Invoices", "Batch", zip, "Upload 2026.zip")).ReadJsonAsync<UploadResponse>();

        Assert.Equal("Upload 2026", report.FolderName);
        Assert.All(Factory.Repository.Documents, d => Assert.Equal("""{"Batch":"Upload 2026"}""", d.IndexValuesJson));
    }

    [Fact]
    public async Task The_same_folder_uploaded_twice_is_stored_twice()
    {
        var first = await (await UploadSampleAsync()).ReadJsonAsync<UploadResponse>();
        var second = await (await UploadSampleAsync()).ReadJsonAsync<UploadResponse>();

        Assert.Equal(first.Stored.Select(s => s.Title), second.Stored.Select(s => s.Title));
        Assert.Empty(first.Stored.Select(s => s.DocumentId).Intersect(second.Stored.Select(s => s.DocumentId)));
        Assert.Equal(8, Factory.Repository.Documents.Count);
    }

    [Fact]
    [Trait("Rule", "R9")]
    [Trait("Rule", "R12")]
    public async Task A_damaged_file_is_reported_and_the_rest_is_stored()
    {
        var response = await Client.UploadAsync("Invoices", "Batch", File.ReadAllBytes(TestFiles.DamagedZip), "damaged.zip");

        var report = await response.ReadJsonAsync<UploadResponse>();
        Assert.Equal("damaged", report.FolderName);
        Assert.Equal("good.txt", Assert.Single(report.Stored).Title);
        var failed = Assert.Single(report.Failed);
        Assert.Equal(("bad.txt", "File is damaged in the zip."), (failed.Path, failed.Reason));
        Assert.Single(Factory.Repository.Documents);
        Assert.Equal(1, Factory.StoredFileCount);
    }

    [Fact]
    [Trait("Rule", "R9")]
    [Trait("Rule", "R12")]
    public async Task A_server_fault_stops_the_upload_and_reports_what_was_stored()
    {
        Factory.Repository.FailOnAdd = 2;

        var problem = await ProblemAssert.IsProblemAsync(await UploadSampleAsync(), HttpStatusCode.InternalServerError,
            "The upload stopped because of a server error. 1 document(s) were stored; 3 were not stored.");

        var report = ((JsonElement)problem.Extensions["report"]!).Deserialize<UploadResponse>(HttpClientExtensions.JsonOptions)!;
        Assert.Equal("invoice.pdf", Assert.Single(report.Stored).Title);
        Assert.Equal(["Batch-01/Scans", "Batch-01/Empty", $"Batch-01/{UnicodeTitle}"], report.Failed.Select(f => f.Path));
        Assert.All(report.Failed, f => Assert.Equal("Not stored because of a server error.", f.Reason));
        Assert.Single(Factory.Repository.Documents);
        Assert.Equal(1, Factory.StoredFileCount);
    }

    [Fact]
    [Trait("Rule", "R1")]
    [Trait("Rule", "R12")]
    public async Task An_unknown_cabinet_is_not_found()
    {
        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Nope", "Batch", ZipBuilder.Create(("a.txt", "A")), "a.zip"),
            HttpStatusCode.NotFound, "Cabinet 'Nope' was not found.");
        AssertNothingStored(Factory);
    }

    [Theory]
    [Trait("Rule", "R1")]
    [Trait("Rule", "R12")]
    [InlineData("ApprovedBy", "Field 'ApprovedBy' is not writable.")]
    [InlineData("Amount", "Field 'Amount' is not a text field.")]
    [InlineData("Nope", "Field 'Nope' does not exist in cabinet 'Invoices'.")]
    public async Task The_field_must_be_a_writable_text_field(string field, string message)
    {
        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", field, ZipBuilder.Create(("a.txt", "A")), "a.zip"),
            HttpStatusCode.BadRequest, message);
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R8")]
    [Trait("Rule", "R12")]
    [Trait("Rule", "R13")]
    public async Task A_folder_name_longer_than_the_field_allows_is_rejected()
    {
        var zip = ZipBuilder.Create(("FolderNameTooLong/a.txt", "A"));

        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Status", zip, "a.zip"),
            HttpStatusCode.BadRequest, "longer than the 12 characters allowed for field 'Status'");
        AssertNothingStored(Factory);
    }

    [Theory]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    [InlineData("not a zip")]
    [InlineData("PK and more text that is still not a zip archive")]
    public async Task A_file_that_is_not_a_zip_is_rejected(string content)
    {
        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Batch", Encoding.UTF8.GetBytes(content), "a.zip"),
            HttpStatusCode.BadRequest, "The file is not a valid zip.");
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R11")]
    [Trait("Rule", "R12")]
    public async Task A_name_that_is_not_valid_utf8_is_rejected()
    {
        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Batch", File.ReadAllBytes(TestFiles.InvalidNameZip), "invalid-name.zip"),
            HttpStatusCode.BadRequest, "not valid UTF-8");
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    public async Task An_unsafe_path_is_rejected()
    {
        var zip = ZipBuilder.Create(("a.txt", "A"), ("../evil.txt", "B"));

        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Batch", zip, "a.zip"),
            HttpStatusCode.BadRequest, "Entry '../evil.txt' has an unsafe path.");
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    public async Task Too_many_entries_are_rejected()
    {
        using var factory = new ArchiveApiFactory(("Upload:MaxEntries", "2"));
        var zip = ZipBuilder.Create(("a.txt", "A"), ("b.txt", "B"), ("c.txt", "C"));

        await ProblemAssert.IsProblemAsync(await factory.CreateClient().UploadAsync("Invoices", "Batch", zip, "a.zip"),
            HttpStatusCode.BadRequest, "The zip has 3 entries; the limit is 2.");
        AssertNothingStored(factory);
    }

    [Fact]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    public async Task Unpacked_content_over_the_limit_is_rejected()
    {
        using var factory = new ArchiveApiFactory(("Upload:MaxUncompressedBytes", "10"));
        var zip = ZipBuilder.Create(("a.txt", "123456"), ("b.txt", "123456"));

        await ProblemAssert.IsProblemAsync(await factory.CreateClient().UploadAsync("Invoices", "Batch", zip, "a.zip"),
            HttpStatusCode.RequestEntityTooLarge, "over the 10 byte limit");
        AssertNothingStored(factory);
    }

    [Fact]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    public async Task A_body_over_the_limit_is_rejected()
    {
        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Batch", new byte[1_200_000], "big.zip"),
            HttpStatusCode.RequestEntityTooLarge);
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R10")]
    [Trait("Rule", "R12")]
    public async Task A_folder_with_nothing_to_store_is_rejected()
    {
        var zip = ZipBuilder.Create(("Root/.DS_Store", "A"), ("Thumbs.db", "B"), ("__MACOSX/._a", "C"));

        await ProblemAssert.IsProblemAsync(await Client.UploadAsync("Invoices", "Batch", zip, "Empty upload.zip"),
            HttpStatusCode.BadRequest, "Folder 'Empty upload' has nothing to store.");
        AssertNothingStored(Factory);
    }

    [Fact]
    [Trait("Rule", "R9")]
    [Trait("Rule", "R12")]
    public async Task A_folder_where_every_file_is_damaged_stores_nothing_and_says_so()
    {
        var response = await Client.UploadAsync("Invoices", "Batch", File.ReadAllBytes(TestFiles.AllDamagedZip), "all-damaged.zip");

        var problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError,
            "Nothing was stored; see the report for each failure.");
        var report = ((JsonElement)problem.Extensions["report"]!).Deserialize<UploadResponse>(HttpClientExtensions.JsonOptions)!;
        Assert.Empty(report.Stored);
        var failed = Assert.Single(report.Failed);
        Assert.Equal(("bad.txt", "File is damaged in the zip."), (failed.Path, failed.Reason));
        Assert.Empty(Factory.Repository.Documents);
        Assert.Equal(0, Factory.StoredFileCount);
    }

    [Fact]
    [Trait("Rule", "R14")]
    public async Task Stored_times_are_utc_in_whole_seconds()
    {
        Factory.Clock.Advance(TimeSpan.FromMilliseconds(750));

        var report = await (await Client.UploadAsync("Invoices", "Batch", ZipBuilder.Create(("a.txt", "A")), "a.zip")).ReadJsonAsync<UploadResponse>();

        var json = await (await Client.GetAsync($"documents/{report.Stored[0].DocumentId}")).Content.ReadAsStringAsync();
        Assert.Contains("\"storedAt\":\"2026-10-01T09:30:00Z\"", json);
        Assert.Contains("\"modifiedAt\":\"2026-10-01T09:30:00Z\"", json);
    }

    private Task<HttpResponseMessage> UploadSampleAsync() =>
        Client.UploadAsync("Invoices", "batch", File.ReadAllBytes(TestFiles.SampleZip), "sample.zip");

    private async Task<DocumentResponse> GetDocumentAsync(Guid id) =>
        await (await Client.GetAsync($"documents/{id}")).ReadJsonAsync<DocumentResponse>();

    private static void AssertNothingStored(ArchiveApiFactory factory)
    {
        Assert.Equal(0, factory.Repository.AddCount);
        Assert.Equal(0, factory.StoredFileCount);
    }
}