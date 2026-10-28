using Archive.Core.Storage;

namespace Archive.Data.Storage;

public class PartFileStore(string rootPath) : IPartFileStore
{
    public Stream? OpenRead(string filePath)
    {
        try
        {
            return new FileStream(Path.Combine(rootPath, filePath), new FileStreamOptions
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

    public async Task<long> WriteAsync(string filePath, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(rootPath, filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        });
        await content.CopyToAsync(file, cancellationToken);
        return file.Length;
    }

    public void Delete(string filePath)
    {
        var fullPath = Path.Combine(rootPath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var directory = Path.GetDirectoryName(fullPath)!;
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
