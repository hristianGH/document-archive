using System.Net;
using Export.Tests.Infrastructure;

namespace Export.Tests.Flows;

public class ExportFailureFlowTests : FlowTestBase
{
    private readonly IReadOnlyList<FakeDocument> documents;

    public ExportFailureFlowTests() => documents = Archive.SeedStandard();

    [Fact]
    [Trait("Rule", "X15")]
    public async Task An_unreachable_Archive_is_retried_then_reported_as_unavailable()
    {
        Archive.Override = _ => throw new HttpRequestException("Connection refused.");

        await AssertProblemBeforeZipAsync(await ExportStandardAsync(), HttpStatusCode.ServiceUnavailable, "The Archive could not be reached.");
        Assert.Equal(4, Archive.CountRequests("GET /cabinets/Invoices"));
    }

    [Fact]
    [Trait("Rule", "X15")]
    public async Task An_Archive_that_keeps_failing_is_retried_then_reported_as_a_bad_gateway()
    {
        Archive.Override = _ => FakeArchive.Problem(HttpStatusCode.InternalServerError, "Database down.");

        await AssertProblemBeforeZipAsync(await ExportStandardAsync(), HttpStatusCode.BadGateway, "The Archive failed with status 500.");
        Assert.Equal(4, Archive.CountRequests("GET /cabinets/Invoices"));
    }

    [Fact]
    [Trait("Rule", "X15")]
    public async Task An_Archive_that_is_briefly_unavailable_is_retried_and_the_export_succeeds()
    {
        var failures = 1;
        Archive.Override = r => Is(r, HttpMethod.Get, "/cabinets/Invoices/documents") && failures-- > 0
            ? FakeArchive.Problem(HttpStatusCode.ServiceUnavailable, "Starting up.")
            : null;

        Assert.Equal(StandardZip, await (await ExportStandardAsync()).ReadZipAsync());
        Assert.Equal(2, Archive.CountRequests($"GET /cabinets/Invoices/documents?from={From}&to={To}&page=1&"));
    }

    [Fact]
    [Trait("Rule", "X15")]
    public async Task An_Archive_that_never_answers_times_out_on_every_attempt()
    {
        Archive.Hold = r => Is(r, HttpMethod.Get, "/cabinets/Invoices") ? new TaskCompletionSource().Task : null;

        var sending = ExportStandardAsync();
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await Archive.WaitingAsync();
            Factory.Clock.Advance(ExportApiFactory.AttemptTimeout);
        }

        await AssertProblemBeforeZipAsync(await sending, HttpStatusCode.GatewayTimeout, "The Archive did not answer within 00:00:10 on any attempt.");
        Assert.Equal(4, Archive.CountRequests("GET /cabinets/Invoices"));
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_failure_on_the_first_document_is_reported_before_any_zip_bytes()
    {
        Archive.Override = r => Is(r, HttpMethod.Get, DocumentPath(0)) ? FakeArchive.Problem(HttpStatusCode.InternalServerError, "Database down.") : null;

        await AssertProblemBeforeZipAsync(await ExportStandardAsync(), HttpStatusCode.BadGateway, "The Archive failed with status 500.");
        Assert.Equal(4, Archive.CountRequests($"GET {DocumentPath(0)}"));
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_first_part_that_sends_nothing_times_out_before_any_zip_bytes()
    {
        Archive.Override = r => Is(r, HttpMethod.Get, ContentPath(0)) ? Archive.StallingContent("") : null;

        var sending = ExportStandardAsync();
        await Archive.WaitingAsync();
        Factory.Clock.Advance(ExportApiFactory.ContentTimeout);

        await AssertProblemBeforeZipAsync(await sending, HttpStatusCode.GatewayTimeout, "The Archive did not finish sending a part within 00:01:30.");
    }

    [Fact]
    [Trait("Rule", "X15")]
    public async Task A_part_that_is_briefly_unavailable_is_retried_and_written_once()
    {
        var failures = 1;
        Archive.Override = r => Is(r, HttpMethod.Get, ContentPath(0)) && failures-- > 0
            ? FakeArchive.Problem(HttpStatusCode.ServiceUnavailable, "Starting up.")
            : null;

        Assert.Equal(StandardZip, await (await ExportStandardAsync()).ReadZipAsync());
        Assert.Equal(2, Archive.CountRequests($"GET {ContentPath(0)}"));
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_document_failing_after_the_zip_started_breaks_the_download_and_marks_nothing()
    {
        Archive.Override = r => Is(r, HttpMethod.Get, DocumentPath(1)) ? FakeArchive.Problem(HttpStatusCode.InternalServerError, "Database down.") : null;

        await AssertDownloadBreaksAsync(StartStandardExportAsync());
        Assert.Empty(Archive.Marks);
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_part_breaking_halfway_breaks_the_download_and_marks_nothing()
    {
        Archive.Override = r => Is(r, HttpMethod.Get, ContentPath(1)) ? FakeArchive.BreakingContent("Яну") : null;

        await AssertDownloadBreaksAsync(StartStandardExportAsync());
        Assert.Empty(Archive.Marks);
        Assert.Equal(1, Archive.CountRequests($"GET {ContentPath(1)}"));
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_part_stalling_halfway_times_out_breaks_the_download_and_marks_nothing()
    {
        Archive.Override = r => Is(r, HttpMethod.Get, ContentPath(1)) ? Archive.StallingContent("Яну") : null;

        var sending = StartStandardExportAsync();
        await Archive.WaitingAsync();
        Factory.Clock.Advance(ExportApiFactory.ContentTimeout);

        await AssertDownloadBreaksAsync(sending);
        Assert.Empty(Archive.Marks);
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task A_failed_marker_breaks_the_download_and_keeps_only_the_earlier_marks()
    {
        await ExportWithFailingMarkerAsync();

        Assert.Equal([(documents[0].Id, "Batch", Marker)], Archive.Marks);
        Assert.Equal(4, Archive.CountRequests($"PUT {DocumentPath(1)}/"));
        Assert.All(documents.Skip(2), d => Assert.Equal(0, Archive.CountRequests($"PUT /documents/{d.Id}/")));
    }

    [Fact]
    [Trait("Rule", "X12")]
    public async Task Exporting_again_after_a_failed_marker_exports_and_marks_everything()
    {
        await ExportWithFailingMarkerAsync();
        Archive.Override = null;

        Assert.Equal(StandardZip, await (await ExportStandardAsync()).ReadZipAsync());
        Assert.All(documents, d => Assert.Equal(Marker, d.IndexValues["Batch"]));
    }

    private async Task ExportWithFailingMarkerAsync()
    {
        Archive.Override = r => Is(r, HttpMethod.Put, $"{DocumentPath(1)}/index-values/Batch")
            ? FakeArchive.Problem(HttpStatusCode.InternalServerError, "Database down.")
            : null;

        await AssertDownloadBreaksAsync(StartStandardExportAsync());
    }

    private string DocumentPath(int index) => $"/documents/{documents[index].Id}";

    private string ContentPath(int index) => $"{DocumentPath(index)}/parts/{documents[index].Parts[0].Id}/content";

    private static bool Is(HttpRequestMessage request, HttpMethod method, string path) =>
        request.Method == method && request.RequestUri!.AbsolutePath == path;

    private async Task AssertProblemBeforeZipAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        await ProblemAssert.IsProblemAsync(response, status, message);
        Assert.Null(response.Content.Headers.ContentDisposition);
        Assert.Empty(Archive.Marks);
    }

    private static async Task AssertDownloadBreaksAsync(Task<HttpResponseMessage> sending)
    {
        using var response = await sending;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Assert.ThrowsAnyAsync<Exception>(() => response.Content.ReadAsByteArrayAsync());
    }
}