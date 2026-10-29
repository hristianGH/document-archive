# Export.Tests

xUnit tests for the Export service. They run without Docker, the Archive or a network.

## References

- `Export.Api`, `Export.Core`, `Export.Data`.
- Packages: xUnit, `Microsoft.AspNetCore.Mvc.Testing` (in-memory server), `FakeTimeProvider`.

## Layout

| Folder | What it tests |
|---|---|
| `Flows/ExportFlowTests` | Successful exports: zip content, dates, marker, paging, streaming. |
| `Flows/ExportRequestFlowTests` | Requests refused before any zip bytes. |
| `Flows/ExportFailureFlowTests` | The Archive down, slow or failing at each stage, and what is left marked. |
| `Clients/ArchiveClientTests` | How `ArchiveClient` turns each Archive answer into a result or an error. |
| `Rules/` | `ExportMarker`, `MarkerFieldValidator`, `ZipLayout`, `ZipNameAllocator`. |
| `Infrastructure/` | Test helpers (below). |
| `TestData/` | `cabinets.json`. |

## How flow tests run

`ExportApiFactory` starts the real Export API in memory. Only two things are replaced:

- the Archive, by `FakeArchive`: the Archive's HTTP API in memory, with the same routes, page size cap (2 by default), range filter, order and ProblemDetails errors;
- the clock, by `FakeTimeProvider`, fixed at 2026-10-28 09:30 UTC. Both Archive timeouts run on this clock, so they fire only when a test moves it forward.

The real retry and timeout setup stays in place.

## Helpers

| Helper | Does |
|---|---|
| `FakeArchive.SeedStandard` | Invoices documents from 2026-10-01 to 2026-10-28 in export order, plus documents just outside the range and in Contracts. |
| `FakeArchive.Add` | Adds one document with parts. |
| `FakeArchive.Override` | Replaces the answer to chosen requests (an error, or a thrown exception). |
| `FakeArchive.Hold` | Delays an answer until the test lets it go. |
| `FakeArchive.BreakingContent` / `StallingContent` | A part that breaks or stops halfway. |
| `FakeArchive.Requests` / `Marks` | What the Export asked for and which markers it set. |
| `HttpClientExtensions` | `ExportAsync`, `StartExportAsync` (returns once headers arrive), `ReadZipAsync`. |
| `ProblemAssert.IsProblemAsync` | Checks the status, the ProblemDetails content type and part of the message. |

## Rule tags

Tests that prove a requirement carry `[Trait("Rule", "X#")]`.

| Tag | Rule |
|---|---|
| X1 | Inputs: cabinet, marker field, from and to dates. |
| X2 | One part becomes a file. |
| X3 | Several parts become a folder. |
| X4 | No parts become an empty folder. |
| X5 | Documents with the same title both survive. |
| X6 | Exported documents get `Exported on yyyy-MM-dd`. |
| X7 | The marker is not a filter and is overwritten. |
| X8 | The client downloads the zip, possibly from another host. |
| X9 | Every page is exported. |
| X10 | Exports may be larger than memory. |
| X11 | Times are UTC. |
| X12 | What is left if the export fails halfway. |
| X13 | Field types and limits are enforced. |
| X14 | Names may contain any Unicode characters. |
| X15 | The Archive may be briefly unavailable. |

Run one rule with `dotnet test --filter "Rule=X12"`.

## Conventions

- Test names are plain sentences: `A_part_breaking_halfway_breaks_the_download_and_marks_nothing`.
- Failure tests check both the answer and which documents were marked.
- Flow tests go through HTTP; they do not call the handler directly.
