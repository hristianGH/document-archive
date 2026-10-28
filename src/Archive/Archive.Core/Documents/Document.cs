namespace Archive.Core.Documents;

public class Document
{
    public Guid Id { get; set; }

    public string Cabinet { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime StoredAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public string IndexValuesJson { get; set; } = string.Empty;

    public IReadOnlyList<Part> Parts { get; set; } = [];
}
