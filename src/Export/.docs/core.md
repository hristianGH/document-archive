# Export.Core

The rules of the export: which field may hold the marker, how documents are laid out in the zip, how duplicate names are handled, and when documents are marked. It talks to the Archive only through `IArchiveClient`, which `Export.Data` implements.

## References

- `Shared`: `ServiceException`.

## Layout

| Folder | What it holds |
|---|---|
| `Handlers/` | `ExportHandler` and `IExportHandler`. |
| `Exports/` | `ExportMarker`, `MarkerFieldValidator`, `ZipLayout`, `ZipNameAllocator`, `PreparedExport`, `ZipEntryPlan`, `SyncBufferingStream`. |
| `Clients/` | `IArchiveClient`, `ArchiveClientSettings`. |
| `Cabinets/`, `Documents/`, `Paging/` | The Export's own view of cabinets, documents and pages. |
| `Exceptions/` | The Export errors. |

## ExportHandler

An export runs in two steps.

### 1. Prepare (before any bytes are sent)

1. Today's date in UTC gives the marker, `Exported on yyyy-MM-dd`.
2. Gets the cabinet from the Archive (unknown → 404).
3. Checks the field with `MarkerFieldValidator`.
4. Gets the first page of documents in the range. The Archive checks the dates. No documents → 404.
5. Names the file `{cabinet}-export-{yyyy-MM-dd}.zip`.

Every error here becomes a normal error answer.

### 2. Write (streams the zip)

1. For each page, for each document: gets the document and copies each part's bytes from the Archive straight into the zip.
2. Moves to the next page until all documents are written. Works with any Archive page size.
3. After every document is in the zip, sets the marker on each one.
4. Only then finishes the zip.

What this means when something fails:

- **Before the zip is finished,** the zip has no table of contents, so it cannot be opened as complete.
- **While writing documents,** nothing is marked.
- **While marking,** documents marked before the failure keep the marker; the rest do not. Exporting again exports and marks everything, because the marker is not a filter.

The zip is sent while it is built and parts are copied without being held in memory, so an export can be larger than the service's memory.

## Rules

### Marker field (MarkerFieldValidator)

The field must exist in the cabinet (found ignoring case), be writable, be a text field, and hold at least as many characters as the marker (22). A field with no maximum length passes. Each failure has its own message; the last one names the field, its limit and the marker length.

### Marker (ExportMarker)

`Exported on yyyy-MM-dd`, always with the Gregorian calendar and Western digits, whatever the server's language settings.

### Zip layout (ZipLayout)

| Document | In the zip |
|---|---|
| One part | A file named after the document title. |
| Several parts | A folder named after the title, with one file per part, in part order. |
| No parts | An empty folder named after the title. |

### Duplicate names (ZipNameAllocator)

- The first name is kept; later ones get ` (2)`, ` (3)`, and so on.
- Files are numbered before the extension (`report (2).pdf`), folders at the end (`Contract (2)`).
- The extension starts at the last dot, unless the dot is the first character.
- Names that differ only in case count as the same.
- Files and folders at the top level share one set of names. Inside each folder, part names are numbered separately.
- A number already taken is skipped.

## Errors

All derive from `ExportException`, which carries the HTTP status.

| Exception | Status | When |
|---|---|---|
| `InvalidInputException` | 400 | The request or the field is not valid. |
| `NotFoundException` | 404 | No documents in the range. |
| `ArchiveRejectedException` | the Archive's 4xx | The Archive refused the request; keeps its reason. |
| `ArchiveFailedException` | 502 | The Archive answered with an error after all retries. |
| `ArchiveUnavailableException` | 503 | The Archive could not be reached after all retries. |
| `ArchiveTimeoutException` | 504 | No answer in time, or a part's bytes took too long. |

## SyncBufferingStream

A small workaround for .NET 10, where zip entries finish with a blocking write that the web server refuses. It holds those bytes and sends them without blocking. It can be removed with .NET 11.

## Conventions

- The time comes from `TimeProvider`, so tests can fix it.
- Handlers log Start, Error (through `LogMethodError`) and End with counts.
- Error messages are plain sentences meant for the user.
