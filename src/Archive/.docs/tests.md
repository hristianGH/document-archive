# Archive.Tests

xUnit tests for the Archive service. They run without Docker, a database or a network.

## References

- `Archive.Api`, `Archive.Core`, `Archive.Data`.
- Packages: xUnit, `Microsoft.AspNetCore.Mvc.Testing` (in-memory server), EF Core InMemory, `FakeTimeProvider`.

## Layout

| Folder | What it tests |
|---|---|
| `Flows/` | The real API end to end over HTTP: cabinets, upload, document query, part content, index values. |
| `Rules/` | Single rules with no server: `UploadPlanner`, `IndexFieldValidator`, `UtcInstant`. |
| `Repositories/` | `DocumentRepository` against an in-memory database. |
| `Infrastructure/` | Test helpers (below). |
| `TestData/` | `cabinets.json` and test zips: `sample.zip`, `damaged.zip`, `all-damaged.zip`, `invalid-name.zip`. |

## How flow tests run

`ArchiveApiFactory` starts the real Archive API in memory with test settings (page size 5, the same upload limits as production). Only two things are replaced:

- the database, by `FakeDocumentRepository` (in memory; it can fail on a chosen save to fake a server error halfway);
- the clock, by `FakeTimeProvider`, starting at 2026-10-01 09:30 UTC.

Part files are written to a temporary folder that is deleted after each test. Each flow test class gets a fresh factory through `FlowTestBase`.

## Helpers

| Helper | Does |
|---|---|
| `HttpClientExtensions` | `UploadAsync`, `PutIndexValueAsync`, `ReadJsonAsync` (fails with the body when the status is not a success). |
| `ProblemAssert.IsProblemAsync` | Checks the status, the ProblemDetails content type and part of the message. |
| `ZipBuilder.Create` | Builds a zip in memory from paths and text; a path ending in `/` is a folder. |
| `TestFiles` | Paths to the files in `TestData/`. |

## Rule tags

Tests that prove a requirement carry `[Trait("Rule", "R#")]`.

| Tag | Rule |
|---|---|
| R1 | Pick a cabinet, a writable text field and a folder. |
| R2 | Each top-level entry becomes one document. |
| R3 | A file becomes a document with one part. |
| R4 | A subfolder's files become parts, in a set order. |
| R5 | Deeper items are skipped and reported. |
| R6 | An empty subfolder becomes a document with no parts. |
| R7 | The title is the file or folder name. |
| R8 | The field is set to the folder name. |
| R9 | The report says what was stored, skipped and failed. |
| R10 | Uploads are untrusted. |
| R11 | Names may contain any Unicode characters. |
| R12 | A failed operation leaves nothing behind. |
| R13 | Field types and limits are enforced. |
| R14 | Times are UTC. |

Run one rule with `dotnet test --filter "Rule=R8"`.

## Conventions

- Test names are plain sentences: `A_damaged_file_is_reported_and_the_rest_is_stored`.
- Every rejected upload also checks that no document and no file was stored.
- Flow tests go through HTTP; they do not call handlers directly.
