# Todos and possible additions

Not done. Each item says what it is and why it is not done yet. See [architecture.md](architecture.md) for what exists.

## Todos

- **Reuse the e2e zips.** `src/web/e2e/fixtures/*.zip` could also feed the .NET tests and the README demo, instead of zips built in code.
- **Correlation id across services.** One id passed from the web client through the Archive and the Export, in every log line. Today each service has its own `TraceId`.
- **Cabinet list mapping.** `ToResponse()` for cabinets lives in `Archive.Api/Cabinets/CabinetMappings`; decide whether it stays there.
- **Fewer, narrower models.** Some request, response and Core models overlap. Several could be narrowed to the fields they need, or reused instead of repeated.

## Possible additions

### Archive

- **Reject names Windows cannot use.** Today names are kept exactly, so a title like `report: final.txt` or `CON.txt` may fail to unpack on Windows. Option: 400 at upload, naming the entry and the character (`< > : " / \ | ? *`, control characters, a trailing dot or space, reserved names like `CON`, `NUL`, `COM1`).
- **CRC check on each unpacked file.** Today only the size is checked. A CRC-32 check while copying would also catch changed bytes. .NET 11 does this itself.
- **Unpack the upload to disk.** Today the zip is read from ASP.NET's upload stream. Unpacking to a temp folder needs the same name checks and cleanup on every path.
- **Concurrency check on index values.** Today two writes to the same field: last write wins. Option: a concurrency token (Postgres `xmin`) or `If-Match` / ETag.
- **Delete document endpoint.** Removes the document, its parts and its files.
- **Chunked upload** for material over the limits (1 MB zip, 5 MB unpacked). Chunk size as a setting; chunks go to a temp file on the volume; abandoned uploads expire and are cleaned up; the same report at the end.
- **Orphan file cleanup.** A hard crash between writing a file and its row leaves a file with no row. A startup or background job could remove them.
- **Separate migration step.** Today the Archive applies migrations when it starts. A migration project or container could do it instead.

### Export

- **Export as a background job.** `POST /exports` → 202 with an id; `GET /exports/{id}` shows progress; `GET /exports/{id}/download` can be retried. Gives progress and a clear error after a failure halfway. Costs a worker, job state, a stored zip with cleanup, and polling in the client.
- **ExportRun table.** Records each export (status, documents marked), to help after a failure halfway.
- **The user's time zone for dates.** Today a day is a UTC day, and the marker date is the UTC date. Option: the client sends its time zone (`Europe/Sofia`) and the Export converts the user's days to UTC. Costs time zone data in the image and daylight-saving edge cases.
- **Remove `SyncBufferingStream`** when the projects move to .NET 11, which fixes the zip's blocking write.

### Operations

- **Metrics and observability.** Today there are only console logs. Options: OpenTelemetry traces and metrics (request times, upload and export counts, Archive retries), sent to a collector with dashboards and alerts.
- **Volume cleanup job.** A scheduled job that checks both volumes stay in step: removes files with no row and reports rows whose file is missing.
- **Backups** of both volumes together (Postgres data and part files).
- **Key provider for secrets** (Azure Key Vault, Docker or Kubernetes secrets) instead of `.env`.
- **Storage container** (MinIO, S3-compatible) instead of a volume on the Archive. Not chosen: an extra container and credentials.
