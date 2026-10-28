using System.Globalization;
using Archive.Core.Exceptions;

namespace Archive.Core.Uploads;

public static class UploadPlanner
{
    private const string NestedReason = "Nested deeper than one level.";
    private const string MacMetadataFolder = "__MACOSX";

    private static readonly StringComparer PartOrder =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering | CompareOptions.IgnoreCase);

    private static readonly HashSet<string> SystemFileNames = new(StringComparer.OrdinalIgnoreCase) { ".DS_Store", "Thumbs.db", "desktop.ini" };

    public static UploadPlan Plan(IReadOnlyList<UploadEntry> entries, string zipFileName)
    {
        foreach (var entry in entries)
        {
            CheckInvalidName(entry.FullName);
        }

        var items = DropSystemFilesAndSymlinks([.. entries.Select(ToItem)]);
        var rootFolder = FindSingleRootFolder(items);
        var rootDepth = rootFolder is null ? 0 : 1;
        var content = items.Where(item => item.DepthBelow(rootDepth) > 0).ToList();

        return new UploadPlan
        {
            FolderName = GetFolderName(rootFolder, zipFileName),
            Documents = GroupAndOrder(content, rootDepth),
            Skipped = SkipNested(content, rootDepth)
        };
    }

    private static void CheckInvalidName(string fullName)
    {
        // U+FFFD (�) is what the zip reader puts in place of bytes that are not valid UTF-8.
        if (fullName.Contains('\uFFFD'))
        {
            throw new InvalidInputException($"Entry '{fullName}' has a name that is not valid UTF-8.");
        }

        var path = fullName.Replace('\\', '/');
        if (path.StartsWith('/') || HasDriveLetter(path) || path.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new InvalidInputException($"Entry '{fullName}' has an unsafe path.");
        }
    }

    private static bool HasDriveLetter(string path) => path.Length > 1 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    private static Item ToItem(UploadEntry entry)
    {
        var path = entry.FullName.Replace('\\', '/');
        return new Item
        {
            Entry = entry,
            Segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries),
            IsDirectory = path.EndsWith('/')
        };
    }

    private static List<Item> DropSystemFilesAndSymlinks(List<Item> items) =>
        [.. items.Where(item => item.Segments.Length > 0 && !item.Entry.IsSymlink && !IsSystemEntry(item.Segments))];

    private static bool IsSystemEntry(string[] segments) =>
        segments.Contains(MacMetadataFolder, StringComparer.OrdinalIgnoreCase) || SystemFileNames.Contains(segments[^1]);

    private static string? FindSingleRootFolder(List<Item> items)
    {
        var topLevelNames = items.Select(item => item.Segments[0]).Distinct(StringComparer.Ordinal).ToList();
        return topLevelNames.Count == 1 && items.All(item => item.IsDirectory || item.Segments.Length > 1)
            ? topLevelNames[0]
            : null;
    }

    private static string GetFolderName(string? rootFolder, string zipFileName) =>
        rootFolder ?? Path.GetFileNameWithoutExtension(zipFileName);

    private static List<PlannedDocument> GroupAndOrder(List<Item> content, int rootDepth)
    {
        var documents = new List<DocumentDraft>();
        var folders = new Dictionary<string, DocumentDraft>(StringComparer.Ordinal);

        foreach (var item in content)
        {
            var depth = item.DepthBelow(rootDepth);
            if (depth == 1 && !item.IsDirectory)
            {
                documents.Add(new DocumentDraft { Path = item.Path, Title = item.Name, Files = [item] });
                continue;
            }

            var folderTitle = item.Segments[rootDepth];
            if (!folders.TryGetValue(folderTitle, out var folder))
            {
                folder = new DocumentDraft { Path = string.Join('/', item.Segments[..(rootDepth + 1)]), Title = folderTitle };
                folders.Add(folderTitle, folder);
                documents.Add(folder);
            }

            if (depth == 2 && !item.IsDirectory)
            {
                folder.Files.Add(item);
            }
        }

        return [.. documents.Select(ToPlannedDocument)];
    }

    private static PlannedDocument ToPlannedDocument(DocumentDraft draft) => new()
    {
        Path = draft.Path,
        Title = draft.Title,
        Parts = [.. draft.Files
            .OrderBy(file => file.Name, PartOrder)
            .Select((file, position) => new PlannedPart
            {
                EntryIndex = file.Entry.Index,
                FileName = file.Name,
                Position = position,
                Length = file.Entry.Length
            })]
    };

    private static List<UploadReportEntry> SkipNested(List<Item> content, int rootDepth) =>
        [.. content
            .Where(item => item.DepthBelow(rootDepth) > 2 || (item.DepthBelow(rootDepth) == 2 && item.IsDirectory))
            .Select(item => new UploadReportEntry { Path = item.DisplayPath, Reason = NestedReason })];

    private class Item
    {
        public UploadEntry Entry { get; set; } = new();

        public string[] Segments { get; set; } = [];

        public bool IsDirectory { get; set; }

        public string Name => Segments[^1];

        public string Path => string.Join('/', Segments);

        public string DisplayPath => IsDirectory ? Path + "/" : Path;

        public int DepthBelow(int rootDepth) => Segments.Length - rootDepth;
    }

    private class DocumentDraft
    {
        public string Path { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public List<Item> Files { get; set; } = [];
    }
}