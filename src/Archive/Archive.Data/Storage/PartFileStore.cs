using Archive.Core.Storage;
using Microsoft.Extensions.Logging;

namespace Archive.Data.Storage;

public class PartFileStore(string rootPath, ILogger<PartFileStore> logger) : IPartFileStore
{
    public Stream? OpenRead(string filePath)
    {
        Stream? stream = null;
        logger.LogInformation("Start PartFileStore.OpenRead with params filePath={FilePath}", filePath);
        try
        {
            stream = new FileStream(Path.Combine(rootPath, filePath), new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
            return stream;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error PartFileStore.OpenRead with params filePath={FilePath}", filePath);
            throw;
        }
        finally
        {
            logger.LogInformation("End PartFileStore.OpenRead with params filePath={FilePath}; found={Found}", filePath, stream is not null);
        }
    }

    public async Task<long> WriteAsync(string filePath, Stream content, CancellationToken cancellationToken)
    {
        long? bytes = null;
        logger.LogInformation("Start PartFileStore.WriteAsync with params filePath={FilePath}", filePath);
        try
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
            bytes = file.Length;
            return file.Length;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error PartFileStore.WriteAsync with params filePath={FilePath}", filePath);
            throw;
        }
        finally
        {
            logger.LogInformation("End PartFileStore.WriteAsync with params filePath={FilePath}; bytes={Bytes}", filePath, bytes);
        }
    }

    public void Delete(string filePath)
    {
        logger.LogInformation("Start PartFileStore.Delete with params filePath={FilePath}", filePath);
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error PartFileStore.Delete with params filePath={FilePath}", filePath);
            throw;
        }
        finally
        {
            logger.LogInformation("End PartFileStore.Delete with params filePath={FilePath}", filePath);
        }
    }
}
