# Agent handoff — Technical Interview / Simple Tasks

> **Purpose:** Give a new Cursor agent full operational context so it can continue this project without re-discovering decisions from scratch.
>
> **This file is NOT the product spec.** For scope and acceptance criteria, read [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md) and [docs/user-stories.md](docs/user-stories.md). For session history, read [AI-NOTES.md](AI-NOTES.md).

---

## Quick start

1. Read this file end-to-end.
2. Skim [AI-NOTES.md](AI-NOTES.md) for what changed and why.
3. Use [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md) only when you need the full original spec.
4. Run the app and tests before making non-trivial changes.

```powershell
# Auth API (from repo root) — start first
cd src/BlaInterview.Auth.Api
dotnet run

# Tasks API (second terminal)
cd src/BlaInterview.Tasks.Api
dotnet run

# Frontend (third terminal)
cd client
npm install
npm run dev
```

| Service | URL |
|---------|-----|
| Auth API | http://localhost:5098 |
| Auth Swagger | http://localhost:5098/swagger |
| Tasks API | http://localhost:5099 |
| Tasks Swagger | http://localhost:5099/swagger |
| App | http://localhost:5173 |

**Demo login:** `demo@example.local` / `Demo123!` → should show **8 seeded tasks**.

---

## Project identity

| | |
|---|---|
| **Repo** | Project Interview (Simple Tasks) |
| **App name** | Simple Tasks |
| **Goal** | .NET technical interview — personal Kanban board |
| **Status** | Implementation + polish **complete** (2026-07-04) |

**Product narrative:** Secure personal Kanban for one user's tasks (priorities, due dates, drag-and-drop, done/canceled terminal columns, reactivate, search/filters). Tasks are never shared between users.

**Do NOT re-scaffold or rewrite from scratch.** Extend, fix, or polish only.

---

## Document map (don't duplicate these)

| File | Role |
|------|------|
| **[AGENT-HANDOFF.md](AGENT-HANDOFF.md)** (this file) | Operational context for the next agent |
| [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md) | Full agreed spec — stack, API contract, domain, seeds |
| [docs/user-stories.md](docs/user-stories.md) | S1–S7 user stories + acceptance criteria |
| [AI-NOTES.md](AI-NOTES.md) | Chronological log of AI sessions, fixes, verification |
| [README.md](README.md) | Human-facing setup, demo users, GenAI workflow summary |
| [docs/interview-walkthrough.md](docs/interview-walkthrough.md) | Live demo script |
| [docs/genai-prompt-vs-result.md](docs/genai-prompt-vs-result.md) | Prompt vs as-built delta |

---

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET **10** (`net10.0`), ASP.NET Web API, Clean Architecture |
| Database | SQLite, EF Core, migrations |
| Auth | ASP.NET Core Identity + JWT |
| Validation | FluentValidation (Application layer) |
| Tests | xUnit, Moq, **Moq.AutoMock**, **Bogus**, WebApplicationFactory |
| Frontend | React 19, Vite, TypeScript, TanStack Query, Tailwind, shadcn/ui, @dnd-kit |

**Environment notes:**
- Requires .NET 10 SDK (`net10.0`).
- **PowerShell:** chain commands with `;`, not `&&`.

**Last verified:** `dotnet test` (**105 pass** — 60 unit + 45 integration), `client/` `npm run build` (pass), `client/` `npm run verify` (pass).

**Lint:** `client/` — `npm run verify` (oxlint + `tsc`). Backend — `.editorconfig` + `Directory.Build.props` (NET analyzers).

---

## Solution layout

```
src/
  BlaInterview.Domain/          # TaskItem, KanbanStatus, TaskPriority — no EF/Identity
  BlaInterview.Application/     # TaskService, validators, DTOs, AutoMapper profiles
  BlaInterview.Infrastructure/  # EF, Identity, JWT, TaskRepository, seeders
  BlaInterview.Api.Shared/      # Shared HTTP middleware, Swagger, CORS helpers
  BlaInterview.Auth.Api/        # Auth + health endpoints (port 5098)
  BlaInterview.Tasks.Api/       # Task CRUD endpoints (port 5099)
tests/
  BlaInterview.Unit.Tests/        # Domain + Application (Moq.AutoMock, Bogus, Theory/MemberData)
  BlaInterview.Integration.Tests/ # HTTP (WAF) + EF/SQLite repository (in-memory fake DB)
client/                           # React frontend
docs/                             # genai-scaffold-prompt.md, user-stories.md
AI-NOTES.md
README.md
AGENT-HANDOFF.md
```

---

## Architectural decisions (already implemented)

1. **Clean Architecture** — business logic in Application; thin controllers; no validation in controllers.
2. **Two segregated APIs** — `Auth.Api` (identity + JWT issuance) and `Tasks.Api` (CRUD); shared JWT config via `JwtAuthenticationExtensions`.
2. **`KanbanStatus` enum** (NOT `TaskStatus`) — avoids clash with `System.Threading.Tasks.TaskStatus`.
3. **AutoMapper `TaskProfile`** — entity/DTO mapping via profiles; pagination constants in `TaskPagination`.
4. **JWT in localStorage** — demo tradeoff documented in README (not HttpOnly cookies).
5. **Logout** — instant, no confirmation modal; clears localStorage + redirects to login.
6. **Terminal columns** — `Completed` / `Cancelled`: drag in only; reopen via `POST /api/tasks/{id}/reactivate` → moves to `Todo`.
7. **Filters** — hide non-matching cards; drag-and-drop still allowed while filtered.
8. **403 vs 404** — another user's task → **403**; missing ID → **404**.
9. **Register** — auto-login after successful registration.

---

## Test layout (LessonsManagement pattern)

Segregated into **two assemblies** — unit vs integration — with shared collection fixtures.

### `BlaInterview.Unit.Tests`

| Folder | Purpose |
|--------|---------|
| `Domain/` | Pure entity tests (`TaskItemTests`) |
| `Application/` | `TaskServiceTests`, `ValidatorTests`, `NotificationTests`, `TaskProfileTests` |
| `Infrastructure/` | `AuthServiceTests`, `JwtTokenServiceTests` |
| `Api/` | `AppExceptionHandlerTests` |
| `Fixtures/` | `TaskServiceFixtures`, `AuthServiceFixtures`, `MapperFixtures`, `DomainFixtures` (Bogus valid/invalid builders) |
| `Data/` | `MemberData` providers (`TaskItemTerminalData`, `KanbanStatusTransitionData`) |

Patterns: `[Collection]` + `ICollectionFixture`, `[Trait("Category", ...)]`, `[Fact/Theory(DisplayName=...)]`, Arrange/Act/Assert comments.

### `BlaInterview.Integration.Tests`

| Folder | Purpose |
|--------|---------|
| `Config/` | `AuthAppFactory`, `TasksAppFactory`, `IntegrationTestsFixture` (shared temp SQLite DB) |
| `Api/` | `AuthTests`, `AuthExtendedTests`, `TaskTests`, `TaskCrudTests`, `TaskQueryTests`, `TaskOwnershipTests` |
| `Infrastructure/` | `TaskRepositoryTests` against **in-memory SQLite** (`:memory:`) |
| `Fixtures/` | `TaskRepositoryFixtures` (fake DB + seed helpers) |

**DB strategy:** Unit tests **fake** `ITaskRepository` via AutoMoq (no DB). Integration repository tests use **in-memory SQLite** (real EF, fake persistence). API tests use **dual WebApplicationFactory** — login on Auth API, task calls on Tasks API, same DB file path.

Filter integration tests by trait, e.g. `dotnet test --filter "Category=Task Service"`.

---

## API

### Public

| Method | Route |
|--------|-------|
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| POST | `/api/auth/logout` |
| GET | `/api/health` |

### Protected (Bearer JWT)

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/tasks` | Paginated list; query via `TaskListQuery` |
| GET | `/api/tasks/{id}` | |
| POST | `/api/tasks` | |
| PUT | `/api/tasks/{id}` | |
| DELETE | `/api/tasks/{id}` | |
| POST | `/api/tasks/{id}/reactivate` | |

Swagger JWT Authorize: `src/BlaInterview.Api.Shared/Swagger/SwaggerExtensions.cs`.

---

## Kanban columns (enum 1:1)

| `KanbanStatus` | UI label | Notes |
|----------------|----------|-------|
| `Todo` | To Do | |
| `InProgress` | In Progress | |
| `OnHold` | On Hold | |
| `InReview` | In Review | |
| `Completed` | Done | terminal — drag in only |
| `Cancelled` | Canceled | terminal — drag in only |

Frontend: `client/src/api/types.ts` (`COLUMNS`, `ALL_STATUSES`).

---

## Demo users & seed data

| Email | Password | Tasks |
|-------|----------|-------|
| `demo@example.local` | `Demo123!` | **8 tasks** across all columns |
| `other@example.local` | `Other123!` | **4 tasks** (403 demo; titles prefixed `[Other]`) |

**Seeding behavior:**
- Runs at API startup in **Development** only (`Program.cs` → `DatabaseSeeder.SeedAsync`).
- **Per-user:** seeds when that user has zero tasks (not a global "skip if any task exists").
- **New registrations** → empty board.
- DB file: `src/BlaInterview.Auth.Api/tasks.db` — delete + restart Auth API (then Tasks API) to force re-seed if demo board is stale.

**Demo task titles:** Plan sprint backlog, Refine API integration (Todo); Model training pipeline, Data node mapping (In Progress); Waiting on design assets (On Hold); Visual flow schema (In Review); Ship release notes (Completed); Deprecated feature cleanup (Cancelled).

Login page pre-fills demo credentials (`client/src/pages/LoginPage.tsx`).

---

## Bugs already fixed (do not reintroduce)

| Issue | Fix location |
|-------|----------------|
| JWT 401 on protected routes (Identity cookie overrode Bearer) | `Infrastructure/DependencyInjection.cs` — default authenticate/challenge = JWT Bearer |
| Seed cards invisible (API numeric enums vs frontend string keys) | `Program.cs` — `JsonStringEnumConverter`; `client/src/api/client.ts` — `normalizeTask()` |
| SQLite `ORDER BY DateTimeOffset` unsupported | `TaskRepository` — sort in memory |
| SQLite `WHERE DateTimeOffset` unsupported | `TaskRepository` — date-range filters in memory |
| Integration test token JSON casing | Tests — case-insensitive deserialization |
| Swagger OpenAPI types | `SwaggerExtensions.cs` — `Microsoft.OpenApi` namespace |

---

## Frontend map

| Area | Path |
|------|------|
| Auth | `client/src/auth/AuthContext.tsx` |
| API client | `client/src/api/client.ts` |
| Types / columns | `client/src/api/types.ts` |
| Board | `client/src/pages/BoardPage.tsx` |
| Kanban | `client/src/components/KanbanBoard.tsx`, `KanbanColumn.tsx`, `TaskCard.tsx` |
| Filters | `client/src/components/SidebarFilters.tsx` |
| Task modal | `client/src/components/TaskDialog.tsx` |
| shadcn/ui | `client/src/components/ui/*` |

---

## User / workflow preferences

- **Scope gate passed:** User approved "OK, implement" — full build is done.
- **Minimize diff scope** — match existing conventions; no drive-by refactors.
- **Do not git commit** unless user explicitly asks.
- **Do not edit** `docs/genai-scaffold-prompt.md` or `docs/user-stories.md` unless scope changes.
- **Update `AI-NOTES.md`** when you make meaningful decisions or fixes.
- **Update `README.md`** only if run/setup behavior changes.
- **Update this file** if operational context changes (new env quirks, new demo users, broken assumptions).

---

## Possible follow-ups (not started)

- Richer domain behavior on `TaskItem` (discuss with team)
- TaskCard / board visual polish (partial — urgency badges, skeletons, empty columns done)
- More unit or E2E tests
- Interview demo walkthrough script → [docs/interview-walkthrough.md](docs/interview-walkthrough.md)
- CI pipeline → **`.github/workflows/ci.yml`**

---

## Agent prompt template

Paste into a new chat opened in this repo root, then add your task:

```
Read AGENT-HANDOFF.md, AI-NOTES.md, and any source files relevant to my task.
Continue this project as the same agent that built it — do not re-scaffold.

My task: [DESCRIBE HERE]
```

---

*Last updated: 2026-07-04 (test restructure)*
