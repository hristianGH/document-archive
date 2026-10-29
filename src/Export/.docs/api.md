# Export.Api

The HTTP host of the Export service. It builds a zip of the documents a cabinet stored in a date range, streams it to the caller, and marks each exported document. It has no database; it reads and writes everything through the Archive's HTTP API.

## References

- `Export.Core`: the export handler, rules, exceptions.
- `Export.Data`: the Archive client.
- `Shared`: request and response models, `ServiceException`, `LogMethodError`.
- Packages: OpenAPI, Swagger UI, `Microsoft.Extensions.Http.Resilience` (retries and timeouts).

## Layout

| Item | What it holds |
|---|---|
| `Program.cs` | Builds the app and the request pipeline. |
| `Startup/ServiceRegistration.cs` | Reads and checks settings, registers every service and the Archive client. |
| `Controllers/ExportsController.cs` | `POST /exports`. |
| `Errors/ProblemExceptionHandler.cs` | Turns exceptions into ProblemDetails. |

## Program.cs

In order:

1. `builder.RegisterServices()`. Missing or invalid settings stop the service at startup.
2. Logs the Archive base URL.
3. Exception handler, status code pages, CORS.
4. OpenAPI at `/openapi/v1.json` and Swagger UI at `/swagger` ("Export API").
5. Controllers.
6. Health: `/health/live` (the process is up, no checks). There is no ready check.

## Settings

| Setting | Meaning | Default |
|---|---|---|
| `Archive:BaseUrl` | The Archive's address, http or https. | `http://archive:8080` in Docker, `http://localhost:5099` locally |
| `Archive:MaxRetries` | Retries per Archive call (1 or more). | 3 |
| `Archive:RetryDelay` | Wait between retries. | 5 s |
| `Archive:AttemptTimeout` | Time to wait for each attempt's answer. | 10 s |
| `Archive:ContentTimeout` | Time to receive one part's bytes. | 90 s |
| `Cors:AllowedOrigins` | Allowed browser origins. Empty means any origin. | empty |

Each missing or invalid setting stops startup with a message naming the environment variable to set (for example `Set Archive__BaseUrl`).

## Endpoint

### `POST /exports`

Body (JSON):

| Property | Meaning |
|---|---|
| `cabinet` | The cabinet; case is ignored. |
| `field` | A writable text field that fits the marker `Exported on yyyy-MM-dd`; case is ignored. |
| `from` | Inclusive, UTC: `yyyy-MM-ddTHH:mm:ssZ`, or `yyyy-MM-dd` for 00:00:00. |
| `to` | Inclusive, UTC: `yyyy-MM-ddTHH:mm:ssZ`, or `yyyy-MM-dd` for 23:59:59. |

Answers:

| Status | When |
|---|---|
| 200 `application/zip` | The zip, as an attachment named `{cabinet}-export-{yyyy-MM-dd}.zip`. `Content-Disposition` is exposed to other origins so a browser client can read the name. |
| 400 | Missing input, bad dates, or a field that cannot hold the marker. |
| 404 | Unknown cabinet, or no documents in the range. |
| 502 | The Archive kept failing. |
| 503 | The Archive could not be reached. |
| 504 | The Archive did not answer in time on any attempt. |
| 500 | Anything else. |

Errors found before the download starts are answered as ProblemDetails. An error after the first zip bytes are sent can only break the download: the connection is cut, so the caller never gets a zip that looks complete.

## Errors

`ProblemExceptionHandler` maps:

- an Export exception to its own status and message (Archive rejections keep the Archive's status and reason);
- a bad HTTP request to its status;
- anything else to 500 "Something went wrong on the server. Please try again later." The real error is only logged.

5xx is logged as an error, anything else as a warning.

## Logging

- Console, one line per entry, with scopes and UTC timestamps.
- The controller logs `Start ExportsController.Create with params ...`, then `End ... in <ms> ms`.
- On failure it logs `Error ...` through `LogMethodError` and rethrows.

## Running

- Docker: port 8080 inside the container; runs as user `app`.
- Local: `http://localhost:5186`, calling the Archive at `http://localhost:5099`.

## Conventions

- The controller stays thin: prepare the export (checks happen here), then stream it.
- JSON uses camelCase; enums are camelCase strings.
- The endpoint declares every answer with `ProducesResponseType`, so OpenAPI shows them.
