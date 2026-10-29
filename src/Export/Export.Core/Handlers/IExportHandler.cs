using Export.Core.Exports;

namespace Export.Core.Handlers;

public interface IExportHandler
{
    Task<PreparedExport> PrepareAsync(string cabinet, string field, string from, string to, CancellationToken cancellationToken);

    Task WriteAsync(PreparedExport export, Stream output, CancellationToken cancellationToken);
}