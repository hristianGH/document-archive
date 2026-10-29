# Archive.Core

The rules of the Archive. It decides what an upload stores, checks index values, reads date ranges and pages, and defines the errors. It does not know about HTTP or the database; it talks to storage through interfaces that `Archive.Data` implements.

## References

- `Shared`: `ServiceException`.
- Package: logging abstractions.

## Layout

| Folder | What it holds |
|---|---|
| `Cabinets/` | `Cabinet`, `IndexField`, `FieldType` (text, number, date), `CabinetCatalog` (all loaded cabinets). |
| `Documents/` | `Document`, `Part`, `DocumentSummary`, `DocumentQuery`, `PartContent`. |
| `Handlers/` | `CabinetHandler`, `DocumentHandler`, `UploadHandler` and their interfaces. |
| `Uploads/` | `UploadPlanner` and the plan and report models. |
| `Validation/` | `IndexFieldValidator`. |
| `Dates/` | `UtcInstant`. |
| `Paging/` | `PagedList`, `PagingSettings`. |
| `Repositories/` | `IDocumentRepository`. |
| `Storage/` | `IPartFileStore`. |
| `Exceptions/` | The Archive errors. |

## Cabinets

- `CabinetCatalog` holds every cabinet from `cabinets.json`. Cabinet and field names are found ignoring case.
- A field has a name, a label, a type, an optional maximum length, and whether it is writable.

## Handlers

### CabinetHandler

Lists all cabinets, or finds one by name. An unknown cabinet is "not found".

### DocumentHandler

- **Get a document** with its parts in order.
- **Get a page** of a cabinet's documents stored between `from` and `to`. Checks the dates, that `from` is not after `to`, and caps the page size at `PagingSettings.MaxPageSize`.
- **Get a part's content.** The part must belong to the document. If the file is missing or its size differs from the stored size, it is a server error.
- **Set an index value.** The field must exist and be writable, and the value must fit its type and limit. If the value is already the same, nothing is written. Otherwise the value is saved and `ModifiedAt` is set to now.

### UploadHandler

Stores a zipped folder as documents in one cabinet:

1. The cabinet must exist; the field must be a writable text field.
2. Checks the zip: its size, that it is a real zip, its entry count and its total unpacked size.
3. `UploadPlanner` turns the entries into documents (see below).
4. The folder name must fit the field's maximum length, and there must be at least one document.
5. Stores each document: saves its part files, then saves the document with `StoredAt` and `ModifiedAt` set to now and the field set to the folder name.
6. Returns the report.

When something goes wrong:

- **A damaged file:** that document goes to "failed" and the rest continue.
- **A server error:** the upload stops; the report says what was already stored and what was not.
- **Nothing stored at all:** it is an error, with the report.
- A document that fails leaves no files behind.

## Upload rules (UploadPlanner)

- If the zip has one root folder, its contents are the folder and its name is the folder name. Otherwise the zip's name without `.zip` is the folder name.
- Each top-level file becomes a document with one part.
- Each top-level subfolder becomes a document whose parts are its files, in natural order ignoring case (`page2` before `page10`).
- An empty subfolder becomes a document with no parts.
- The title is the file or folder name.
- Anything nested deeper is skipped with "Nested deeper than one level."
- System files (such as `__MACOSX`) and symlinks are dropped without a report.
- Unsafe paths (absolute paths, drive letters, `..`) and names that are not valid UTF-8 reject the whole upload.
- Names keep every Unicode character exactly.

## Index values (IndexFieldValidator)

| Type | Accepts | Stored as |
|---|---|---|
| Text | A string, up to the maximum length. `""` clears it. | The string. |
| Number | A JSON number. | The number. |
| Date | A real date as `yyyy-MM-dd`. | `yyyy-MM-dd`. |

`null` and wrong types are rejected with a message that says what the field needs.

## Dates (UtcInstant)

- `from` and `to` are UTC only: `yyyy-MM-ddTHH:mm:ssZ` or `yyyy-MM-dd`.
- A date-only `from` is 00:00:00; a date-only `to` is 23:59:59.
- Stored times are UTC, cut to whole seconds.

## Errors

All derive from `ArchiveException`, which carries the HTTP status.

| Exception | Status |
|---|---|
| `InvalidInputException` | 400 |
| `NotFoundException` | 404 |
| `PayloadTooLargeException` | 413 |
| `PartContentMissingException` | 500 |
| `UploadFailedException` (carries the upload report) | 500 |

## Conventions

- Handlers take the time from `TimeProvider`, not `DateTime.UtcNow`, so tests can fix it.
- Handlers follow the same logging pattern as the API: Start, Error (through `LogMethodError`), End with counts.
- Error messages are plain sentences meant for the user.
