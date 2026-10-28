namespace Archive.Core.Documents;

public class DocumentQuery
{
    public string Cabinet { get; set; } = string.Empty;

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
