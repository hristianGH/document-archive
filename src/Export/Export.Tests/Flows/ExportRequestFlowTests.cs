using System.Net;
using System.Net.Http.Json;
using Export.Tests.Infrastructure;

namespace Export.Tests.Flows;

public class ExportRequestFlowTests : FlowTestBase
{
    public ExportRequestFlowTests() => Archive.SeedStandard();

    [Theory]
    [Trait("Rule", "X1")]
    [InlineData("cabinet")]
    [InlineData("field")]
    [InlineData("from")]
    [InlineData("to")]
    public async Task A_missing_input_is_rejected(string missing)
    {
        var body = new Dictionary<string, string> { ["cabinet"] = "Invoices", ["field"] = "Batch", ["from"] = From, ["to"] = To };
        body.Remove(missing);

        await ProblemAssert.IsProblemAsync(await Client.PostAsJsonAsync("exports", body), HttpStatusCode.BadRequest);
        AssertNothingExported();
    }

    [Fact]
    [Trait("Rule", "X1")]
    public async Task An_unknown_cabinet_is_not_found()
    {
        await ProblemAssert.IsProblemAsync(await Client.ExportAsync("Receipts", "Batch", From, To),
            HttpStatusCode.NotFound, "Cabinet 'Receipts' was not found.");
        AssertNothingExported();
    }

    [Theory]
    [Trait("Rule", "X13")]
    [InlineData("InvoiceNumber", "The export marker 'Exported on 2026-10-28' is 22 characters long, but 'Invoice number' (InvoiceNumber) holds at most 20. Choose a text field that holds at least 22 characters.")]
    [InlineData("Status", "The export marker 'Exported on 2026-10-28' is 22 characters long, but 'Status' (Status) holds at most 12. Choose a text field that holds at least 22 characters.")]
    [InlineData("Amount", "Field 'Amount' is not a text field.")]
    [InlineData("ApprovedBy", "Field 'ApprovedBy' is not writable.")]
    [InlineData("Nope", "Field 'Nope' does not exist in cabinet 'Invoices'.")]
    public async Task A_field_that_cannot_hold_the_marker_is_rejected_with_the_reason(string field, string message)
    {
        await ProblemAssert.IsProblemAsync(await Client.ExportAsync("Invoices", field, From, To), HttpStatusCode.BadRequest, message);
        AssertNothingExported();
    }

    [Theory]
    [Trait("Rule", "X1")]
    [InlineData("2026-13-01", To, "'from' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).")]
    [InlineData(From, "28-10-2026", "'to' must be yyyy-MM-ddTHH:mm:ssZ or yyyy-MM-dd (UTC).")]
    [InlineData(To, From, "'from' must not be later than 'to'.")]
    public async Task Bad_dates_are_rejected_with_the_Archive_reason(string from, string to, string message)
    {
        await ProblemAssert.IsProblemAsync(await Client.ExportAsync("Invoices", "Batch", from, to), HttpStatusCode.BadRequest, message);
        AssertNothingExported();
    }

    [Fact]
    [Trait("Rule", "X1")]
    public async Task A_range_without_documents_is_not_found()
    {
        await ProblemAssert.IsProblemAsync(await Client.ExportAsync("Invoices", "Batch", "2025-01-01", "2025-01-31"),
            HttpStatusCode.NotFound, "No documents in Invoices in this range.");
        AssertNothingExported();
    }

    private void AssertNothingExported()
    {
        Assert.Equal(0, Archive.CountRequests("GET /documents/"));
        Assert.Empty(Archive.Marks);
    }
}