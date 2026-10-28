namespace Archive.Core.Uploads;

public class PlannedPart
{
    public int EntryIndex { get; set; }

    public string FileName { get; set; } = string.Empty;

    public int Position { get; set; }

    public long Length { get; set; }
}
