using System.Net;
using Archive.Tests.Infrastructure;
using Shared.Models.Cabinets;

namespace Archive.Tests.Flows;

public class CabinetsFlowTests : FlowTestBase
{
    [Fact]
    [Trait("Rule", "R13")]
    public async Task Lists_the_cabinets_with_their_fields()
    {
        var cabinets = await (await Client.GetAsync("cabinets")).ReadJsonAsync<List<CabinetResponse>>();

        Assert.Equal(["Invoices", "Contracts"], cabinets.Select(c => c.Name));
        var invoices = cabinets[0];
        Assert.Equal(
            ["InvoiceNumber", "Supplier", "Amount", "InvoiceDate", "Batch", "Status", "ApprovedBy"],
            invoices.Fields.Select(f => f.Name));

        var status = invoices.Fields.Single(f => f.Name == "Status");
        Assert.Equal((FieldType.Text, 12, true), (status.Type, status.MaxLength, status.Writable));
        var amount = invoices.Fields.Single(f => f.Name == "Amount");
        Assert.Equal((FieldType.Number, (int?)null, true), (amount.Type, amount.MaxLength, amount.Writable));
        Assert.False(invoices.Fields.Single(f => f.Name == "ApprovedBy").Writable);
    }

    [Fact]
    public async Task Gets_one_cabinet_ignoring_case()
    {
        var cabinet = await (await Client.GetAsync("cabinets/contracts")).ReadJsonAsync<CabinetResponse>();

        Assert.Equal("Contracts", cabinet.Name);
        Assert.Equal(FieldType.Date, cabinet.Fields.Single(f => f.Name == "SignedOn").Type);
    }

    [Fact]
    public async Task An_unknown_cabinet_is_not_found()
    {
        await ProblemAssert.IsProblemAsync(await Client.GetAsync("cabinets/Unknown"), HttpStatusCode.NotFound, "Cabinet 'Unknown' was not found.");
    }
}