using System.Text.Json;

namespace Shared.Models.Documents;

public class DocumentListItemResponse
{
    public Guid Id { get; set; }

    public string Cabinet { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime StoredAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public JsonElement IndexValues { get; set; }

    public int PartCount { get; set; }
}
