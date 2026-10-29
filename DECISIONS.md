# Decisions

The large decisions on infrastructure and data, each as "A, because B". How it all fits together: [.docs/architecture.md](.docs/architecture.md).

## Infrastructure

- **Each service is split into Api, Core and Data, with a small Shared library,** because the rules then stay testable without a database or network, and each layer has one clear job.
- **nginx serves the web client and forwards calls to the services,** because the browser then needs one address. The service addresses come from `config.json`, written when the container starts, so the same image runs with any settings.
- **Secrets come from `.env` or .NET user secrets, with no defaults,** because no credential may be in an image or in the repository. A key store (for example Azure Key Vault or a secrets container) can replace `.env` later.
- **The Archive applies its database migrations itself and retries until the database is up,** because services may start in any order.
- **The Export retries Archive calls and limits how long each may take,** because the Archive may be briefly unavailable.
- **The export zip is streamed to the browser while it is built,** because exports may be larger than the Export service's memory.
- **Documents are marked only after the whole zip is written,** because a failure halfway then leaves nothing marked, instead of marked documents and a broken zip. Running the export again fixes any failure.
- **Everything is UTC, and a date range includes the whole last day,** because users, services and containers may be in different time zones.
- **Every error is ProblemDetails,** because failures need one documented shape on every endpoint.
- **`dotnet test` uses fakes for the database and the Archive, and Playwright tests cover the real stack,** because `dotnet test` must run with no network and no containers.

## Data

- **PostgreSQL for the records,** because:
  - `jsonb` holds each cabinet's own index fields in one column, and one field can be updated in place;
  - `timestamptz` stores UTC instants exactly;
  - the EF Core provider (Npgsql) is mature, and the official image is small, free, and has a ready health check (`pg_isready`).
- **Part files live on a volume, not in the database,** because files can then be streamed instead of loaded into memory.
- **Index values are one `jsonb` column, checked against `cabinets.json` in Core,** because each cabinet has different fields.
- **An upload is one zip, with limits (1 MB zip, 5 MB unpacked, 1000 entries),** because a browser cannot send a folder in one request, and uploads are untrusted.
- **Paging uses page numbers and sizes, ordered by `StoredAt` then `Id`,** because that keeps the order stable during an export.

## What is cut and what is missing

Listed with the reasons in [.docs/todos.md](.docs/todos.md).

## AI notes

The work was done with an AI agent. I gave it detailed instructions and had it write a plan first. I checked the plan, added clarifications, and approved it before any code was written. The agent followed the approved steps closely.

Where it went wrong:

- **It asked again about things already decided.** Over several rounds it re-asked questions that had been settled earlier. I pointed this out, and it then re-read the earlier decisions before asking.

## Time log

See [.docs/timelog.md](.docs/timelog.md).
