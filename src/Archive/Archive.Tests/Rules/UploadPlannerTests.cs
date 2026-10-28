using Archive.Core.Exceptions;
using Archive.Core.Uploads;

namespace Archive.Tests.Rules;

public class UploadPlannerTests
{
    [Fact]
    [Trait("Rule", "R2")]
    [Trait("Rule", "R3")]
    [Trait("Rule", "R7")]
    public void Each_top_level_entry_becomes_one_document_titled_by_its_name()
    {
        var plan = Plan("upload.zip", "Root/a.txt", "Root/Folder/b.txt", "Root/Folder/c.txt", "Root/z.txt");

        Assert.Equal(["a.txt", "Folder", "z.txt"], plan.Documents.Select(d => d.Title));
        Assert.Equal(["a.txt"], plan.Documents[0].Parts.Select(p => p.FileName));
        Assert.Equal(2, plan.Documents[1].Parts.Count);
    }

    [Fact]
    [Trait("Rule", "R4")]
    public void Folder_parts_are_in_natural_order()
    {
        var plan = Plan("upload.zip", "Root/Scans/p10.pdf", "Root/Scans/p2.pdf", "Root/Scans/P1.pdf");

        var parts = Assert.Single(plan.Documents).Parts;
        Assert.Equal(["P1.pdf", "p2.pdf", "p10.pdf"], parts.Select(p => p.FileName));
        Assert.Equal([0, 1, 2], parts.Select(p => p.Position));
    }

    [Fact]
    [Trait("Rule", "R5")]
    public void Anything_nested_deeper_is_skipped_and_reported()
    {
        var plan = Plan("upload.zip", "Root/Folder/a.txt", "Root/Folder/deep/x.txt", "Root/Folder/sub/");

        Assert.Equal(["a.txt"], Assert.Single(plan.Documents).Parts.Select(p => p.FileName));
        Assert.Equal(["Root/Folder/deep/x.txt", "Root/Folder/sub/"], plan.Skipped.Select(s => s.Path));
        Assert.All(plan.Skipped, s => Assert.Equal("Nested deeper than one level.", s.Reason));
    }

    [Fact]
    [Trait("Rule", "R6")]
    public void An_empty_subfolder_becomes_a_document_with_no_parts()
    {
        var plan = Plan("upload.zip", "Root/Empty/", "Root/a.txt");

        var empty = Assert.Single(plan.Documents, d => d.Title == "Empty");
        Assert.Empty(empty.Parts);
    }

    [Fact]
    [Trait("Rule", "R8")]
    public void Folder_name_is_the_single_root_folder()
    {
        Assert.Equal("Batch-01", Plan("anything.zip", "Batch-01/a.txt", "Batch-01/b/").FolderName);
    }

    [Theory]
    [Trait("Rule", "R8")]
    [InlineData("a.txt", "b.txt")]
    [InlineData("a.txt")]
    [InlineData("One/a.txt", "Two/b.txt")]
    public void Folder_name_is_the_zip_name_when_there_is_no_single_root_folder(params string[] names)
    {
        Assert.Equal("Upload 2026", Plan("Upload 2026.zip", names).FolderName);
    }

    [Theory]
    [Trait("Rule", "R10")]
    [InlineData("../evil.txt")]
    [InlineData("Root/../../evil.txt")]
    [InlineData("Root/./a.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\evil.txt")]
    [InlineData("Root\\..\\evil.txt")]
    public void Unsafe_paths_are_rejected(string name)
    {
        var ex = Assert.Throws<InvalidInputException>(() => Plan("upload.zip", "Root/a.txt", name));
        Assert.Contains("unsafe path", ex.Message);
    }

    [Fact]
    [Trait("Rule", "R10")]
    public void System_files_and_symlinks_are_dropped_silently()
    {
        UploadEntry[] entries =
        [
            new() { Index = 0, FullName = "Root/a.txt", Length = 1 },
            new() { Index = 1, FullName = "Root/.DS_Store", Length = 1 },
            new() { Index = 2, FullName = "Root/Folder/Thumbs.db", Length = 1 },
            new() { Index = 3, FullName = "Root/desktop.ini", Length = 1 },
            new() { Index = 4, FullName = "__MACOSX/Root/._a.txt", Length = 1 },
            new() { Index = 5, FullName = "Root/link.txt", Length = 1, IsSymlink = true }
        ];

        var plan = UploadPlanner.Plan(entries, "upload.zip");

        Assert.Equal("Root", plan.FolderName);
        Assert.Equal(["a.txt"], plan.Documents.Select(d => d.Title));
        Assert.Empty(plan.Skipped);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void Unicode_names_are_kept_exactly()
    {
        const string folder = "\u00DCn\u00EF \u540D\u524D";
        const string file = "\u30D5\u30A1\u30A4\u30EB e\u0301.txt";

        var plan = Plan("upload.zip", $"Root/{folder}/{file}");

        var document = Assert.Single(plan.Documents);
        Assert.Equal(folder, document.Title);
        Assert.Equal(file, Assert.Single(document.Parts).FileName);
    }

    [Fact]
    [Trait("Rule", "R11")]
    public void A_name_that_is_not_valid_utf8_is_rejected()
    {
        var ex = Assert.Throws<InvalidInputException>(() => Plan("upload.zip", "Root/caf\uFFFD.txt"));
        Assert.Contains("not valid UTF-8", ex.Message);
    }

    private static UploadPlan Plan(string zipFileName, params string[] names) =>
        UploadPlanner.Plan([.. names.Select((name, index) => new UploadEntry { Index = index, FullName = name, Length = 1 })], zipFileName);
}