# Export.Data

The Export's connection to the Archive. `ArchiveClient` calls the Archive's HTTP API and turns its answers into `Export.Core` models and errors.

## References

- `Export.Core`: `IArchiveClient` and the models it returns.
- `Shared`: the Archive's response models.
- Package: Polly (to recognise a timed-out attempt).

## Layout

| Item | What it is |
|---|---|
| `Clients/ArchiveClient` | Implements `IArchiveClient`. |
| `Clients/ArchiveMappings` | `Shared` responses to `Export.Core` models. |

## Calls

| Method | Archive call |
|---|---|
| `GetCabinetAsync` | `GET /cabinets/{cabinet}` |
| `GetDocumentIdsAsync` | `GET /cabinets/{cabinet}/documents?from&to&page&pageSize=1000`. The Archive lowers the page size to its maximum and states the size it used. |
| `GetDocumentAsync` | `GET /documents/{id}` |
| `CopyPartContentAsync` | `GET /documents/{id}/parts/{partId}/content`, copied straight to the zip. |
| `SetIndexValueAsync` | `PUT /documents/{id}/index-values/{field}` with the marker. |

Path and query values are URL-escaped, so a `+` in a date arrives as a `+`.

## Retries and timeouts

Set up in `Export.Api` (see its settings):

- Each call is retried on network errors, timeouts and server errors, with a fixed wait between attempts.
- Each attempt has its own timeout.
- Reading a part's bytes has a separate timeout, because the attempt timeout ends once the headers arrive.

## How answers are turned into errors

| Archive answer | Error | Status |
|---|---|---|
| Success | — | — |
| 4xx | `ArchiveRejectedException`, with the Archive's status and `detail` (or a plain sentence if there is none) | the Archive's |
| 5xx after all retries | `ArchiveFailedException` | 502 |
| Could not connect after all retries | `ArchiveUnavailableException` | 503 |
| Every attempt timed out, or a part stalled | `ArchiveTimeoutException` | 504 |
| The caller cancelled | passed on as a cancellation | — |

## Conventions

- Every method logs Start, Error and End with its parameters and results.
- Only the client knows the Archive's routes; `Export.Core` never sees HTTP.
