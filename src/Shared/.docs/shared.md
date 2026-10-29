# Shared

A small library used by both services. It holds the HTTP contract between the client, the Archive and the Export, the base error type, and one logging helper. It has no service logic.

## References

- Package: logging abstractions.
- Used by: `Archive.Api`, `Archive.Core`, `Export.Api`, `Export.Core`, `Export.Data`.

## Layout

| Folder | What it holds |
|---|---|
| `Models/Cabinets/` | `CabinetResponse`, `IndexFieldResponse`, `FieldType`. |
| `Models/Documents/` | `DocumentResponse`, `DocumentListItemResponse`, `PartResponse`, `SetIndexValueRequest`. |
| `Models/Exports/` | `ExportRequest`. |
| `Models/Paging/` | `PagedResponse<T>`. |
| `Exceptions/` | `ServiceException`. |
| `Logging/` | `LoggerExtensions`. |

## Models

The JSON shapes the services send and receive.

| Model | Shape |
|---|---|
| `CabinetResponse` | `name`, `displayName`, `fields` |
| `IndexFieldResponse` | `name`, `label`, `type` (`text`, `number`, `date`), `maxLength` (null means no limit), `writable` |
| `DocumentListItemResponse` | `id`, `cabinet`, `title`, `storedAt`, `modifiedAt`, `indexValues`, `partCount` |
| `DocumentResponse` | the same, with `parts` instead of `partCount` |
| `PartResponse` | `id`, `position`, `fileName`, `size` |
| `SetIndexValueRequest` | `value`: a string for text (`""` allowed), a number for number, `yyyy-MM-dd` for date; `null` is refused |
| `ExportRequest` | `cabinet`, `field`, `from`, `to`, all required |
| `PagedResponse<T>` | `items`, `page`, `pageSize` (the size actually used), `totalCount` |

Times are UTC. `indexValues` is a JSON object with one key per field.

Property descriptions on the request models appear in the OpenAPI documents of both services.

## ServiceException

The base of every Archive and Export error. It carries the HTTP status the error is answered with, so each service's exception handler can turn it into ProblemDetails.

## LogMethodError

`logger.LogMethodError(exception, message, args)` logs a failed method at the right level:

- **Warning:** the caller's fault (a `ServiceException` with a 4xx status) or a cancelled request.
- **Error:** anything else.

The exception is always included.

## Conventions

- Only add what both services, or a service and its client, need to agree on.
- Models are plain classes with no behaviour.
- Each service maps these models to its own Core models; Core logic does not depend on them.
