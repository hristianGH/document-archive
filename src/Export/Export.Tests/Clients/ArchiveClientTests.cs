using System.Net;
using Export.Core.Cabinets;
using Export.Core.Clients;
using Export.Core.Exceptions;
using Export.Data.Clients;
using Export.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Polly.Timeout;

namespace Export.Tests.Clients;

// ArchiveClient over the fake Archive without the retry pipeline: how each Archive answer is mapped.
public class ArchiveClientTests : IDisposable
{
    private readonly FakeArchive archive = new();

    private readonly FakeTimeProvider clock = new(ExportApiFactory.Now);

    private readonly HttpClient httpClient;

    private readonly ArchiveClient client;

    public ArchiveClientTests()
    {
        httpClient = new HttpClient(archive) { BaseAddress = new Uri("http://archive.test/") };
        client = new ArchiveClient(httpClient, new ArchiveClientSettings { ContentTimeout = ExportApiFactory.ContentTimeout }, clock,
            NullLogger<ArchiveClient>.Instance);
    }

    [Fact]
    public async Task A_cabinet_keeps_each_field_type_limit_and_writability()
    {
        var cabinet = await client.GetCabinetAsync("contracts", CancellationToken.None);

        (string, FieldType, int?, bool)[] expected =
        [
            ("ContractNumber", FieldType.Text, 30, true),
            ("Party", FieldType.Text, 100, true),
            ("SignedOn", FieldType.Date, null, true),
            ("Value", FieldType.Number, null, true),
            ("Notes", FieldType.Text, 255, true),
            ("Category", FieldType.Text, 40, false)
        ];
        Assert.Equal("Contracts", cabinet.Name);
        Assert.Equal(expected, cabinet.Fields.Select(f => (f.Name, f.Type, f.MaxLength, f.Writable)));
    }

    [Fact]
    public async Task A_page_asks_for_the_largest_size_and_keeps_what_the_Archive_states()
    {
        var documents = Enumerable.Range(1, 3).Select(i => archive.Add("Invoices", $"doc-{i}.txt", FakeArchive.Utc(2026, 10, i, 12, 0, 0))).ToList();

        var page = await client.GetDocumentIdsAsync("Invoices", "2026-10-01", "2026-10-28", 2, CancellationToken.None);

        Assert.Equal("GET /cabinets/Invoices/documents?from=2026-10-01&to=2026-10-28&page=2&pageSize=1000", Assert.Single(archive.Requests));
        Assert.Equal((2, 2, 3), (page.Page, page.PageSize, page.TotalCount));
        Assert.Equal([documents[2].Id], page.Items);
    }

    [Fact]
    public async Task A_plus_in_a_date_arrives_as_a_plus_not_a_space()
    {
        string? query = null;
        archive.Override = r =>
        {
            query = r.RequestUri!.Query;
            return null;
        };

        await Assert.ThrowsAsync<ArchiveRejectedException>(() =>
            client.GetDocumentIdsAsync("Invoices", "2026-10-01T00:00:00+02:00", "2026-10-28", 1, CancellationToken.None));

        Assert.Contains("%2B", query);
        Assert.Equal("2026-10-01T00:00:00+02:00", QueryHelpers.ParseQuery(query)["from"].ToString());
    }

    [Fact]
    public async Task A_document_keeps_its_parts_with_id_position_and_file_name()
    {
        var document = archive.Add("Invoices", "Фактури Q1", FakeArchive.Utc(2026, 10, 5, 12, 0, 0), ("януари.txt", "a"), ("февруари.txt", "b"));

        var result = await client.GetDocumentAsync(document.Id, CancellationToken.None);

        Assert.Equal((document.Id, "Фактури Q1"), (result.Id, result.Title));
        Assert.Equal(document.Parts.Select(p => (p.Id, p.Position, p.FileName)), result.Parts.Select(p => (p.Id, p.Position, p.FileName)));
    }

    [Fact]
    public async Task Part_content_is_copied_byte_for_byte()
    {
        byte[] bytes = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
        archive.Override = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        using var destination = new MemoryStream();

        await client.CopyPartContentAsync(Guid.NewGuid(), Guid.NewGuid(), destination, CancellationToken.None);

        Assert.Equal(bytes, destination.ToArray());
    }

    [Fact]
    public async Task The_marker_is_put_as_the_field_value()
    {
        var document = archive.Add("Invoices", "invoice-001.txt", FakeArchive.Utc(2026, 10, 1, 12, 0, 0));

        await client.SetIndexValueAsync(document.Id, "Batch", "Exported on 2026-10-28", CancellationToken.None);

        Assert.Equal($"PUT /documents/{document.Id}/index-values/Batch", Assert.Single(archive.Requests));
        Assert.Equal([(document.Id, "Batch", "Exported on 2026-10-28")], archive.Marks);
    }

    [Fact]
    public async Task A_rejection_keeps_the_Archive_status_and_reason()
    {
        var ex = await Assert.ThrowsAsync<ArchiveRejectedException>(() => client.GetCabinetAsync("Receipts", CancellationToken.None));

        Assert.Equal((HttpStatusCode.NotFound, "Cabinet 'Receipts' was not found."), (ex.StatusCode, ex.Message));
    }

    [Fact]
    public async Task A_rejection_without_a_reason_names_the_status()
    {
        archive.Override = _ => new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("Busy") };

        var ex = await Assert.ThrowsAsync<ArchiveRejectedException>(() => client.GetCabinetAsync("Invoices", CancellationToken.None));

        Assert.Equal((HttpStatusCode.Conflict, "The Archive answered 409."), (ex.StatusCode, ex.Message));
    }

    [Fact]
    public async Task An_Archive_failure_is_a_bad_gateway()
    {
        archive.Override = _ => FakeArchive.Problem(HttpStatusCode.InternalServerError, "Database down.");

        var ex = await Assert.ThrowsAsync<ArchiveFailedException>(() => client.GetCabinetAsync("Invoices", CancellationToken.None));

        Assert.Equal((HttpStatusCode.BadGateway, "The Archive failed with status 500."), (ex.StatusCode, ex.Message));
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    public async Task An_Archive_that_cannot_be_reached_is_unavailable(Type failure)
    {
        archive.Override = _ => throw (Exception)Activator.CreateInstance(failure)!;

        var ex = await Assert.ThrowsAsync<ArchiveUnavailableException>(() => client.GetCabinetAsync("Invoices", CancellationToken.None));

        Assert.Equal((HttpStatusCode.ServiceUnavailable, "The Archive could not be reached."), (ex.StatusCode, ex.Message));
    }

    [Fact]
    public async Task A_cancelled_caller_gets_a_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        archive.Hold = _ => new TaskCompletionSource().Task;

        var getting = client.GetCabinetAsync("Invoices", cancellation.Token);
        await archive.WaitingAsync();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => getting);
    }

    [Fact]
    public async Task Attempts_that_all_timed_out_are_a_gateway_timeout()
    {
        archive.Override = _ => throw new TimeoutRejectedException(ExportApiFactory.AttemptTimeout);

        var ex = await Assert.ThrowsAsync<ArchiveTimeoutException>(() => client.GetCabinetAsync("Invoices", CancellationToken.None));

        Assert.Equal((HttpStatusCode.GatewayTimeout, "The Archive did not answer within 00:00:10 on any attempt."), (ex.StatusCode, ex.Message));
    }

    [Fact]
    public async Task A_part_that_stalls_times_out_after_the_content_timeout()
    {
        archive.Override = _ => archive.StallingContent("partial");
        using var destination = new MemoryStream();

        var copying = client.CopyPartContentAsync(Guid.NewGuid(), Guid.NewGuid(), destination, CancellationToken.None);
        await archive.WaitingAsync();
        clock.Advance(ExportApiFactory.ContentTimeout);

        var ex = await Assert.ThrowsAsync<ArchiveTimeoutException>(() => copying);
        Assert.Equal((HttpStatusCode.GatewayTimeout, "The Archive did not finish sending a part within 00:01:30."), (ex.StatusCode, ex.Message));
    }

    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}