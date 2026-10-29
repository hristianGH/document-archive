using Export.Core.Cabinets;
using Export.Core.Exceptions;
using Export.Core.Exports;
using Export.Data.Clients;
using Export.Tests.Infrastructure;

namespace Export.Tests.Rules;

public class MarkerFieldValidatorTests
{
    private const string Marker = "Exported on 2026-10-28";

    [Theory]
    [Trait("Rule", "X13")]
    [InlineData("Invoices", "Supplier")]
    [InlineData("Invoices", "Batch")]
    [InlineData("Contracts", "ContractNumber")]
    [InlineData("Contracts", "Party")]
    [InlineData("Contracts", "Notes")]
    public void Writable_text_fields_long_enough_for_the_marker_pass(string cabinet, string field)
    {
        Assert.Equal(field, MarkerFieldValidator.GetMarkerField(Load(cabinet), field, Marker).Name);
    }

    [Fact]
    [Trait("Rule", "X13")]
    public void The_field_is_found_ignoring_case_and_returned_with_its_real_name()
    {
        Assert.Equal("Batch", MarkerFieldValidator.GetMarkerField(Load("Invoices"), "bATCH", Marker).Name);
    }

    [Theory]
    [Trait("Rule", "X13")]
    [InlineData("InvoiceNumber", "The export marker 'Exported on 2026-10-28' is 22 characters long, but 'Invoice number' (InvoiceNumber) holds at most 20. Choose a text field that holds at least 22 characters.")]
    [InlineData("Status", "The export marker 'Exported on 2026-10-28' is 22 characters long, but 'Status' (Status) holds at most 12. Choose a text field that holds at least 22 characters.")]
    [InlineData("Amount", "Field 'Amount' is not a text field.")]
    [InlineData("InvoiceDate", "Field 'InvoiceDate' is not a text field.")]
    [InlineData("ApprovedBy", "Field 'ApprovedBy' is not writable.")]
    [InlineData("Nope", "Field 'Nope' does not exist in cabinet 'Invoices'.")]
    public void Fields_that_cannot_hold_the_marker_are_rejected_with_the_reason(string field, string message)
    {
        var ex = Assert.Throws<InvalidInputException>(() => MarkerFieldValidator.GetMarkerField(Load("Invoices"), field, Marker));
        Assert.Equal(message, ex.Message);
    }

    [Theory]
    [Trait("Rule", "X13")]
    [InlineData(22)]
    [InlineData(null)]
    public void A_text_field_fits_when_its_limit_is_the_marker_length_or_it_has_none(int? maxLength)
    {
        var cabinet = new Cabinet { Name = "Test", Fields = [new IndexField { Name = "Note", Type = FieldType.Text, MaxLength = maxLength, Writable = true }] };

        Assert.Equal("Note", MarkerFieldValidator.GetMarkerField(cabinet, "Note", Marker).Name);
    }

    private static Cabinet Load(string name) => TestFiles.Cabinets.Single(c => c.Name == name).ToCabinet();
}