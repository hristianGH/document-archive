using Export.Core.Paging;

namespace Export.Core.Exports;

public sealed class PreparedExport
{
    public required string Cabinet { get; init; }

    public required string Field { get; init; }

    public required string From { get; init; }

    public required string To { get; init; }

    public required string Marker { get; init; }

    public required string FileName { get; init; }

    public required PagedList<Guid> FirstPage { get; init; }
}