namespace Export.Core.Documents;

public class Document
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<Part> Parts { get; set; } = [];
}