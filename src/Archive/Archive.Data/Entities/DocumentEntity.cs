namespace Archive.Data.Entities;

public class DocumentEntity : SystemFields
{
    public Guid Id { get; set; }

    public string Cabinet { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string IndexValues { get; set; } = "{}";

    public List<PartEntity> Parts { get; set; } = [];
}
