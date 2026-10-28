using System.Text.Json;
using Archive.Api.Cabinets;
using Archive.Core.Cabinets;
using Archive.Core.Exceptions;
using Archive.Core.Validation;
using Archive.Tests.Infrastructure;

namespace Archive.Tests.Rules;

public class IndexFieldValidatorTests
{
    private static readonly Cabinet Invoices = CabinetFileLoader.Load(TestFiles.CabinetsJson).Find("Invoices")!;

    [Fact]
    [Trait("Rule", "R1")]
    public void A_writable_field_is_found_ignoring_case()
    {
        Assert.Equal("Status", IndexFieldValidator.GetWritableField(Invoices, "status").Name);
    }

    [Theory]
    [Trait("Rule", "R1")]
    [InlineData("ApprovedBy", "Field 'ApprovedBy' is not writable.")]
    [InlineData("Nope", "Field 'Nope' does not exist in cabinet 'Invoices'.")]
    public void Read_only_and_unknown_fields_are_rejected(string field, string message)
    {
        var ex = Assert.Throws<InvalidInputException>(() => IndexFieldValidator.GetWritableField(Invoices, field));
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    [Trait("Rule", "R1")]
    public void Upload_field_must_be_text()
    {
        IndexFieldValidator.EnsureText(Field("Batch"));
        var ex = Assert.Throws<InvalidInputException>(() => IndexFieldValidator.EnsureText(Field("Amount")));
        Assert.Equal("Field 'Amount' is not a text field.", ex.Message);
    }

    [Theory]
    [Trait("Rule", "R13")]
    [InlineData("1234567890123")]
    [InlineData("12345678901\U0001F600")]
    public void Text_longer_than_max_length_is_rejected(string value)
    {
        IndexFieldValidator.ValidateText(Field("Status"), "123456789012");
        var ex = Assert.Throws<InvalidInputException>(() => IndexFieldValidator.ValidateText(Field("Status"), value));
        Assert.Contains("longer than the 12 characters", ex.Message);
    }

    [Theory]
    [Trait("Rule", "R13")]
    [InlineData("Status", "\"Paid\"", "Paid")]
    [InlineData("Status", "\"\"", "")]
    [InlineData("InvoiceDate", "\"2026-10-28\"", "2026-10-28")]
    public void Valid_text_and_date_values_are_accepted(string field, string json, string expected)
    {
        Assert.Equal(expected, IndexFieldValidator.ParseValue(Field(field), Json(json)));
    }

    [Fact]
    [Trait("Rule", "R13")]
    public void A_valid_number_is_accepted()
    {
        Assert.Equal(1234.50m, IndexFieldValidator.ParseValue(Field("Amount"), Json("1234.50")));
    }

    [Theory]
    [Trait("Rule", "R13")]
    [InlineData("Status", "12", "needs a string")]
    [InlineData("Status", "\"1234567890123\"", "longer than the 12 characters")]
    [InlineData("Amount", "\"12\"", "needs a number")]
    [InlineData("Amount", "1e400", "out of range")]
    [InlineData("InvoiceDate", "\"2026-02-30\"", "needs a real date in the format yyyy-MM-dd")]
    [InlineData("InvoiceDate", "\"28.10.2026\"", "needs a real date in the format yyyy-MM-dd")]
    [InlineData("InvoiceDate", "20261028", "needs a real date in the format yyyy-MM-dd")]
    [InlineData("Batch", "null", "A value is required")]
    public void Values_of_the_wrong_type_or_out_of_limits_are_rejected(string field, string json, string message)
    {
        var ex = Assert.Throws<InvalidInputException>(() => IndexFieldValidator.ParseValue(Field(field), Json(json)));
        Assert.Contains(message, ex.Message);
    }

    private static IndexField Field(string name) => Invoices.FindField(name)!;

    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);
}