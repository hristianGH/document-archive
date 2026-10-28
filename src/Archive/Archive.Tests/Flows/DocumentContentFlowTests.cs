using System.Net;
using Archive.Core.Documents;
using Archive.Tests.Infrastructure;

namespace Archive.Tests.Flows;

public class DocumentContentFlowTests : FlowTestBase
{
    [Fact]
    public async Task An_unknown_document_is_not_found()
    {
        var id = Guid.NewGuid();

        await ProblemAssert.IsProblemAsync(await Client.GetAsync($"documents/{id}"), HttpStatusCode.NotFound, $"Document '{id}' was not found.");
    }

    [Fact]
    public async Task A_part_of_another_document_is_not_found()
    {
        var first = Seed(new Part { Id = Guid.NewGuid(), FileName = "a.txt", Size = 1, FilePath = "a" });
        var second = Seed(new Part { Id = Guid.NewGuid(), FileName = "b.txt", Size = 1, FilePath = "b" });
        var partId = second.Parts[0].Id;

        await ProblemAssert.IsProblemAsync(await Client.GetAsync($"documents/{first.Id}/parts/{partId}/content"),
            HttpStatusCode.NotFound, $"Part '{partId}' of document '{first.Id}' was not found.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12")]
    public async Task A_missing_or_wrong_size_file_is_a_server_error(string? fileContent)
    {
        var document = Seed(new Part { Id = Guid.NewGuid(), FileName = "a.txt", Size = 5, FilePath = "doc/part" });
        if (fileContent is not null)
        {
            Directory.CreateDirectory(Path.Combine(Factory.FilesPath, "doc"));
            File.WriteAllText(Path.Combine(Factory.FilesPath, "doc", "part"), fileContent);
        }

        var partId = document.Parts[0].Id;
        await ProblemAssert.IsProblemAsync(await Client.GetAsync($"documents/{document.Id}/parts/{partId}/content"),
            HttpStatusCode.InternalServerError, $"Content for part {partId} is missing or corrupt.");
    }

    private Document Seed(Part part)
    {
        var document = new Document { Id = Guid.NewGuid(), Cabinet = "Invoices", Title = part.FileName, IndexValuesJson = "{}", Parts = [part] };
        Factory.Repository.Seed(document);
        return document;
    }
}