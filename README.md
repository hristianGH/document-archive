# document-archive

A small document archive: upload folders of files into file cabinets, then export them back out as a zip.

- **Archive service** (ASP.NET Core): cabinets, documents, uploads, index values. Stores records in PostgreSQL and files on a volume.
- **Export service** (ASP.NET Core): builds the export zip from the Archive and marks each exported document.
- **Web client** (React, served by nginx): cabinets, upload and export pages.

How it fits together: [.docs/architecture.md](.docs/architecture.md).

## Prerequisites

| Tool | Needed for |
|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose) | Running everything. PostgreSQL runs in a container; no local install needed. |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet test`, and running the services outside Docker. |
| [Node.js 22](https://nodejs.org/) or later | The web client outside Docker, and the UI tests. |
| Git | Cloning the repository. |

Only Docker is needed to run the system.

## Env variables

Copy `.env.example` to `.env` in the repo root and fill it in. `.env` is ignored by git.

| Variable | Required | Default | Meaning |
|---|---|---|---|
| `POSTGRES_PASSWORD` | **yes** | none | Password for the database. Any value; compose stops if it is missing. |
| `POSTGRES_PORT` | no | 5433 | Host port for PostgreSQL. |
| `ARCHIVE_PORT` | no | 8081 | Host port for the Archive. |
| `EXPORT_PORT` | no | 8082 | Host port for the Export. |
| `WEB_PORT` | no | 8080 | Host port for the web client. |

The services have more settings (upload limits, page size, retries, CORS). Each has a default; see the `.docs` folder of each service.

## Run with Docker

1. Clone the repository and open a terminal in its root.
2. Create the settings file:
   ```
   cp .env.example .env
   ```
   Set `POSTGRES_PASSWORD` in `.env`.
3. Build and start everything:
   ```
   docker compose up --build
   ```
4. Open:

   | What | Address |
   |---|---|
   | Web client | http://localhost:8080 |
   | Archive Swagger | http://localhost:8081/swagger |
   | Export Swagger | http://localhost:8082/swagger |

5. Logs:
   ```
   docker compose logs -f archive export web
   ```
6. Stop:
   ```
   docker compose down
   ```
   Data is kept. `docker compose down -v` **deletes all documents and files**.



## Tests

### .NET tests

No database, network or containers needed.

```
dotnet test
```

Run the tests of one rule, for example the upload rule R8 or the export rule X5:

```
dotnet test --filter "Rule=R8"
```

### End-to-end tests (Playwright)

They run in Docker against a separate stack with its own database and ports (18080–18082), so they never touch your data.

```
cd src/web
npm install
npm run e2e
```

Read the summary at the end of the output. The stack stays up afterwards to look at failures. Then:

```
npx playwright show-report
npm run e2e:down
```

## Run without Docker (development)

1. Start only the database:
   ```
   docker compose up -d postgres
   ```
2. Give the Archive the connection string with the password from `.env` (kept in your user profile, not in the repo):
   ```
   cd src/Archive/Archive.Api
   dotnet user-secrets set "ConnectionStrings:Archive" "Host=localhost;Port=5433;Database=archive;Username=archive;Password=<your password>"
   ```
3. Start the services, each in its own terminal:
   ```
   dotnet run --project src/Archive/Archive.Api
   dotnet run --project src/Export/Export.Api
   ```
   The Archive runs on http://localhost:5099 and the Export on http://localhost:5186.
4. Start the web client:
   ```
   cd src/web
   npm install
   npm run dev
   ```
   Open the address Vite prints.
