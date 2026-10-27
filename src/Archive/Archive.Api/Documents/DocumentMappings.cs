using System.Text.Json;
using Archive.Core.Documents;
using Archive.Core.Paging;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Archive.Api.Documents;

public static class DocumentMappings
{
    public static DocumentResponse ToResponse(this Document document) => new()
    {
        Id = document.Id,
        Cabinet = document.Cabinet,
        Title = document.Title,
        StoredAt = document.StoredAt,
        ModifiedAt = document.ModifiedAt,
        IndexValues = ParseIndexValues(document.IndexValuesJson),
        Parts = [.. document.Parts.Select(ToResponse)]
    };

    public static PagedResponse<DocumentListItemResponse> ToResponse(this PagedList<DocumentSummary> page) => new()
    {
        Items = [.. page.Items.Select(ToResponse)],
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount
    };

    private static DocumentListItemResponse ToResponse(this DocumentSummary document) => new()
    {
        Id = document.Id,
        Cabinet = document.Cabinet,
        Title = document.Title,
        StoredAt = document.StoredAt,
        ModifiedAt = document.ModifiedAt,
        IndexValues = ParseIndexValues(document.IndexValuesJson),
        PartCount = document.PartCount
    };

    private static PartResponse ToResponse(this Part part) => new()
    {
        Id = part.Id,
        Position = part.Position,
        FileName = part.FileName,
        Size = part.Size
    };

    private static JsonElement ParseIndexValues(string json) => JsonSerializer.Deserialize<JsonElement>(json);
}
