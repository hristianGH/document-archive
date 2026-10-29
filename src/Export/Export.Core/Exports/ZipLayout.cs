using Export.Core.Documents;

namespace Export.Core.Exports;

public static class ZipLayout
{
    public static IReadOnlyList<ZipEntryPlan> Plan(Document document, ZipNameAllocator rootNames)
    {
        var parts = document.Parts.OrderBy(p => p.Position).ToList();
        if (parts.Count == 1)
        {
            return [new ZipEntryPlan(rootNames.AllocateFile(document.Title), parts[0].Id)];
        }

        var folder = rootNames.AllocateFolder(document.Title);
        if (parts.Count == 0)
        {
            return [new ZipEntryPlan($"{folder}/", null)];
        }

        var partNames = new ZipNameAllocator();
        return parts.Select(p => new ZipEntryPlan($"{folder}/{partNames.AllocateFile(p.FileName)}", p.Id)).ToList();
    }
}