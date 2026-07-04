# AI Notes — BLA Interview

Log of meaningful AI-assisted sessions: what was suggested, what was accepted/rejected/changed, and what verified it.

---

## Phase 1 — Alignment (2026-07-04)

- **AI suggested:** Single informal user story only; defer detailed stories until after scaffold.
- **Accepted/changed:** User chose one product narrative plus S1–S7 scoped stories with acceptance criteria; added S7 search/filters (title, status multi-select, created/updated date presets + custom range); terminal Done/Canceled columns with single `POST /reactivate` endpoint; filtered board hides non-matching cards; drag-and-drop allowed while filtered.
- **Verified:** Scope saved to `docs/genai-scaffold-prompt.md` and `docs/user-stories.md` on user approval ("OK, save prompt").

## Phase 1 — Gap review (2026-07-04)

- **AI suggested:** Pre-implement defaults for JWT storage, logout, 404 vs 403, filter edge cases, design patterns.
- **Accepted/changed:** Switched from HttpOnly cookie back to **JWT in localStorage (Option A)** with documented demo tradeoff (XSS vs simplicity); logout instant, no modal.
- **Accepted/changed:** App name **Simple Tasks**; **shadcn/ui** + TanStack Query; **WebApplicationFactory** integration tests; auto-login after register; delete confirmation dialog.

## Phase 2 — Implementation (2026-07-04)

- **AI suggested:** Full Clean Architecture scaffold, Identity+JWT, Kanban React UI with @dnd-kit, xUnit tests per layer.
- **Accepted/changed:** Used .NET 10 SDK (net10.0) on available machine; renamed `TaskStatus` → `KanbanStatus` (clash with `System.Threading.Tasks.TaskStatus`); JWT default scheme fix (Identity cookie was overriding Bearer); manual TaskMapper instead of AutoMapper; client-side sort for SQLite DateTimeOffset ORDER BY limitation.
- **Verified:** `dotnet build`, `dotnet test` (10 tests pass), Vite client scaffold.
- **Verified:** `docs/genai-scaffold-prompt.md` and `docs/user-stories.md` updated.

## Phase 2 — Polish + seed visibility (2026-07-04)

- **User report:** No seed cards visible on board after login.
- **Root cause:** API serialized `KanbanStatus` as integers (`0`–`5`); frontend grouped tasks by string keys (`"Todo"`, etc.) → all cards fell through to undefined columns.
- **Fixes:** `JsonStringEnumConverter` in `Program.cs`; `normalizeTask()` fallback in `client.ts`; per-user seeding in `DatabaseSeeder` (demo/other users seeded independently); login page pre-filled with demo credentials + hint.
- **Polish:** Swagger JWT Authorize button; shadcn/ui on login, register, task dialog, sidebar filters, delete confirm; README GenAI section + seed troubleshooting.
- **Verified:** `dotnet test`, `npm run build`.

## Refactor — TaskQuery persistence boundary (2026-07-04)

- **Change:** Introduced `Application/Queries/TaskQuery` for repository filtering; `TaskFilterRequest` stays API-facing. `TaskService` maps via `TaskMapper.ToQuery`; `TaskRepository` no longer imports DTOs. Sort remains client-side (SQLite `DateTimeOffset` ORDER BY limitation).
- **Verified:** `dotnet test`.

## 2026-07-04 — Git submission prep

- Hardened `.gitignore` (bin/obj, `*.db`, `node_modules`, `.cursor/`, `appsettings.Development.json`, `.env*`).
- JWT: committed `appsettings.json` uses a non-production placeholder; real dev key lives in gitignored `appsettings.Development.json` (see `.example` file).
- Sanitized `AGENT-HANDOFF.md` local paths; README documents first-time config copy step.
- Verified `dotnet test` (28 pass) and `git add -n` excludes secrets/artifacts.


- **User asked:** Align BLA tests with LessonsManagement reference projects (`TestXUnit.Tests` + `IntegrationTestsXUnitApp.Tests`) — fixtures, Theory, Trait, MemberData, Collection; segregate unit vs integration.
- **Accepted/changed:** Collapsed 4 per-layer projects → **2 assemblies** (`BlaInterview.Unit.Tests`, `BlaInterview.Integration.Tests`). Unit uses **Moq.AutoMock** + **Bogus** (`GenerateValid*` / `GenerateInvalid*`); integration uses **ICollectionFixture** only (removed redundant `IClassFixture`); split API tests into `AuthTests` + `TaskTests`; repository tests use **in-memory SQLite** (fake DB, real EF).
- **Expanded coverage:** `TaskItem` terminal/active statuses via `[Theory]` + `[MemberData]`; terminal status transitions; valid active transitions; create valid/invalid paths with `Verify` on repository.
- **Verified:** `dotnet test` — **28 pass** (19 unit + 9 integration).

## Improvements batch (2026-07-04)

- **Global exception handling:** `AppExceptionHandler` (`IExceptionHandler`) — controllers no longer use try/catch.
- **DTO split:** `AuthDtos.cs` separated from `TaskDtos.cs`.
- **Pagination:** `GET /api/tasks` returns `TaskListResponse` (`items`, `totalCount`, `page`, `pageSize`); defaults page 1 / size 100.
- **Query binding:** `TaskListQuery` in Api layer with `[FromQuery(Name = "status")]` → maps to `TaskFilterRequest`.
- **Frontend polish:** due-date urgency styling, empty column states, `BoardSkeleton` loading state.
- **Filter integration tests:** HTTP tests for list, search, status filter.
- **CI:** `.github/workflows/ci.yml` — `dotnet test` + `npm run build`.
- **NU1903:** pinned `SQLitePCLRaw.lib.e_sqlite3` 3.50.3 in Infrastructure + Integration tests.
- **Demo script:** `docs/interview-walkthrough.md`.
- **Testing seed:** `DatabaseSeeder` runs in `Testing` environment for integration tests.
- **Skipped:** richer domain model on `TaskItem` (deferred for discussion).
- **Verified:** `dotnet test` (28 pass), `npm run build`.

## 2026-07-04 — JWT session 401 on board load

- **User report:** Console 401 on `GET /api/tasks` while UI still showed logged-in state.
- **Root cause:** `isAuthenticated` only checked token presence in localStorage, not expiry; no global 401 handler to clear session or redirect.
- **Fixes:** `isTokenValid()` (stored `expiresAt` + JWT `exp`); `ApiError` + 401 handler in `client.ts` clears auth and notifies `AuthContext`; expired sessions cleared on app init; TanStack Query skips retry on 401; task-list path normalization fixed for query strings.
- **Verified:** `npm run build`.
