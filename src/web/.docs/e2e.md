# web e2e tests

Playwright tests that drive the real UI in Chromium against a full stack of its own: a fresh database, the Archive, the Export and the web client. Data is uploaded and exported through the UI.

## References

- Packages: `@playwright/test` 1.64.1 (the same version as the runner image), `jszip` (reads downloaded zips).
- `docker-compose.e2e.yml` at the repo root.

## Layout

| Item | What it holds |
|---|---|
| `playwright.config.ts` | One worker, 60 s per test, Chromium, list and HTML reports, trace and screenshot on failure. |
| `e2e/global-setup.ts` | Waits up to 120 s for the web page, the Archive's ready check and the Export's live check. |
| `e2e/*.spec.ts` | The tests. |
| `e2e/support/` | Helpers (below). |
| `e2e/fixtures/` | Zips the tests upload. |
| `e2e/Dockerfile` | The runner image: Playwright on Ubuntu, runs `npx playwright test`. |

## The test stack

`docker-compose.e2e.yml` is separate from the normal `docker-compose.yml` and never starts with it. It has its own project name (`document-archive-e2e`), its own volumes and its own ports:

| Service | Port |
|---|---|
| web | 18080 |
| archive | 18081 |
| export | 18082 |
| postgres | 15433 (password `e2e`) |

The `e2e` service runs the tests once, after the web client starts, against `http://web:8080`. Reports are written to `src/web/playwright-report` and `src/web/test-results`.

The stack stays up after a run so failures can be looked at in the browser and the logs.

## Commands

From `src/web`:

| Command | Does |
|---|---|
| `npm run e2e` | Removes the old test stack and its data, builds and starts a new one, and follows the test output. |
| `npm run e2e:down` | Removes the test stack and its data. |
| `npx playwright show-report` | Opens the last HTML report. |

`npm run e2e` follows the logs, so its exit code is not the tests' result; read the summary at the end.

Against a stack already running, `npx playwright test` uses `E2E_BASE_URL`, or `http://localhost:18080` when it is not set.

## Fixtures

| File | Content |
|---|---|
| `sample-material.zip` | The task's sample folder. |
| `delivery-04.zip` | A file, a subfolder with pages in mixed order, a deeper folder, and an empty folder. |
| `not-a-zip.zip` | Text with a `.zip` name. |

TODO: reuse these zips in the .NET tests and the README demo.

## Tests

| File | Covers |
|---|---|
| `navigation.spec.ts` | Moving between pages; the health dots. |
| `cabinets.spec.ts` | The cabinets page lists cabinets and their fields. |
| `upload.spec.ts` | The sample upload with its report; a short folder name fitting a short field; a folder name too long for the field; a file that is not a zip; no double start. |
| `export.spec.ts` | Runs in order on Contracts: uploads the sample twice, then checks the round trip byte for byte, the marker, that the marker is overwritten, that a field too short for the marker is refused, that an empty range is not found, and no double start. |

## Helpers

| File | Does |
|---|---|
| `support/stack.ts` | The base address, fixture paths, today's UTC date. |
| `support/ui.ts` | Finds the form, the Response panel, its badge and the toast; starts uploads and exports; downloads the export; counts and delays requests. |
| `support/zips.ts` | Reads a zip into file names and contents. |
| `support/archive-api.ts` | Reads documents and field values straight from the Archive, to check what the UI did. |

## Conventions

- Find controls by role and name, like a user would. Selects are found as comboboxes, because their label text also includes the options.
- The submit button is found as the form's submit button, because a file input also counts as a button.
- Seed data through the UI, not through the API.
- Each test says what it proves in its name.
