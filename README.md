# Todo List

A small TODO list app with an Angular front-end and a .NET Web API back-end.
Tasks live in memory inside the API and reset on each restart — no database
is required.

> Built for an interview take-home. The goal was a "simple" CRUD app done
> with conventions a reviewer would expect to see in a real codebase:
> proper layering on the back end, presentational components on the
> front end, real tests on both sides.

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick start](#quick-start)
- [Verifying the install](#verifying-the-install)
- [Running the tests](#running-the-tests)
- [API reference](#api-reference)
- [Logic flow — how a click reaches the store and back](#logic-flow--how-a-click-reaches-the-store-and-back)
- [Architecture notes](#architecture-notes)
- [Project layout](#project-layout)
- [Troubleshooting](#troubleshooting)
- [Things I considered and decided against](#things-i-considered-and-decided-against)

---

## Features

- View, add, update, and delete todos.
- **Inline edit (✎)** — change title, due date, and notes without leaving
  the list.
- **Subtasks (▾)** — expand any todo to add, check off, or delete its
  subtasks. The row shows `done / total` progress.
- **Active / Completed tabs** — todos move to *Completed* when every
  subtask is checked, or when the top-level checkbox is checked (for todos
  with no subtasks).
- Empty-title submissions are rejected with `400`. Unknown ids return `404`.

---

## Tech stack

### Backend (`backend/`)

| Thing | Version | Why |
|---|---|---|
| .NET SDK | **10.0** | Latest LTS-track |
| ASP.NET Core MVC | (bundled) | Controllers with attribute routing — conventional enterprise layout |
| Microsoft.AspNetCore.OpenApi | 10.0.8 | OpenAPI / Swagger metadata at `/openapi/v1.json` |
| C# language version | 13 (preview-aligned) | Records, file-scoped namespaces, collection expressions |

### Backend tests (`backend.Tests/`)

| Thing | Version | Why |
|---|---|---|
| xUnit | (xunit template default) | Standard .NET test framework |
| Microsoft.AspNetCore.Mvc.Testing | (bundled) | `WebApplicationFactory<Program>` for HTTP-level integration tests |

### Frontend (`frontend/`)

| Thing | Version | Why |
|---|---|---|
| Angular | **21.2** | Latest — signals, modern control flow, `input()` / `output()` APIs |
| TypeScript | 5.9 | Required by Angular 21 |
| Tailwind CSS | **4.3** | All styling done with Tailwind utility classes; no SCSS anywhere |
| PostCSS + `@tailwindcss/postcss` | 8.5 / 4.3 | How Tailwind v4 integrates with Angular's Vite-based build |
| RxJS | 7.8 | Used as a thin layer behind `HttpClient` (`firstValueFrom`) |
| Vitest | 4.0 | Test runner (replaces Karma/Jasmine in modern Angular) |
| jsdom | 28 | DOM in Vitest environment |
| Prettier | 3.8 | Formatter (not enforced in CI) |

### Tooling

- **Node.js** 20+ and **npm** 10+ (the repo pins `npm@11.12.0` via `packageManager`).
- **Angular Router** drives a single `/todos` route (lazy-loaded).
- **Angular dev server proxy** (`frontend/proxy.conf.json`) forwards
  `/api/*` to the backend so the browser never deals with CORS or
  self-signed certs.

---

## Prerequisites

You need both of these on `PATH` before cloning:

1. **.NET 10 SDK** — verify with `dotnet --version` (should be `10.x`).
   Install: <https://dotnet.microsoft.com/download/dotnet/10.0>
2. **Node.js 20+** and **npm** — verify with `node --version` and
   `npm --version`. Install: <https://nodejs.org/>

That's it. No database, no Docker, no global CLI tools.

---

## Quick start

Clone, restore, run. Two terminals — one for each process.

### 1. Clone

```bash
git clone https://github.com/ChinmayGit8765/TODO-list.git
cd TODO-list
```

### 2. Terminal A — backend

```bash
cd backend
dotnet restore
dotnet run
```

The API starts on **`http://localhost:5033`**. You should see Kestrel log
`Now listening on: http://localhost:5033` after a few seconds.

HTTPS redirection is **disabled in Development**, so the reviewer doesn't
need to trust an ASP.NET dev certificate to get the app running.

### 3. Terminal B — front-end

```bash
cd frontend
npm install        # first time only
npm start
```

`npm install` will pull Angular, Tailwind, Vitest, etc. (a few hundred MB of
`node_modules/`). `npm start` runs `ng serve`, which:

- Compiles the app and watches for changes.
- Boots a dev server on **`http://localhost:4200`**.
- Proxies `/api/*` to `http://localhost:5033` (see `frontend/proxy.conf.json`).

### 4. Open the app

Open <http://localhost:4200> in a browser. You should see five seeded todos.

---

## Verifying the install

If you want to confirm everything wired up correctly without using the UI:

```bash
# Hits the backend through the Angular dev server's proxy.
curl http://localhost:4200/api/todos
```

You should get a JSON array of 5 seeded todos. If you get `ECONNREFUSED`
the backend isn't running. If you get HTML, the proxy isn't loading the
config — check `frontend/proxy.conf.json` and `frontend/angular.json`.

---

## Running the tests

### Backend (xUnit)

```bash
# From the repo root:
dotnet test
```

This builds both projects and runs all 23 tests:

- **13 unit tests** in `backend.Tests/TodoStoreTests.cs` — exercise
  `TodoStore` directly (constructor seeding, add/get/update/delete,
  subtask lifecycle, null handling).
- **9 integration tests** in `backend.Tests/TodoApiTests.cs` — boot the
  full ASP.NET pipeline via `WebApplicationFactory<Program>` and hit the
  real HTTP routes with `HttpClient`.

> **Important:** stop the backend (`Ctrl+C`) before `dotnet test`, otherwise
> the build can't replace `backend.exe` while it's running and you'll get a
> file-lock error.

### Frontend (Vitest)

```bash
cd frontend
npm test
```

Runs once and exits. 10 tests:

- 2 for the `App` shell (creates, renders router-outlet).
- 8 for `TodosService` — uses `HttpTestingController` to mock the HTTP
  layer and assert that the service's signals update correctly on each
  call.

---

## API reference

Base URL: `http://localhost:5033/api/todos` (or `/api/todos` through the
front-end proxy at `:4200`).

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/todos` | — | `200` array of `TodoItem` |
| `GET` | `/api/todos/{id}` | — | `200` `TodoItem` · `404` |
| `POST` | `/api/todos` | `{ "title": string }` | `201` `TodoItem` (with `Location` header) · `400` |
| `PUT` | `/api/todos/{id}` | `UpdateTodoRequest` | `200` `TodoItem` · `400` · `404` |
| `DELETE` | `/api/todos/{id}` | — | `204` · `404` |
| `POST` | `/api/todos/{id}/subtasks` | `{ "title": string }` | `200` `TodoItem` · `400` · `404` |
| `PUT` | `/api/todos/{id}/subtasks/{subId}/toggle` | — | `200` `TodoItem` · `404` |
| `DELETE` | `/api/todos/{id}/subtasks/{subId}` | — | `200` `TodoItem` · `404` |

### Shapes

```jsonc
// TodoItem
{
  "id": "9b1a0c6a-…",        // Guid, server-assigned
  "title": "Buy groceries",
  "createdAt": "2026-05-14T09:00:00Z",
  "dueAt": "2026-05-15T09:00:00Z",      // nullable
  "notes": "Don't forget bread",        // nullable
  "isComplete": false,
  "subTasks": [
    { "id": "…", "title": "Milk", "isComplete": false }
  ]
}

// UpdateTodoRequest (PUT body — all fields required)
{
  "title": "Buy groceries",
  "dueAt": "2026-05-15T00:00:00Z",      // null clears
  "notes": "…",                         // null clears
  "isComplete": false
}
```

Live OpenAPI spec (Development only): <http://localhost:5033/openapi/v1.json>

---

## Logic flow — how a click reaches the store and back

This is the read I'd want before reading code. Following a single
interaction end to end:

### Example: user adds a subtask to "Buy groceries"

```
Browser
  │
  │  1. User clicks ▾ on the "Buy groceries" row.
  │     onToggleExpand(id) flips that id in expandedIds (signal).
  │     The @if (isExpanded(id)) branch in todo-list.html mounts <app-sub-task-list>.
  │
  │  2. User types "Cheese" and submits the add-subtask form.
  │     SubTaskListComponent.onAdd() → emits addSub("Cheese").
  │     todo-list.html listener calls onAddSubTask(todoId, "Cheese").
  │     onAddSubTask → todosService.addSubTask(todoId, "Cheese").
  │
  │  3. TodosService:
  │       http.post<Todo>('/api/todos/{id}/subtasks', { title: 'Cheese' })
  │       firstValueFrom() unwraps the Observable to a Promise.
  │       On success, _todos signal is updated via replace(updatedTodo).
  │
  ▼
Angular dev server (port 4200)
  │
  │  4. proxy.conf.json forwards /api/* to http://localhost:5033.
  │     Browser sees same-origin — no CORS round-trip.
  │
  ▼
ASP.NET Core (port 5033)
  │
  │  5. Routing matches POST /api/todos/{id}/subtasks to
  │     TodosController.AddSubTask(Guid id, CreateSubTaskRequest req).
  │     The [ApiController] attribute auto-validates and binds the body.
  │     The route constraint {id:guid} rejects non-Guid ids as 404.
  │
  │  6. Controller validates non-empty title, then calls
  │     _store.AddSubTask(id, request.Title.Trim()).
  │
  │  7. TodoStore.AddSubTask takes the write-lock, looks up the parent,
  │     appends a new SubTask record (with a fresh Guid), creates a new
  │     immutable TodoItem via `with { SubTasks = newList }`, and replaces
  │     the entry in the ConcurrentDictionary atomically.
  │
  │  8. Returns the updated TodoItem. Controller wraps as 200 Ok(item).
  │
  ▼ (response)
TodosService
  │
  │  9. replace(item) is called: the signal updates so the row carrying
  │     this todo's id is swapped for the new version.
  │
  ▼
SubTaskListComponent
  │
  │ 10. Receives the new todo via its input() — Angular re-renders the
  │     subtask list, the input clears (draftTitle reset), and the row's
  │     progress badge (e.g. "0/5") increments to reflect the new subtask.
```

The same shape applies to every interaction:

- **Click an icon button** → component method → service call → HTTP →
  controller → store mutation → response → signal update → re-render.

The interesting decisions in that chain:

- **Subtask mutations return the full updated todo** — not just the
  subtask. One round-trip gets the frontend everything it needs to
  redraw, no second GET required.
- **Components stay dumb.** Only `TodoListComponent` injects `TodosService`.
  `TodoCardComponent`, `TodoEditFormComponent`, and `SubTaskListComponent`
  take inputs and emit outputs — they never touch HTTP or the store.
- **Backend store is the single source of truth for shape.** The
  controller always returns the persisted record, never the request
  payload. This means the frontend can't drift out of sync with what
  the server believes.

### Failure paths

- **Empty title on add/edit:** the component method short-circuits before
  the HTTP call; the form input's "Add" button is also disabled while the
  draft is empty.
- **Backend down on initial load:** `ngOnInit`'s `await load()` throws,
  the error signal is set, the page shows "Could not load todos. Is the
  backend running?"
- **Backend down mid-action:** the per-action try/catch sets a localised
  error message ("Failed to add subtask.") but leaves the existing list
  intact.
- **Concurrent edits to the same todo:** the backend takes a lock for
  read-modify-write operations, so each request observes a consistent
  snapshot. The last writer wins on race conditions — acceptable for an
  in-memory single-user app.

---

## Architecture notes

### Backend (single deployable, enterprise-style layering)

- `Controllers/` — HTTP boundary only. Maps requests to service calls and
  returns `ActionResult<T>`. Each action has `[ProducesResponseType]`
  attributes so the OpenAPI doc lists all possible responses.
- `Contracts/` — public request DTOs (`CreateTodoRequest`,
  `UpdateTodoRequest`, `CreateSubTaskRequest`). The controller never
  accepts a domain entity directly — clients can't sneak in `Id` or
  `CreatedAt` through a request body.
- `Services/` — application layer. `ITodoStore` is the abstraction the
  controller depends on; `TodoStore` is the in-memory implementation.
  `TodoUpdate` is the *service-layer* command record (kept distinct from
  the transport DTO so the store has no dependency on HTTP types).
- `Domain/` — pure entities: `TodoItem`, `SubTask`, plus the seed data.
  No framework dependencies.

The store is a **singleton** (`AddSingleton<ITodoStore, TodoStore>()`),
so one instance serves the lifetime of the process. Compound
read-modify-write operations (toggle subtask, update todo) take a lock to
stay atomic — `ConcurrentDictionary` alone is not enough for those.

**Access modifiers.** Interfaces, DTOs, and domain records are `public`
because they appear in the public surface of the controller. The store
implementation, seed data, and `TodoSeed` helpers are `internal`.
`InternalsVisibleTo("backend.Tests")` opens internals to the test project.

### Frontend (single page, three presentational components)

- `TodosService` (`services/todos.service.ts`) is the only place that
  knows about HTTP. It holds the canonical list in a read-only signal;
  components subscribe and re-render automatically.
- `TodoListComponent` (`pages/todo-list/`) orchestrates per-row UI state
  (`expandedIds`, `editingId`) and wires service calls to events from its
  children.
- `TodoCardComponent`, `TodoEditFormComponent`, `SubTaskListComponent`
  (`components/`) are *dumb*: inputs in, outputs out, no service injection.
- All styling is **Tailwind v4** utility classes inline in templates. No
  SCSS. Tailwind is wired up via PostCSS — see `.postcssrc.json`.
- The list page is **lazy-loaded** (`loadComponent`), so the initial
  bundle stays small.

---

## Project layout

```
TodoList.slnx                            — solution file linking the projects
.gitignore                               — node_modules, bin, obj, dist

backend/                                 — .NET Web API
  Controllers/TodosController.cs         — HTTP boundary; one controller
  Contracts/TodoRequests.cs              — public request DTOs
  Services/ITodoStore.cs                 — application abstraction + TodoUpdate
  Services/TodoStore.cs                  — singleton in-memory implementation
  Domain/TodoItem.cs                     — TodoItem + SubTask entities
  Domain/TodoSeed.cs                     — startup seed data
  Program.cs                             — service registration + Map pipeline
  backend.csproj                         — project file (incl. InternalsVisibleTo)

backend.Tests/                           — xUnit tests
  TodoStoreTests.cs                      — store unit tests
  TodoApiTests.cs                        — endpoint integration tests
  backend.Tests.csproj

frontend/                                — Angular app
  src/styles.css                         — Tailwind import + html/body base
  src/app/models/todo.ts                 — TS interfaces mirroring the API
  src/app/services/todos.service.ts      — signal-based facade over HTTP
  src/app/components/
    todo-card/                           — single-row presentation
    todo-edit-form/                      — inline edit form
    sub-task-list/                       — subtask panel
  src/app/pages/todo-list/               — page that composes the above
  proxy.conf.json                        — dev server proxy to the backend
  .postcssrc.json                        — Tailwind v4 PostCSS plugin
  angular.json                           — build/serve config
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Front-end says "Could not load todos." | Backend not running, or running on a different port | Start `dotnet run` in `backend/`; confirm `http://localhost:5033/api/todos` returns JSON |
| `Port 4200 is already in use` | Previous `ng serve` still alive | On Windows: `Get-NetTCPConnection -LocalPort 4200 -State Listen \| Select OwningProcess`, then `Stop-Process -Id <pid>` |
| `dotnet test` fails with "file is being used by another process" | Backend is still running | Stop the backend (`Ctrl+C` in its terminal) before running tests |
| 404 on subtask `toggle` | Frontend cached an old todo id, or the parent was deleted | Reload the page so the list refetches |
| Tailwind classes have no effect | `styles.css` missing the `@import "tailwindcss";` line, or `.postcssrc.json` not in `frontend/` | Verify both files; re-run `npm start` |

---

## Things I considered and decided against

- **A database.** The brief says in-memory; adding one is extra surface
  area without solving anything the brief asks for. The store interface
  (`ITodoStore`) means a real database implementation could be swapped in
  without touching the controller.
- **LLM-assisted subtask suggestions.** I prototyped an "AI suggest"
  button that would POST a parent title and get back proposed subtasks.
  Dropped for this submission — it would need an API key, error handling
  for the LLM being unavailable, and setup steps in this README that
  would block a reviewer without a key. Happy to walk through how I'd
  wire it up.
- **NgRx / a state-management library.** Overkill for three CRUD
  operations. A signals-based service is the idiomatic Angular 17+
  approach and keeps the read path obvious.
- **Splitting into actual microservices.** "Microservices" implies
  multiple separately deployable services with their own data stores;
  that's the wrong architecture for an in-memory single-entity app. The
  internal folder structure (Controllers / Contracts / Services /
  Domain) gives the same separation-of-concerns story within one
  service.
- **Detail page route.** An earlier version routed each todo to
  `/todos/:id`. Removed in favour of inline edit + subtask dropdown —
  faster interactions, fewer round-trips, less code.
