using Export.Core.Documents;
using Export.Core.Exports;

namespace Export.Tests.Rules;

public class ZipLayoutTests
{
    private readonly ZipNameAllocator rootNames = new();

    [Fact]
    [Trait("Rule", "X2")]
    public void A_document_with_one_part_becomes_a_file_named_after_the_document()
    {
        var document = NewDocument("invoice-001.txt", "scan-0001.txt");

        var entry = Assert.Single(ZipLayout.Plan(document, rootNames));

        Assert.Equal(new ZipEntryPlan("invoice-001.txt", document.Parts[0].Id), entry);
    }

    [Fact]
    [Trait("Rule", "X3")]
    [Trait("Rule", "X14")]
    public void A_document_with_several_parts_becomes_a_folder_with_the_parts_in_position_order()
    {
        var document = NewDocument("Фактури Q1", "януари.txt", "февруари.txt", "март.txt");
        var inOrder = document.Parts;
        document.Parts = [.. inOrder.OrderByDescending(p => p.Position)];

        var plan = ZipLayout.Plan(document, rootNames);

        Assert.Equal(["Фактури Q1/януари.txt", "Фактури Q1/февруари.txt", "Фактури Q1/март.txt"], plan.Select(e => e.Name));
        Assert.Equal(inOrder.Select(p => (Guid?)p.Id), plan.Select(e => e.PartId));
    }

    [Fact]
    [Trait("Rule", "X4")]
    public void A_document_without_parts_becomes_an_empty_folder()
    {
        var entry = Assert.Single(ZipLayout.Plan(NewDocument("empty-folder"), rootNames));

        Assert.Equal(new ZipEntryPlan("empty-folder/", null), entry);
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void Documents_with_the_same_title_both_keep_their_content()
    {
        var first = NewDocument("invoice-001.txt", "a.txt");
        var second = NewDocument("invoice-001.txt", "b.txt");

        Assert.Equal("invoice-001.txt", Assert.Single(ZipLayout.Plan(first, rootNames)).Name);
        Assert.Equal(new ZipEntryPlan("invoice-001 (2).txt", second.Parts[0].Id), Assert.Single(ZipLayout.Plan(second, rootNames)));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void Parts_with_the_same_name_in_one_folder_are_numbered()
    {
        var plan = ZipLayout.Plan(NewDocument("Scans", "page.txt", "page.txt"), rootNames);

        Assert.Equal(["Scans/page.txt", "Scans/page (2).txt"], plan.Select(e => e.Name));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void Parts_in_different_folders_do_not_number_each_other()
    {
        ZipLayout.Plan(NewDocument("A", "page.txt", "x.txt"), rootNames);

        var plan = ZipLayout.Plan(NewDocument("B", "page.txt", "y.txt"), rootNames);

        Assert.Equal(["B/page.txt", "B/y.txt"], plan.Select(e => e.Name));
    }

    private static Document NewDocument(string title, params string[] fileNames) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Parts = [.. fileNames.Select((name, i) => new Part { Id = Guid.NewGuid(), Position = i, FileName = name })]
    };
}