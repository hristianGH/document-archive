namespace Archive.Core.Uploads;

public class PlannedDocument
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<PlannedPart> Parts { get; set; } = [];
}
