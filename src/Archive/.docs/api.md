# Archive.Api

The HTTP host of the Archive service. It loads the file cabinets, stores uploaded folders as documents, and serves documents, their parts and their index values.

## References

- `Archive.Core`: handlers, rules, exceptions.
- `Archive.Data`: database and file storage.
- `Shared`: response models, `ServiceException`, `LogMethodError`.
- Packages: OpenAPI, Swagger UI, Npgsql EF Core.

## Layout

| Folder | What it holds |
|---|---|
| `Program.cs` | Builds the app and the request pipeline. |
| `Startup/ServiceRegistration.cs` | Reads and checks settings, registers every service. |
| `Startup/DatabaseMigrator.cs` | Applies database migrations when the service starts. |
| `Controllers/` | `CabinetsController`, `DocumentsController`. |
| `Cabinets/` | `CabinetFileLoader` (reads `cabinets.json`), cabinet mappings. |
| `Documents/` | Document mappings to the `Shared` models. |
| `Uploads/` | Upload response models, mappings, `UploadSizeLimitFilter`. |
| `Errors/ProblemExceptionHandler.cs` | Turns exceptions into ProblemDetails. |
| `HealthChecks/DatabaseHealthCheck.cs` | Checks that the database answers. |

## Program.cs

In order:

1. `builder.RegisterServices()`. Missing or invalid settings stop the service at startup.
2. Logs how many cabinets were loaded.
3. Exception handler, status code pages, CORS.
4. OpenAPI at `/openapi/v1.json` and Swagger UI at `/swagger` ("Archive API").
5. Controllers.
6. Health: `/health/live` (the process is up, no checks) and `/health/ready` (the database answers).

## Settings

All are required. A missing one stops startup with a message naming the environment variable to set (for example `Set Storage__FilesPath`).

| Setting | Meaning |
|---|---|
| `ConnectionStrings:Archive` | PostgreSQL connection string. |
| `Cabinets:Path` | Path to `cabinets.json`. |
| `Storage:FilesPath` | Folder where part files are saved. |
| `Paging:MaxPageSize` | Largest page the document query returns (1 or more; 5 by default). |
| `Upload:MaxZipBytes` | Largest zip accepted (1 MB by default). |
| `Upload:MaxUncompressedBytes` | Largest total unpacked size (5 MB by default). |
| `Upload:MaxEntries` | Most entries a zip may have (1000 by default). |
| `Cors:AllowedOrigins` | Allowed browser origins. Empty means any origin. |

`cabinets.json` is read strictly: every property is required and field types are `text`, `number` or `date`. A bad file stops startup.

## Endpoints

All errors are ProblemDetails (`application/problem+json`) with `status` and `detail`.

| Method and path | Does | Answers |
|---|---|---|
| `GET /cabinets` | Lists the cabinets with their fields. | 200 |
| `GET /cabinets/{cabinet}` | One cabinet; the name ignores case. | 200, 404 |
| `POST /cabinets/{cabinet}/uploads` | Stores a zipped folder. Multipart form: `field` (a writable text field) and `file` (the zip). | 200 report, 400, 404, 413, 500 with report |
| `GET /cabinets/{cabinet}/documents?from&to&page&pageSize` | Documents stored between `from` and `to`, both included, oldest first. `page` starts at 1 (default 1), `pageSize` defaults to 5 and is capped at `Paging:MaxPageSize`. | 200 page, 400, 404 |
| `GET /documents/{id}` | One document with its parts in order. | 200, 404 |
| `GET /documents/{id}/parts/{partId}/content` | The part's bytes as `application/octet-stream`, with its file name. | 200, 404, 500 if the file is missing or the wrong size |
| `PUT /documents/{id}/index-values/{field}` | Sets one index value. Body `{ "value": ... }`. Other fields are kept. | 200 document, 400, 404 |

### Dates

`from` and `to` must be UTC: `yyyy-MM-ddTHH:mm:ssZ`, or `yyyy-MM-dd`. A date-only `from` means 00:00:00 and a date-only `to` means 23:59:59, so a date covers the whole day.

### Upload report

The 200 answer lists:

- `folderName` and `field`: the value written and the field it went to;
- `stored`: each document with its id, title and part count;
- `skipped`: paths that were ignored, with the reason;
- `failed`: files that could not be stored, with the reason;
- `message`: set when something failed, otherwise null.

If the upload stops because of a server error, or nothing could be stored, the answer is a 500 ProblemDetails with the same report in a `report` property.

### Upload size limit

`UploadSizeLimitFilter` caps the request body at `Upload:MaxZipBytes` plus 64 KB for the form. A bigger body is 413; a broken form is 400.

## Errors

`ProblemExceptionHandler` maps:

- an Archive exception to its own status and message;
- a bad HTTP request to its status;
- anything else to 500 "Something went wrong on the server. Please try again later." The real error is only logged.

5xx is logged as an error, anything else as a warning.

## Logging

- Console, one line per entry, with scopes and UTC timestamps.
- Every controller action logs `Start <Class>.<Method> with params ...`, then `End ... in <ms> ms` with result counts.
- On failure it logs `Error ...` through `LogMethodError` and rethrows; the exception handler answers.

## Database and startup order

- `DatabaseMigrator` applies migrations in the background and retries every 5 seconds while the database is not reachable, so the service can start before the database.
- Database calls retry up to 3 times.
- `/health/ready` checks the database with `SELECT 1` and a 3 second timeout.

## Running

- Docker: port 8080 inside the container; `cabinets.json` is copied to `/app/config`; files go to `/data/files`; runs as user `app`.
- Local: `http://localhost:5099`, database on `localhost:5433`, cabinets from `fixtures/cabinets.json`, files in `.data/files` at the repo root.

## Conventions

- Controllers stay thin: log, call a handler, map the result to a `Shared` model.
- JSON uses camelCase; enums are camelCase strings.
- Every endpoint declares its answers with `ProducesResponseType`, so OpenAPI shows them.
