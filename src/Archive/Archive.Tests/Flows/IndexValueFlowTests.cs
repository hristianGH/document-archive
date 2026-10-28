using System.Net;
using System.Text;
using Archive.Core.Documents;
using Archive.Tests.Infrastructure;
using Shared.Models.Documents;

namespace Archive.Tests.Flows;

public class IndexValueFlowTests : FlowTestBase
{
    private static readonly DateTime StoredAt = ArchiveApiFactory.StartTime.UtcDateTime;
    private readonly Guid id = Guid.NewGuid();

    public IndexValueFlowTests()
    {
        Factory.Repository.Seed(NewDocument(id, "Invoices"));
        Factory.Clock.Advance(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Setting_a_text_value_keeps_the_other_fields()
    {
        var document = await (await Client.PutIndexValueAsync(id, "status", "\"Paid\"")).ReadJsonAsync<DocumentResponse>();

        Assert.Equal("Paid", document.IndexValues.GetProperty("Status").GetString());
        Assert.Equal("Batch-01", document.IndexValues.GetProperty("Batch").GetString());
        Assert.Equal(StoredAt, document.StoredAt);
        Assert.Equal(StoredAt.AddHours(1), document.ModifiedAt);
        Assert.Equal(1, Factory.Repository.SetIndexValueCount);
    }

    [Fact]
    public async Task An_empty_string_clears_a_text_field()
    {
        var document = await (await Client.PutIndexValueAsync(id, "Status", "\"\"")).ReadJsonAsync<DocumentResponse>();

        Assert.Equal("", document.IndexValues.GetProperty("Status").GetString());
    }

    [Theory]
    [Trait("Rule", "R13")]
    [InlineData("Amount", "1234.50", "1234.50")]
    [InlineData("InvoiceDate", "\"2026-10-28\"", "\"2026-10-28\"")]
    public async Task Numbers_and_dates_are_stored_in_canonical_form(string field, string valueJson, string storedJson)
    {
        var document = await (await Client.PutIndexValueAsync(id, field, valueJson)).ReadJsonAsync<DocumentResponse>();

        Assert.Equal(storedJson, document.IndexValues.GetProperty(field).GetRawText());
    }

    [Fact]
    public async Task The_same_value_is_not_written_again()
    {
        var document = await (await Client.PutIndexValueAsync(id, "Batch", "\"Batch-01\"")).ReadJsonAsync<DocumentResponse>();

        Assert.Equal(StoredAt, document.ModifiedAt);
        Assert.Equal(0, Factory.Repository.SetIndexValueCount);
    }

    [Theory]
    [Trait("Rule", "R13")]
    [InlineData("ApprovedBy", "\"Me\"", "Field 'ApprovedBy' is not writable.")]
    [InlineData("Nope", "\"x\"", "Field 'Nope' does not exist in cabinet 'Invoices'.")]
    [InlineData("Status", "\"1234567890123\"", "longer than the 12 characters allowed for field 'Status'")]
    [InlineData("Status", "12", "Field 'Status' needs a string.")]
    [InlineData("Amount", "\"12\"", "Field 'Amount' needs a number.")]
    [InlineData("InvoiceDate", "\"2026-02-30\"", "Field 'InvoiceDate' needs a real date in the format yyyy-MM-dd.")]
    [InlineData("Batch", "null", "A value is required for field 'Batch'.")]
    public async Task Invalid_values_are_rejected(string field, string valueJson, string message)
    {
        await ProblemAssert.IsProblemAsync(await Client.PutIndexValueAsync(id, field, valueJson), HttpStatusCode.BadRequest, message);
        Assert.Equal(0, Factory.Repository.SetIndexValueCount);
    }

    [Fact]
    public async Task A_missing_body_is_rejected()
    {
        var response = await Client.PutAsync($"documents/{id}/index-values/Status", new StringContent("", Encoding.UTF8, "application/json"));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(0, Factory.Repository.SetIndexValueCount);
    }

    [Fact]
    public async Task An_unknown_document_is_not_found()
    {
        var unknown = Guid.NewGuid();

        await ProblemAssert.IsProblemAsync(await Client.PutIndexValueAsync(unknown, "Status", "\"Paid\""),
            HttpStatusCode.NotFound, $"Document '{unknown}' was not found.");
    }

    [Fact]
    public async Task A_document_of_a_cabinet_missing_from_the_catalog_is_a_server_error()
    {
        var orphan = Guid.NewGuid();
        Factory.Repository.Seed(NewDocument(orphan, "Retired"));

        await ProblemAssert.IsProblemAsync(await Client.PutIndexValueAsync(orphan, "Status", "\"Paid\""),
            HttpStatusCode.InternalServerError, "Something went wrong on the server.");
        Assert.Equal(0, Factory.Repository.SetIndexValueCount);
    }

    private static Document NewDocument(Guid id, string cabinet) => new()
    {
        Id = id,
        Cabinet = cabinet,
        Title = "invoice.pdf",
        StoredAt = StoredAt,
        ModifiedAt = StoredAt,
        IndexValuesJson = """{"Batch":"Batch-01","Status":"New"}"""
    };
}