namespace Archive.Core.Uploads;

public class UploadSettings
{
    public long MaxZipBytes { get; set; }

    public long MaxUncompressedBytes { get; set; }

    public int MaxEntries { get; set; }
}
