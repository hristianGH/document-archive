# Architecture

How the system fits together: services, layout, conventions, data, endpoints, errors, startup and the upload and export rules. Details of each project's code are in its own `.docs` folder (see Repository layout). Todos and possible additions are in [todos.md](todos.md).

## Services (docker compose)

`docker-compose.yml`, project name `document-archive`.

| Container | Technology | Purpose | Host port |
|---|---|---|---|
| `postgres` | PostgreSQL 17 (alpine) | The Archive's records: documents, index values, part metadata. Volume `pgdata`. | none |
| `archive` | ASP.NET Core | Archive service. Loads `cabinets.json`, stores documents in Postgres and part files on volume `files`, applies the upload and field rules. | `ARCHIVE_PORT` (8081) |
| `export` | ASP.NET Core | Export service. No database; reads and writes the Archive only through its HTTP API. | `EXPORT_PORT` (8082) |
| `web` | React + TypeScript + Vite, served by nginx | Web client. nginx serves the built files and proxies `/api/archive` and `/api/export` to the services. | `WEB_PORT` (8080) |

- Start order: `archive` waits for `postgres` to be healthy, `export` waits for `archive` to be healthy.
- Health checks: `postgres` with `pg_isready`, `archive` on `/health/ready`, `export` on `/health/live`.
- One Dockerfile per service, multi-stage (`sdk:10.0` → `aspnet:10.0`), build context is the repo root, runs as the non-root `app` user on port 8080.
- `POSTGRES_PASSWORD` has no default; compose stops with a message if it is missing. Settings to supply are in `.env.example`.
- Both volumes survive `docker compose down` / `up`. Only `down -v` removes them.
- The e2e test stack is separate: `docker-compose.e2e.yml` (see E2E tests).

## Repository layout

```
document-archive/
  DocumentArchive.sln          solution folders: Shared, Archive, Export
  docker-compose.yml           the app
  docker-compose.e2e.yml       the UI test stack, started on its own
  .env.example                 settings to supply; copy to .env (git-ignored)
  .docs/                       this file and todos.md
  fixtures/cabinets.json
  src/
    Shared/                    models, logging helper, exception base
    Archive/
      .docs/                   api.md, core.md, data.md, tests.md
      Archive.Api/  Archive.Core/  Archive.Data/  Archive.Tests/
    Export/
      .docs/                   api.md, core.md, data.md, tests.md
      Export.Api/  Export.Core/  Export.Data/  Export.Tests/
    web/
      .docs/                   app.md, e2e.md
      e2e/                     Playwright UI tests
```

**Documentation (`.docs`)**

Each project area has a `.docs` folder with one file per project. These describe what the code does: layout, settings, endpoints, rules, errors, logging and conventions. Agents read them to learn a project before working on it.

| Folder | Files |
|---|---|
| `src/Archive/.docs/` | `api.md`, `core.md`, `data.md`, `tests.md` |
| `src/Export/.docs/` | `api.md`, `core.md`, `data.md`, `tests.md` |
| `src/Shared/.docs/` | `shared.md` |
| `src/web/.docs/` | `app.md`, `e2e.md` |

**Project references**

- `*.Api` → its Core, its Data, Shared.
- `*.Data` → its Core. `Export.Data` also → Shared (reads the Archive's models).
- `*.Core` → Shared, only for `LogMethodError` and `ServiceException`, never its models.
- `*.Tests` → its Api, Core, Data.
- Each `.csproj` sets `net10.0`, `Nullable` and `ImplicitUsings`. There is no `Directory.Build.props`.
- Tests use xUnit.

## .NET conventions

- **Three projects per service**, not shared with the other service:
  - **Api**: host, controllers, settings, error handling.
  - **Core**: rules and handlers. No I/O.
  - **Data**: the database and files (Archive), the Archive HTTP client (Export).
- **Controllers** for endpoints. They stay thin: log, call a handler, map the result.
- **Shared** holds only what both services need:
  - `Shared/Models/`: `Cabinets/`, `Documents/`, `Paging/`. The Archive returns them, the Export reads them.
  - `Shared/Logging/LoggerExtensions` (`LogMethodError`).
  - `Shared/Exceptions/ServiceException` (an exception that carries its HTTP status).
- **Models:**
  - Named `…Response` (returned) or `…Request` (received). No `Dto` suffix.
  - Plain classes with `{ get; set; }` and defaults (`string.Empty`, `[]`), not records.
  - Mapping: entity → Core model (Data) → response (Api). Core never uses Shared's models.
- **JSON:** camelCase; enums are camelCase strings (`"type": "text"`).
- **Primary constructors** for services (controllers, handlers, repositories, stores, filters); parameters used directly, no copied `_fields`.
- **`async` / `await`** rather than returning another method's `Task`.
- **Time:** from an injected `TimeProvider`, always UTC, so tests use a fake clock.
- **Settings** are read and checked at startup in `Startup/ServiceRegistration.cs`. A missing or invalid setting stops the service with the variable to set.
- **Logging:** console, one line per entry, UTC timestamps, scopes on (`TraceId` matches `traceId` in errors).
  - Every public method of controllers, handlers, the repository and the file store logs `Start Class.Method with params …`, `Error …` in `catch` (then `throw;`), and `End …` with counts in `finally`. Controllers add the time in ms.
  - Params are ids, names, counts and sizes only. Never file content, index values or secrets.
  - Api and Core log errors with `LogMethodError`: a 4xx `ServiceException` or a cancelled request is a Warning, anything else an Error.
- **Line endings:** LF everywhere (`.gitattributes`, `.editorconfig`).

## Data model (Postgres)

Only the Archive has a database. Mapping is in `Archive.Data/ArchiveDbContext.cs`; entities in `Archive.Data/Entities/`.

**Documents**

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | UUIDv7, set by Core |
| `Cabinet` | text | Cabinet `name` from `cabinets.json` |
| `Title` | text | Name of the uploaded file or folder, kept exactly |
| `StoredAt` | timestamptz | UTC, whole seconds, set once at upload |
| `ModifiedAt` | timestamptz | UTC, whole seconds, set at upload and on every index value change |
| `IndexValues` | jsonb | Field name → value |

Index: `(Cabinet, StoredAt, Id)`, for the date filter and stable paging.

**Parts**

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | UUIDv7, set by Core |
| `DocumentId` | uuid → Documents | Cascade delete |
| `Position` | int | 0-based order in the document |
| `FileName` | text | Original file name, kept exactly |
| `Size` | bigint | Bytes |
| `FilePath` | text | `{documentId}/{partId}` on the files volume; never returned by the API |

Unique indexes: `(DocumentId, Position)` and `FilePath`. A document with no parts has no part rows.

**Rules for these columns**

- **Part order:** natural sort by file name, ignoring case (`page1`, `page2`, `page10`). Names that sort equal keep zip order. See `UploadPlanner.ToPlannedDocument`.
- **Each document** gets its own `StoredAt` when it is saved.
- **No `null` values.** A text field can be emptied with `""`. A field never set has no key in `IndexValues`.
- **Index value types** (checked in `Archive.Core/Validation/IndexFieldValidator.cs`):

  | Field type | JSON | .NET |
  |---|---|---|
  | text | string | `string` |
  | number | number | `decimal` |
  | date | string `yyyy-MM-dd` | `DateOnly` |

- `maxLength` counts UTF-16 code units (`string.Length`).
- Cabinets are not stored in the database; they come from `cabinets.json`, read once at startup.
- Part files are not in the database. A file is written first, then its row; if the row fails, the file is deleted.

## Archive service

Code details: `src/Archive/.docs/`.

### Endpoints

| Method and path | Does | Used by |
|---|---|---|
| `GET /cabinets` | Lists the cabinets with their fields. | Client |
| `GET /cabinets/{cabinet}` | One cabinet; the name ignores case. | Client, Export |
| `POST /cabinets/{cabinet}/uploads` | Stores a zipped folder. Form: `field`, `file`. Returns the report. | Client |
| `GET /cabinets/{cabinet}/documents?from&to&page&pageSize` | Documents with `StoredAt` in a UTC range, both ends included, paged, no parts. | Client, Export |
| `GET /documents/{id}` | One document with its parts. | Client, Export |
| `GET /documents/{id}/parts/{partId}/content` | One part's bytes. | Export |
| `PUT /documents/{id}/index-values/{field}` | Sets one writable field. Body `{ "value": … }`. | Export (marker) |
| `GET /health/live` | The process is up. | Docker |
| `GET /health/ready` | The database answers (`SELECT 1`), otherwise 503. | Docker |

Controllers: `Archive.Api/Controllers/CabinetsController.cs`, `DocumentsController.cs`. Health is mapped in `Program.cs`.

- **Dates:** `from` / `to` are UTC, `yyyy-MM-dd` or `yyyy-MM-ddTHH:mm:ssZ`. A date-only `to` means 23:59:59. Parsed by `Archive.Core/Dates/UtcInstant.cs`.
- **Paging:** `page` starts at 1, `pageSize` defaults to 5 and is capped at `Paging:MaxPageSize`. Order `StoredAt`, then `Id`. Answer `{ items, page, pageSize, totalCount }`.
- **Same value:** a `PUT` with the value already stored writes nothing and returns 200, so a retry is safe.

### Upload format

One zip per upload.

- **Folder name:** if the zip holds a single top-level folder, its name; otherwise the zip's file name without `.zip`. Written to the chosen field on every document.
- Each top-level entry is one document; files one level down are its parts; anything deeper is skipped and reported.
- System files (`.DS_Store`, `Thumbs.db`, `desktop.ini`, `__MACOSX/`) and symlinks are dropped silently.
- Rejected before anything is stored: unsafe paths (`../`, absolute, drive letters), names that are not valid UTF-8, too many entries, too large, nothing to store.
- Limits (settings `Upload:*`): zip 1 MB, unpacked 5 MB (413); 1000 entries (400).
- A file whose unpacked size differs from the zip's label fails as damaged; the rest continue.
- Answer: 200 with the report (`stored`, `skipped`, `failed`, `message`). A server fault stops the upload: 500 with the same report in `report`.

Code: `Archive.Core/Handlers/UploadHandler.cs`, `Archive.Core/Uploads/UploadPlanner.cs`, `Archive.Api/Uploads/UploadSizeLimitFilter.cs`.

### Startup and migrations

- `Archive.Api/Startup/DatabaseMigrator.cs` applies EF migrations when the service starts. While the database cannot be reached it retries every 5 s; any other failure stops the service.
- While running, database calls retry 3 times, at most 5 s apart (`EnableRetryOnFailure`). A request that still fails gets 500; `/health/ready` gives 503 until the database is back.
- Startup stops on a missing setting or a bad `cabinets.json`.

## Export service

Code details: `src/Export/.docs/`.

### Endpoints

| Method and path | Does | Used by |
|---|---|---|
| `POST /exports` | Checks the request, streams the zip, then writes the markers. | Client |
| `GET /health/live` | The process is up. Does not check the Archive. | Docker |

Controller: `Export.Api/Controllers/ExportsController.cs`.

- **Request:** `{ "cabinet": "Invoices", "field": "Batch", "from": "2026-10-01", "to": "2026-10-28" }`. Dates go to the Archive unchanged.
- **Answer:** `application/zip`, file name `{cabinet}-export-{yyyy-MM-dd}.zip` in `Content-Disposition` (exposed through CORS).

### How an export runs

In `Export.Core/Handlers/ExportHandler.cs`:

1. `PrepareAsync`, before any bytes are sent. Checks the cabinet, the marker field and the first page. Any failure is a normal error answer.
2. `WriteAsync` sends the zip while building it:
   1. every page, every document, every part copied straight from the Archive into the zip;
   2. after all documents are in the zip, each one is marked;
   3. only then the zip is finished.

If it fails halfway:

| Fails while | State left |
|---|---|
| Preparing | Nothing sent, nothing marked; error answer |
| Writing documents | Nothing marked; the download breaks |
| Marking | Documents before the failure are marked; the download breaks |

A broken download is never a usable zip. Running the export again fixes every case, because the marker is not a filter.

### Archive calls

In `Export.Data/Clients/ArchiveClient.cs`, set up in `Export.Api/Startup/ServiceRegistration.cs`.

| Setting | Default | Meaning |
|---|---|---|
| `Archive:BaseUrl` | `http://archive:8080` in Docker | Required. |
| `Archive:MaxRetries` | 3 | Retries for a call that cannot connect, times out or gets 5xx. |
| `Archive:RetryDelay` | 5 s | Wait between retries. |
| `Archive:AttemptTimeout` | 10 s | Longest wait for one answer. |
| `Archive:ContentTimeout` | 90 s | Longest time to copy one part. |

After the retries: a 4xx from the Archive is passed on as is, 5xx → 502, unreachable → 503, timeout → 504.

### Startup

No database, nothing to migrate. Starts after the Archive is healthy, but does not need it to start. Settings are checked at startup.

## Errors (both services)

Every error is ProblemDetails (RFC 9457, `application/problem+json`) on every endpoint.

- `Errors/ProblemExceptionHandler.cs` in each Api turns exceptions into ProblemDetails:
  - a `ServiceException` (`ArchiveException`, `ExportException`) → its own status and message;
  - a bad HTTP request → its status;
  - anything else → 500 "Something went wrong on the server. Please try again later."
- `AddProblemDetails()` and `UseStatusCodePages()` give bare status codes the same shape.
- Fields: `status`, `detail` (the message the client shows), `traceId` (links to the logs), `errors` (per field, from model binding), `report` (upload only).
- 5xx is logged as an Error, 4xx as a Warning. Stack traces never reach the client.

## OpenAPI

Every endpoint of both services is documented in OpenAPI, with its answers declared through `ProducesResponseType`. Available in every environment, including the containers:

- `/openapi/v1.json`: the document.
- `/swagger`: Swagger UI.

## Rules

Each upload and export rule, where it is checked, and its test tag. Run one rule's tests with `dotnet test --filter "Rule=R8"`. Test details: `src/Archive/.docs/tests.md`, `src/Export/.docs/tests.md`.

### Upload (R1–R14)

Code is in `Archive.Core` unless noted.

| Tag | Rule | Checked in |
|---|---|---|
| R1 | Pick a cabinet, a writable text field and a folder. | `UploadHandler.UploadAsync`; `IndexFieldValidator.GetWritableField`, `EnsureText` |
| R2 | Each top-level entry is one document. | `UploadPlanner.FindSingleRootFolder`, `GroupAndOrder` |
| R3 | A file is a document with one part. | `UploadPlanner.GroupAndOrder` |
| R4 | A subfolder is one document; its files are the parts, in a documented order. | `UploadPlanner.GroupAndOrder`, `ToPlannedDocument` (natural order) |
| R5 | Anything deeper is skipped and reported. | `UploadPlanner.SkipNested` |
| R6 | An empty subfolder is a document with no parts. | `UploadPlanner.GroupAndOrder` |
| R7 | The title is the file or folder name. | `UploadPlanner.GroupAndOrder` |
| R8 | The chosen field gets the folder name, the same for every document. | `UploadPlanner.GetFolderName`; `IndexFieldValidator.ValidateText` (fits `maxLength`) |
| R9 | The report says what was stored, skipped and failed. | `UploadHandler` (`UploadReport`, `UploadFailedException`); `Archive.Api/Errors/ProblemExceptionHandler` (`report`) |
| R10 | Uploads are untrusted. | `Archive.Api/Uploads/UploadSizeLimitFilter`; `UploadHandler` (size, entry count, damaged file); `UploadPlanner.CheckInvalidName`, `DropSystemFilesAndSymlinks` |
| R11 | Names may be any Unicode. | `UploadPlanner.CheckInvalidName` (valid UTF-8); names kept exactly; files saved by id |
| R12 | Nothing left behind. | `UploadHandler` (deletes a failed document's files); `Archive.Data/Repositories/DocumentRepository.AddAsync` |
| R13 | Field types and limits are enforced. | `IndexFieldValidator` (`GetWritableField`, `EnsureText`, `ValidateText`, `ParseValue`) |
| R14 | Different time zones. | `UtcInstant` (`ToWholeSeconds`, `TryParseFrom`, `TryParseTo`); times from `TimeProvider` in UTC |

### Export (X1–X15)

Code is in `Export.Core` unless noted.

| Tag | Rule | Checked in |
|---|---|---|
| X1 | Pick a cabinet, a marker field and two dates; dates are inclusive, filter on `StoredAt`. | `ExportHandler.PrepareAsync`; the date rule in the Archive (`UtcInstant`) |
| X2 | One part → a file. | `ZipLayout.Plan` |
| X3 | Several parts → a folder with one file per part. | `ZipLayout.Plan` |
| X4 | No parts → an empty folder. | `ZipLayout.Plan` |
| X5 | Same titles both survive, by a documented rule. | `ZipNameAllocator` (`AllocateFile`, `AllocateFolder`) |
| X6 | The field is set to `Exported on yyyy-MM-dd`. | `ExportMarker.For` (in `ExportHandler.PrepareAsync`); written in `ExportHandler.WriteAsync` |
| X7 | The marker is not a filter and is overwritten. | `ExportHandler` (no filter on the marker) |
| X8 | Download through the client, from any host. | `Export.Api/Controllers/ExportsController`; CORS in `Export.Api/Startup/ServiceRegistration` |
| X9 | Works with any Archive page size. | `ExportHandler.WriteAsync` (pages by the size the Archive states) |
| X10 | Larger than memory. | `ExportHandler.WriteAsync`; `Export.Data/Clients/ArchiveClient.CopyPartContentAsync` (streamed) |
| X11 | Different time zones. | `ExportHandler.PrepareAsync` (UTC date from `TimeProvider`) |
| X12 | A safe state after a failure halfway. | `ExportHandler.WriteAsync` (marks only after every document is written) |
| X13 | Field types and limits. | `MarkerFieldValidator.GetMarkerField` |
| X14 | Any Unicode in names. | `ZipLayout`, `ZipNameAllocator` (UTF-8 entry names, kept exactly) |
| X15 | The Archive may be briefly unavailable. | `Export.Data/Clients/ArchiveClient`; retries and timeouts in `Export.Api/Startup/ServiceRegistration` |

## Web client

React + TypeScript + Vite in `src/web`, served by nginx in one container. Pages: cabinets, upload, export. `src/` holds `api/` (calls), `service/` (request flow), `view/` (pages) and `components/`.

- Service addresses come from `config.json`, written by nginx at container start from `ARCHIVE_URL` and `EXPORT_URL`, so the same image runs with any settings.
- Shows when a request is in flight and blocks starting it twice. Rules stay on the server; the client shows the server's messages.

Details: `src/web/.docs/app.md`.

## E2E tests

Playwright tests in `src/web/e2e` drive the real UI in Chromium against a stack of their own.

- **Stack:** `docker-compose.e2e.yml`, project `document-archive-e2e`, own volumes and ports (web 18080, archive 18081, export 18082, postgres 15433). An `e2e` container runs the tests once the stack is up. The stack stays up afterwards for debugging.
- **Layout:**

  | Item | Holds |
  |---|---|
  | `e2e/*.spec.ts` | `navigation`, `cabinets`, `upload`, `export` |
  | `e2e/fixtures/` | The zips the tests upload |
  | `e2e/support/` | Helpers for the UI, zips, the Archive API and the stack |
  | `e2e/global-setup.ts` | Waits for the stack to be ready |
  | `e2e/Dockerfile` | The runner image |

- **Data:** uploaded and exported through the UI, not through the API.
- **Commands** (from `src/web`): `npm run e2e` (fresh stack and run), `npm run e2e:down`, `npx playwright show-report`.

Details: `src/web/.docs/e2e.md`.

## UUIDv7 ids

Documents and parts use `Guid.CreateVersion7()`.

- The first 48 bits are the creation time in milliseconds; the rest is random.
- New ids sort roughly by time, so Postgres adds them at the end of the index instead of on random pages.
- Made in Core before anything is written, because the part file path `{documentId}/{partId}` is needed before the row exists.
- Trade-off: an id shows when it was made. Not a concern here.
