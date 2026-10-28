using System.IO.Compression;
using System.Text.Json;
using Archive.Core.Dates;
using Archive.Core.Documents;
using Archive.Core.Exceptions;
using Archive.Core.Repositories;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Handlers;

public class UploadHandler(
    ICabinetHandler cabinetHandler,
    IDocumentRepository documentRepository,
    IPartFileStore partFileStore,
    UploadSettings uploadSettings,
    // Instead of DateTime.UtcNow so tests can fix the time; same clock in production, no real overhead.
    TimeProvider timeProvider,
    ILogger<UploadHandler> logger) : IUploadHandler
{
    private const int UnixFileTypeMask = 0xF000;
    private const int UnixSymlinkType = 0xA000;
    private const string DamagedReason = "File is damaged in the zip.";
    private const string NotStoredReason = "Not stored because of a server error.";
    private const string NothingStoredMessage = "Nothing was stored; see the report for each failure.";

    public async Task<UploadReport> UploadAsync(string cabinet, string field, string zipFileName, Stream zip, CancellationToken cancellationToken)
    {
        var cabinetModel = cabinetHandler.Get(cabinet);
        var indexField = IndexFieldValidator.GetWritableField(cabinetModel, field);
        IndexFieldValidator.EnsureText(indexField);

        if (zip.Length > uploadSettings.MaxZipBytes)
        {
            throw new PayloadTooLargeException($"The zip is {zip.Length} bytes; the limit is {uploadSettings.MaxZipBytes} bytes.");
        }

        await using var archive = await OpenZipAsync(zip, cancellationToken);
        EnsureWithinLimits(archive.Entries);

        var plan = UploadPlanner.Plan([.. archive.Entries.Select(ToUploadEntry)], zipFileName);
        if (string.IsNullOrWhiteSpace(plan.FolderName))
        {
            throw new InvalidInputException("The folder has no name. Give the zip file a name.");
        }

        IndexFieldValidator.ValidateText(indexField, plan.FolderName);
        if (plan.Documents.Count == 0)
        {
            throw new InvalidInputException($"Folder '{plan.FolderName}' has nothing to store.");
        }

        var indexValuesJson = JsonSerializer.Serialize(new Dictionary<string, string> { [indexField.Name] = plan.FolderName });
        var stored = new List<StoredDocument>();
        var failed = new List<UploadReportEntry>();
        var report = new UploadReport { FolderName = plan.FolderName, Field = indexField.Name, Stored = stored, Skipped = plan.Skipped, Failed = failed };

        for (var i = 0; i < plan.Documents.Count; i++)
        {
            var plannedDocument = plan.Documents[i];
            try
            {
                stored.Add(await StoreAsync(archive, cabinetModel.Name, plannedDocument, indexValuesJson, cancellationToken));
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex, "Damaged file in {Path} for cabinet {Cabinet}", plannedDocument.Path, cabinetModel.Name);
                failed.Add(new UploadReportEntry { Path = plannedDocument.Path, Reason = DamagedReason });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not store {Path} in cabinet {Cabinet}; stopping the upload", plannedDocument.Path, cabinetModel.Name);
                failed.AddRange(plan.Documents.Skip(i).Select(d => new UploadReportEntry { Path = d.Path, Reason = NotStoredReason }));
                var message = $"The upload stopped because of a server error. {stored.Count} document(s) were stored; "
                    + $"{plan.Documents.Count - i} were not stored. See the report; upload only the documents that were not stored.";
                throw new UploadFailedException(message, report, ex);
            }
        }

        return stored.Count == 0
            ? throw new UploadFailedException(NothingStoredMessage, report)
            : report;
    }

    private static async Task<ZipArchive> OpenZipAsync(Stream zip, CancellationToken cancellationToken)
    {
        try
        {
            // leaveOpen: the caller opened the stream and disposes it; ZipArchive must not close it.
            return await ZipArchive.CreateAsync(zip, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null, cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidInputException("The file is not a valid zip.", ex);
        }
    }

    private void EnsureWithinLimits(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        if (entries.Count > uploadSettings.MaxEntries)
        {
            throw new InvalidInputException($"The zip has {entries.Count} entries; the limit is {uploadSettings.MaxEntries}.");
        }

        var unpackedBytes = 0L;
        foreach (var entry in entries)
        {
            if (entry.Length > uploadSettings.MaxUncompressedBytes - unpackedBytes)
            {
                throw new PayloadTooLargeException($"The unpacked content is over the {uploadSettings.MaxUncompressedBytes} byte limit.");
            }

            unpackedBytes += entry.Length;
        }
    }

    private static UploadEntry ToUploadEntry(ZipArchiveEntry entry, int index) => new()
    {
        Index = index,
        FullName = entry.FullName,
        Length = entry.Length,
        IsSymlink = ((entry.ExternalAttributes >> 16) & UnixFileTypeMask) == UnixSymlinkType
    };

    private async Task<StoredDocument> StoreAsync(ZipArchive archive, string cabinet, PlannedDocument plannedDocument, string indexValuesJson, CancellationToken cancellationToken)
    {
        var documentId = Guid.CreateVersion7();
        var parts = new List<Part>();
        try
        {
            foreach (var plannedPart in plannedDocument.Parts)
            {
                var partId = Guid.CreateVersion7();
                var part = new Part
                {
                    Id = partId,
                    Position = plannedPart.Position,
                    FileName = plannedPart.FileName,
                    Size = plannedPart.Length,
                    FilePath = $"{documentId}/{partId}"
                };
                parts.Add(part);

                await using var content = await archive.Entries[plannedPart.EntryIndex].OpenAsync(cancellationToken);
                if (await partFileStore.WriteAsync(part.FilePath, content, cancellationToken) != part.Size)
                {
                    throw new InvalidDataException($"'{plannedPart.FileName}' does not match its declared size.");
                }
            }

            var now = UtcInstant.ToWholeSeconds(timeProvider.GetUtcNow());
            await documentRepository.AddAsync(new Document
            {
                Id = documentId,
                Cabinet = cabinet,
                Title = plannedDocument.Title,
                StoredAt = now,
                ModifiedAt = now,
                IndexValuesJson = indexValuesJson,
                Parts = parts
            }, cancellationToken);

            return new StoredDocument { DocumentId = documentId, Title = plannedDocument.Title, PartCount = parts.Count };
        }
        catch
        {
            DeleteFiles(parts);
            throw;
        }
    }

    private void DeleteFiles(IEnumerable<Part> parts)
    {
        foreach (var part in parts)
        {
            try
            {
                partFileStore.Delete(part.FilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete part file {FilePath}", part.FilePath);
            }
        }
    }
}