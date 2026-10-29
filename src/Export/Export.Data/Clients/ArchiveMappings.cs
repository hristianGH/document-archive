using Export.Core.Cabinets;
using Export.Core.Documents;
using Export.Core.Paging;
using Shared.Models.Cabinets;
using Shared.Models.Documents;
using Shared.Models.Paging;
using CoreFieldType = Export.Core.Cabinets.FieldType;
using ResponseFieldType = Shared.Models.Cabinets.FieldType;

namespace Export.Data.Clients;

public static class ArchiveMappings
{
    public static Cabinet ToCabinet(this CabinetResponse response) => new()
    {
        Name = response.Name,
        DisplayName = response.DisplayName,
        Fields = [.. response.Fields.Select(ToIndexField)]
    };

    public static IndexField ToIndexField(this IndexFieldResponse response) => new()
    {
        Name = response.Name,
        Label = response.Label,
        Type = response.Type.ToFieldType(),
        MaxLength = response.MaxLength,
        Writable = response.Writable
    };

    public static Document ToDocument(this DocumentResponse response) => new()
    {
        Id = response.Id,
        Title = response.Title,
        Parts = [.. response.Parts.Select(p => new Part { Id = p.Id, Position = p.Position, FileName = p.FileName })]
    };

    public static PagedList<Guid> ToIdPage(this PagedResponse<DocumentListItemResponse> response) => new()
    {
        Items = [.. response.Items.Select(d => d.Id)],
        Page = response.Page,
        PageSize = response.PageSize,
        TotalCount = response.TotalCount
    };

    private static CoreFieldType ToFieldType(this ResponseFieldType type) => type switch
    {
        ResponseFieldType.Text => CoreFieldType.Text,
        ResponseFieldType.Number => CoreFieldType.Number,
        ResponseFieldType.Date => CoreFieldType.Date,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}