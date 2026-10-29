# AGENTS.md

## Read first

- [.docs/architecture.md](.docs/architecture.md): services, layout, conventions, data model, endpoints, errors, rules and where each is checked.
- [DECISIONS.md](DECISIONS.md): the main infrastructure and data decisions, and why.

## More context when needed

- The `.docs` folder of the project you work on describes what its code does:
  - `src/Archive/.docs/`: `api.md`, `core.md`, `data.md`, `tests.md`
  - `src/Export/.docs/`: `api.md`, `core.md`, `data.md`, `tests.md`
  - `src/Shared/.docs/`: `shared.md`
  - `src/web/.docs/`: `app.md`, `e2e.md`
- [.docs/todos.md](.docs/todos.md): what is cut and possible additions.
- [README.md](README.md): settings, and how to build, run and test.

When a change alters what the code does, update the matching `.docs` file.

## How to add an operation

An operation is a new API endpoint. Follow the [.NET conventions](.docs/architecture.md#net-conventions).

1. **Api:** add an action to a controller. Keep it thin: log, call a handler, map the result.
2. **Core:** add or extend a handler with the rules. No I/O.
3. **Data:** add what the handler needs from the database or files (Archive), or from the Archive client (Export).
4. **Models:** add `…Request` / `…Response` models. Put them in `Shared/Models/` only if both services need them.
5. **Errors:** throw a `ServiceException` with the HTTP status; it becomes a ProblemDetails response.
6. **OpenAPI:** declare the response types and status codes on the action, so `/swagger` shows them.
7. **UI:** add the call and screen in `src/web` if users need it.
8. **Tests:** add API or rule tests in `*.Tests`, and an e2e test if the UI changed.
9. **Docs:** update the endpoint tables in `.docs/architecture.md` and the project's `.docs` files.

## How to verify a change

1. **`dotnet test`:** API and rule tests on the in-memory server. They're fast and need no containers. Run them on every backend change. One rule only: `dotnet test --filter "Rule=R8"`.
2. **`npm run e2e`** (in `src/web`): end-to-end tests through the UI against their own stack (web, Archive, Export, Postgres). Run them on any change, to check that the whole system still works together.
