using Export.Core.Exports;

namespace Export.Tests.Rules;

public class ZipNameAllocatorTests
{
    private readonly ZipNameAllocator names = new();

    [Fact]
    [Trait("Rule", "X5")]
    public void The_first_name_is_kept_and_later_copies_are_numbered_before_the_extension()
    {
        Assert.Equal("invoice-001.txt", names.AllocateFile("invoice-001.txt"));
        Assert.Equal("invoice-001 (2).txt", names.AllocateFile("invoice-001.txt"));
        Assert.Equal("invoice-001 (3).txt", names.AllocateFile("invoice-001.txt"));
    }

    [Theory]
    [Trait("Rule", "X5")]
    [InlineData(".gitignore", ".gitignore (2)")]
    [InlineData("scan", "scan (2)")]
    [InlineData("archive.tar.gz", "archive.tar (2).gz")]
    public void The_extension_starts_at_the_last_dot_unless_it_is_the_first_character(string name, string copy)
    {
        names.AllocateFile(name);

        Assert.Equal(copy, names.AllocateFile(name));
    }

    [Theory]
    [Trait("Rule", "X5")]
    [Trait("Rule", "X14")]
    [InlineData("Фактури Q1", "Фактури Q1 (2)")]
    [InlineData("v1.2 drafts", "v1.2 drafts (2)")]
    public void Folders_are_numbered_at_the_end(string name, string copy)
    {
        names.AllocateFolder(name);

        Assert.Equal(copy, names.AllocateFolder(name));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void Names_that_differ_only_in_case_are_duplicates()
    {
        names.AllocateFile("Invoice.txt");

        Assert.Equal("invoice (2).txt", names.AllocateFile("invoice.txt"));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void Files_and_folders_share_one_set_of_names()
    {
        names.AllocateFile("report");

        Assert.Equal("report (2)", names.AllocateFolder("report"));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void A_number_already_taken_is_skipped()
    {
        names.AllocateFile("invoice (2).txt");
        names.AllocateFile("invoice.txt");

        Assert.Equal("invoice (3).txt", names.AllocateFile("invoice.txt"));
    }

    [Fact]
    [Trait("Rule", "X5")]
    public void A_real_title_matching_a_numbered_copy_gets_its_own_number()
    {
        names.AllocateFile("invoice.txt");
        names.AllocateFile("invoice.txt");

        Assert.Equal("invoice (2) (2).txt", names.AllocateFile("invoice (2).txt"));
    }
}