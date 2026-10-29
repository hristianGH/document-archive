using System.Globalization;
using System.IO.Compression;
using System.Text;
using Export.Core.Clients;
using Export.Core.Exceptions;
using Export.Core.Exports;
using Microsoft.Extensions.Logging;
using Shared.Logging;

namespace Export.Core.Handlers;

public class ExportHandler(
    IArchiveClient archiveClient,
    TimeProvider timeProvider,
    ILogger<ExportHandler> logger) : IExportHandler
{
    public async Task<PreparedExport> PrepareAsync(string cabinet, string field, string from, string to, CancellationToken cancellationToken)
    {
        PreparedExport? export = null;
        logger.LogInformation("Start ExportHandler.PrepareAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
            cabinet, field, from, to);
        try
        {
            var exportDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var marker = ExportMarker.For(exportDate);
            var foundCabinet = await archiveClient.GetCabinetAsync(cabinet, cancellationToken);
            var markerField = MarkerFieldValidator.GetMarkerField(foundCabinet, field, marker);

            var firstPage = await archiveClient.GetDocumentIdsAsync(foundCabinet.Name, from, to, 1, cancellationToken);
            if (firstPage.TotalCount == 0)
            {
                throw new NotFoundException($"No documents in {foundCabinet.Name} in this range.");
            }

            export = new PreparedExport
            {
                Cabinet = foundCabinet.Name,
                Field = markerField.Name,
                From = from,
                To = to,
                Marker = marker,
                FileName = string.Create(CultureInfo.InvariantCulture, $"{foundCabinet.Name}-export-{exportDate:yyyy-MM-dd}.zip"),
                FirstPage = firstPage
            };
            return export;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ExportHandler.PrepareAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
                cabinet, field, from, to);
            throw;
        }
        finally
        {
            logger.LogInformation("End ExportHandler.PrepareAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}; documents={DocumentCount}",
                cabinet, field, from, to, export?.FirstPage.TotalCount);
        }
    }

    public async Task WriteAsync(PreparedExport export, Stream output, CancellationToken cancellationToken)
    {
        var documentIds = new List<Guid>();
        var marked = 0;
        logger.LogInformation("Start ExportHandler.WriteAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
            export.Cabinet, export.Field, export.From, export.To);
        try
        {
            // Disposed only on success: disposing writes the table of contents, which would make a broken export look complete.
            var zip = await ZipArchive.CreateAsync(output, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8, cancellationToken);
            var rootNames = new ZipNameAllocator();
            var page = export.FirstPage;
            while (true)
            {
                foreach (var id in page.Items)
                {
                    var document = await archiveClient.GetDocumentAsync(id, cancellationToken);
                    foreach (var entry in ZipLayout.Plan(document, rootNames))
                    {
                        await WriteEntryAsync(zip, entry, document.Id, cancellationToken);
                    }

                    documentIds.Add(id);
                }

                if (page.Items.Count == 0 || (long)page.Page * page.PageSize >= page.TotalCount)
                {
                    break;
                }

                page = await archiveClient.GetDocumentIdsAsync(export.Cabinet, export.From, export.To, page.Page + 1, cancellationToken);
            }

            foreach (var id in documentIds)
            {
                await archiveClient.SetIndexValueAsync(id, export.Field, export.Marker, cancellationToken);
                marked++;
            }

            await zip.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ExportHandler.WriteAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
                export.Cabinet, export.Field, export.From, export.To);
            throw;
        }
        finally
        {
            logger.LogInformation("End ExportHandler.WriteAsync with params cabinet={Cabinet}, field={Field}, from={From}, to={To}; documents={DocumentCount}, marked={MarkedCount}",
                export.Cabinet, export.Field, export.From, export.To, documentIds.Count, marked);
        }
    }

    private async Task WriteEntryAsync(ZipArchive zip, ZipEntryPlan entry, Guid documentId, CancellationToken cancellationToken)
    {
        // Every entry is opened, folders too: an entry never opened is written synchronously, which Kestrel rejects.
        var entryStream = await zip.CreateEntry(entry.Name).OpenAsync(cancellationToken);
        if (entry.PartId is { } partId)
        {
            await archiveClient.CopyPartContentAsync(documentId, partId, entryStream, cancellationToken);
        }

        await entryStream.DisposeAsync();
    }
}