using Archive.Core.Storage;

namespace Archive.Data.Storage;

public class PartFileStore : IPartFileStore
{
    private readonly string _rootPath;

    public PartFileStore(string rootPath)
    {
        _rootPath = rootPath;
    }

    public Stream? OpenRead(string filePath)
    {
        try
        {
            return new FileStream(Path.Combine(_rootPath, filePath), new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }
}
