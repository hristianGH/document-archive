# web

The browser client: a React app built with Vite and served by nginx. It shows the cabinets, uploads a zipped folder to the Archive, and downloads an export from the Export service.

## References

- Packages: React 19, Vite, TypeScript.
- Talks to the Archive and the Export only over HTTP; it shares no code with them.

## Layout

| Item | What it holds |
|---|---|
| `src/main.tsx` | Loads `config.json`, then renders the app. If the settings cannot be loaded, shows the reason instead. |
| `src/App.tsx` | Loads the cabinets, picks the page, holds the selected cabinet. |
| `src/config.ts` | Reads `archiveUrl` and `exportUrl` from `config.json`. |
| `src/api/` | HTTP calls: `archive.ts`, `export.ts`, `health.ts`, shared `http.ts`, `types.ts`. |
| `src/service/` | App logic with no markup: routing, settings, health polling, request state, field lists, file download. |
| `src/view/` | The three pages and the upload report. |
| `src/components/` | Reusable pieces: header, panels, select, toast, loading, empty and error states. |
| `src/styles.css` | All styles. |
| `nginx/` | The nginx site template and the script that writes `config.json` at container start. |
| `public/config.json` | Settings used by the development server. |
| `e2e/`, `playwright.config.ts` | UI end-to-end tests (see `e2e.md`). |

## Pages

The page is chosen by the address hash; an unknown hash shows the cabinets.

| Address | Page | Calls |
|---|---|---|
| `#/cabinets` | **File cabinets.** Pick a cabinet and see its fields: label, name, type, maximum length, writable. | `GET /cabinets` |
| `#/upload` | **Upload a folder.** Pick a cabinet, a field for the folder name, and a zip. Shows the report: stored, skipped, failed. | `POST /cabinets/{cabinet}/uploads` |
| `#/export` | **Export documents.** Pick a cabinet, a field for the marker, and from and to dates. Downloads the zip. | `POST /exports` |

- The selected cabinet is shared by all pages.
- The field lists show only writable text fields, with their maximum length.
- The Export page notes that the marker needs at least 22 characters and that dates are whole UTC days, both included.
- A cabinet with no writable text field shows a message instead of the form.

Every page shows a **Request** panel (the form and the endpoint it calls) and a **Response** panel (idle, working, success, or the error).

## Header

- Links to the three pages.
- A dot for each service: up, down or checking. The Archive is checked at `/health/ready`, the Export at `/health/live`, every 30 seconds by default.
- A gear menu to check now or change how often to check. The choice is saved in the browser.

## Requests and errors

- A request cannot be started twice while it runs; the button is disabled.
- Every failure shows the reason in the Response panel and in a toast.
- Error answers are read as ProblemDetails. A failed upload with a report still shows the report.
- If a service cannot be reached, the message says so.
- If an export breaks after the download started, the message says the zip is incomplete and some documents may already be marked.
- The downloaded file keeps the name the Export service gives it.

## Settings and serving

`config.json` holds the two service addresses. In Docker it is written when the container starts, from:

| Variable | Default | Meaning |
|---|---|---|
| `ARCHIVE_URL` | `/api/archive` | Address the browser uses for the Archive. |
| `EXPORT_URL` | `/api/export` | Address the browser uses for the Export. |
| `ARCHIVE_UPSTREAM` | `http://archive:8080` | Where nginx sends `/api/archive/...`. |
| `EXPORT_UPSTREAM` | `http://export:8080` | Where nginx sends `/api/export/...`. |
| `NGINX_RESOLVER` | `127.0.0.11` | DNS for service names. |

nginx:

- serves the app on port 8080, sending unknown paths to `index.html`;
- forwards `/api/archive/...` and `/api/export/...` to the services without the prefix;
- looks up service names per request, so it starts even when the services are not up yet;
- caps upload bodies at 2 MB and does not buffer them;
- does not buffer export downloads and waits up to 300 seconds for them;
- never caches `config.json`.

The addresses can point at services on another host; the services allow other origins through CORS.

## Running

- `npm run dev`: the development server, sending `/api/archive` to `localhost:5099` and `/api/export` to `localhost:5186`.
- `npm run build`: type check and production build.
- Docker: built with Node, served by nginx on port 8080.

## Conventions

- `api/` only makes HTTP calls and returns data or throws `ApiError`.
- `service/` holds logic and hooks; `view/` and `components/` hold markup.
- Each request goes through `useRequest`, which gives the state, blocks double starts and shows the toast.
- Forms use real labels and controls, so they are reachable by role and name.
