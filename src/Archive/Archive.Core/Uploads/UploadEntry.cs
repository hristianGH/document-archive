namespace Archive.Core.Uploads;

public class UploadEntry
{
    public int Index { get; set; }

    public string FullName { get; set; } = string.Empty;

    public long Length { get; set; }

    public bool IsSymlink { get; set; }
}
