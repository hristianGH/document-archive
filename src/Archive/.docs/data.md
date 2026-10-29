# Archive.Data

Storage for the Archive: document records in PostgreSQL and part files on disk. It implements the interfaces from `Archive.Core`.

## References

- `Archive.Core`.
- Package: Npgsql EF Core (PostgreSQL).

## Layout

| Item | What it is |
|---|---|
| `ArchiveDbContext` | The database model. |
| `Entities/` | `DocumentEntity`, `PartEntity`, `SystemFields` (`StoredAt`, `ModifiedAt`). |
| `Repositories/DocumentRepository` | Implements `IDocumentRepository`. |
| `Storage/PartFileStore` | Implements `IPartFileStore`. |
| `Migrations/` | `InitialCreate`. |

## Database

Two tables:

| Table | Columns | Notes |
|---|---|---|
| `Documents` | `Id`, `Cabinet`, `Title`, `StoredAt`, `ModifiedAt`, `IndexValues` | `IndexValues` is `jsonb`, one key per field. Indexed on cabinet, stored time and id for the range query. |
| `Parts` | `Id`, `DocumentId`, `Position`, `FileName`, `Size`, `FilePath` | One position per document and one file path per part. Deleted with their document. |

Migrations are applied by the API when it starts.

## DocumentRepository

- **Get a document** with its parts ordered by position.
- **Get a page:** documents of one cabinet with `StoredAt` between `from` and `to`, both included, ordered by `StoredAt` then `Id` so pages stay stable. Returns the page and the total count.
- **Get a part**, only through its own document.
- **Add a document** with its parts in one save.
- **Set an index value** in one statement that merges the key into `IndexValues` and sets `ModifiedAt`. Other fields stay as they are, even when written at the same time.

## PartFileStore

- Files are saved under `Storage:FilesPath` as `{documentId}/{partId}`, never by their original name.
- Write refuses to overwrite an existing file and returns the bytes written.
- Read returns nothing when the file is missing.
- Delete removes the file, and the document's folder once it is empty.

## Conventions

- Reads use no tracking. Writes clear the tracker afterwards, so a failed save leaves nothing pending.
- Every method logs Start, Error and End with its parameters and result counts.
- Entities stay in this project; callers get `Archive.Core` models.
