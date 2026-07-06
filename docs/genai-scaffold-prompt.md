# Technical Interview — Agreed Scaffold Prompt

> **Interview reminder:** This file is the **final consolidated prompt** from Phase 1 — the result of iterating with AI (user story, S1–S7, JWT, filters, terminal columns, etc.) until you approved and saved it (*"OK, save prompt"*). It is **not** the first raw chat; workflow summary → [AI-NOTES](../AI-NOTES.md). Implementation (Phase 2) started from here; what diverged afterward (two APIs, JWT/enum fixes, etc.) is in [README — Validation and corrections](../README.md#validation-and-corrections).
>
> Saved during Phase 1 alignment. Use this document as the prompt you would paste into a GenAI tool to generate the scaffold.
> **Do not use external identity providers (Clerk, Auth0, etc.).**
>
> **See also:** [User stories](user-stories.md) · [README GenAI workflow](../README.md#validation-and-corrections) · [README](../README.md) · [AI-NOTES](../AI-NOTES.md)

---

## User story

As a professional managing my own work, I want a secure personal Kanban board where I can create tasks with priorities and due dates, drag them across my workflow, and clearly mark work as done or canceled, so that I always know what to focus on next and can deliberately reopen work when plans change, without sharing or mixing tasks with other users.

See [docs/user-stories.md](user-stories.md) for scoped stories (S1–S7) and acceptance criteria.

---

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10 (`net10.0`), ASP.NET Web API (MVC controllers) |
| Architecture | Clean Architecture — KISS, SOLID |
| Database | SQLite, EF Core, LINQ, Migrations |
| Auth | ASP.NET Core Identity + JWT (users in our DB, password hash) |
| Validation | FluentValidation (Application layer) |
| Mapping | AutoMapper |
| Tests | xUnit, Moq, TDD preferred |
| Frontend | React 19, Vite, TypeScript, **shadcn/ui**, Tailwind CSS, TanStack Query |
| Drag-and-drop | `@dnd-kit/core` |

**App name:** **Simple Tasks** (sidebar header / browser title)

---

## Solution structure

```
simple-tasks/
  src/
    BlaInterview.Domain/           # Entities, enums — no EF, no Identity
    BlaInterview.Application/      # DTOs, interfaces, services, validators, AutoMapper profiles
    BlaInterview.Infrastructure/   # EF Core DbContext, Identity, repositories, JWT, migrations
    BlaInterview.Api/              # Controllers, auth middleware, DI wiring
  tests/
    BlaInterview.Domain.Tests/
    BlaInterview.Application.Tests/
    BlaInterview.Infrastructure.Tests/
    BlaInterview.Api.Tests/
  client/                          # React 19 + Vite + TypeScript
  docs/
    genai-scaffold-prompt.md
    user-stories.md
  AI-NOTES.md
  README.md
```

### Layer rules

- **Domain:** pure entities and enums; no EF, Identity, or ASP.NET references
- **Application:** business logic, validation, use cases, interfaces; no EF or HTTP
- **Infrastructure:** EF Core, Identity, repositories, JWT token generation, migrations
- **API:** thin controllers — routing and HTTP only; no business logic
- Do NOT put validation in controllers
- Do NOT put business rules in React components (UI calls API; rules live in Application)

---

## Domain model

### User (ASP.NET Core Identity)

- Stored via Identity (`ApplicationUser` or extend IdentityUser)
- Registration: **email + password only**
- Fields managed by Identity: `Id`, `Email`, `PasswordHash`

### Task

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | Primary key |
| `Title` | string | Required |
| `Description` | string | Optional |
| `Status` | enum | See Kanban columns below |
| `Priority` | enum | `Low`, `Medium`, `High`, `Urgent` |
| `DueDate` | DateTimeOffset? | Optional; date-only (UTC calendar day). Reject past dates on **create** and when **changing** to a different past date on update; allow unchanged overdue dates and clearing. Stored as ISO 8601 UTC. |
| `UserId` | string (FK) | Owner — tasks belong to authenticated user |
| `CreatedAt` | DateTimeOffset (UTC) | Set on create (server-side) |
| `UpdatedAt` | DateTimeOffset (UTC) | Set on create; update on every change |

### TaskStatus enum (maps 1:1 to Kanban columns)

| Column (UI) | Enum value | Default on create |
|-------------|------------|-------------------|
| To Do | `Todo` | Yes |
| In Progress | `InProgress` | |
| On Hold | `OnHold` | |
| In Review | `InReview` | |
| Done | `Completed` | Terminal |
| Canceled | `Cancelled` | Terminal |

### TaskPriority enum

`Low`, `Medium`, `High`, `Urgent`

---

## Status transition rules (Option C — hybrid)

### Active columns — free drag

Among `Todo`, `InProgress`, `OnHold`, `InReview`: any status may change to any other active status.

### Into terminal columns — drag allowed

From any active column → `Completed` or `Cancelled` via drag or `PUT`.

### Out of terminal columns — drag blocked

- Cannot drag from Done or Canceled to any other column
- `PUT /api/tasks/{id}` with a new status when current is `Completed` or `Cancelled` → **400 Bad Request**
- Message: terminal tasks must be reactivated before changing status
- **Non-status fields** (title, description, priority, due date) **may still be updated** on terminal tasks without reactivate

### Reactivate (only way out of terminal)

```
POST /api/tasks/{id}/reactivate
Authorization: Bearer {token}
```

| Condition | Response |
|-----------|----------|
| Status is `Completed` or `Cancelled`, user is owner | **200** — status set to `Todo` |
| Status is active | **400** |
| Not owner | **403** |
| No JWT | **401** |

UI: Done and Canceled cards show **Move to To Do** (or Reopen) button — not draggable out.

---

## API endpoints

### Public routes (no JWT)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | `{ email, password }` — create user |
| POST | `/api/auth/login` | `{ email, password }` → `{ token, expiresAt }` |
| POST | `/api/auth/logout` | **204** — client clears token; optional no-op on server (stateless JWT) |
| GET | `/api/health` | Optional health check |

### Protected routes (`[Authorize]` — JWT Bearer)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/tasks` | List current user's tasks (supports filters — see below) |
| GET | `/api/tasks/{id}` | Get one task (ownership enforced) |
| POST | `/api/tasks` | Create task (default status `Todo`) |
| PUT | `/api/tasks/{id}` | Update task fields and/or status (terminal rules apply) |
| DELETE | `/api/tasks/{id}` | Delete task |
| POST | `/api/tasks/{id}/reactivate` | Move terminal task back to `Todo` |

### Auth error responses

| Case | Status |
|------|--------|
| No token / invalid token on protected route | **401 Unauthorized** |
| Access another user's task | **403 Forbidden** |
| Validation failure | **400 Bad Request** |
| Task id does not exist | **404 Not Found** |
| Task exists but belongs to another user | **403 Forbidden** |

### GET /api/tasks — query parameters (all optional, AND logic)

| Param | Type | Behavior |
|-------|------|----------|
| `search` | string | Title contains term, case-insensitive |
| `status` | enum (repeatable) | Multi-select status filter |
| `createdFrom` | date (ISO) | `CreatedAt >= start of day` |
| `createdTo` | date (ISO) | `CreatedAt <= end of day` |
| `updatedFrom` | date (ISO) | `UpdatedAt >= start of day` |
| `updatedTo` | date (ISO) | `UpdatedAt <= end of day` |

Validation: invalid status → **400**; `from > to` on date ranges → **400**.

**Filter defaults:**

- Empty `search` → no title filter (show all matching other criteria)
- No status params or all statuses selected → no status filter (show all)
- No date params or preset **Any** → no date filter for that field (show all)
- Active filters combine with **AND** only when explicitly set

---

## FluentValidation rules

### Register

- Valid email format, required
- Password must follow ASP.NET Core Identity security defaults (see Password policy below)

### Login

- Email and password required

### Create / Update Task

- Title: required, max length **200**
- Description: optional, max length **2000**
- Status: valid enum; terminal transition rules enforced in service layer
- Priority: valid enum; default `Medium` on create if omitted
- DueDate: optional (date only). **Create:** must not be in the past. **Update:** reject only when the user **changes** the due date to a different past calendar day; unchanged overdue dates and clearing (`null`) are allowed. Rationale: overdue and completed tasks keep realistic due dates; backdating is blocked.

---

## Password policy (Identity)

Configure `PasswordOptions` for security best practices:

- Minimum length: **8** characters
- Require **digit**, **lowercase**, **uppercase**
- Require **non-alphanumeric** character (or at least one special character)
- Enable **lockout** after repeated failed login attempts (e.g. 5 attempts, 5-minute lockout)
- Identity handles password hashing (PBKDF2) — never store plain text

Duplicate email on register → **400** or **409** with clear message (do not leak stack traces).

---

## Date and timezone handling

- **Store** all timestamps (`CreatedAt`, `UpdatedAt`, `DueDate`) as **UTC** in the database using `DateTimeOffset`
- **API** accepts and returns ISO 8601 with timezone offset (e.g. `2026-07-04T15:00:00-03:00`)
- **Frontend** displays dates in the **user's local timezone**; converts local date boundaries to UTC when sending filter ranges
- Date filters: `createdFrom` / `createdTo` interpreted as start/end of day in user's timezone, converted to UTC for queries

---

## JWT configuration

- Scheme: `Authorization: Bearer {token}`
- Token issued on successful login
- Configurable expiry (e.g. 60 minutes) via Options pattern
- Users stored in SQLite via ASP.NET Core Identity
- **Storage:** **`localStorage`** (Option A — chosen for demo UX)

### JWT in localStorage — demo tradeoff (document in README)

For **interview/demo purposes**, the JWT is stored in `localStorage` and sent as `Authorization: Bearer {token}`. This keeps React + Swagger + Postman testing simple and survives page refresh. **Tradeoff:** any XSS on the page could read the token — in production we would prefer HttpOnly cookies or short-lived access tokens with refresh. Mitigations for demo: short JWT expiry (~60 min), no sensitive data in token payload beyond `sub` / email, and no token logging.

### JWT storage options (reference — Option A chosen)

| Option | Pros | Cons |
|--------|------|------|
| **localStorage** ✅ | Survives refresh; simple; Swagger/Postman-friendly | XSS can steal token |
| **sessionStorage** | Per-tab; cleared on close | Still XSS-vulnerable |
| **In-memory** | Not persisted | Lost on refresh |
| **HttpOnly secure cookie** | Best XSS protection | CORS + credentials; harder Swagger setup |

---

## Swagger / OpenAPI

- Enable Swagger UI in **Development** environment
- Document auth: **Bearer JWT** scheme so protected endpoints can be tested from Swagger
- Use for manual validation during interview (401 without token, CRUD, filters)

---

## Design patterns

Apply these patterns consistently (KISS — no over-engineering):

| Pattern | Where | Purpose |
|---------|-------|---------|
| **Clean Architecture / Layered** | Solution structure | Separation of concerns; Domain independent of infrastructure |
| **Repository** | `ITaskRepository` in Application; impl in Infrastructure | Abstract data access from business logic |
| **Application Service (Use Case)** | `TaskService`, `AuthService` in Application | Orchestrate business rules, validation, ownership |
| **DTO** | Request/response models in Application | Decouple API contracts from domain entities |
| **AutoMapper** | Application profiles | Map Entity ↔ DTO without manual boilerplate |
| **Dependency Injection** | `Program.cs` / extension methods | Loose coupling; testability with Moq |
| **Options pattern** | `JwtSettings`, `PasswordOptions` | Strongly typed configuration |
| **FluentValidation** | Application validators | Validation rules separate from controllers and entities |
| **Strategy (implicit)** | Identity password validators, JWT bearer handler | Framework-provided extensibility |
| **Unit of Work (via DbContext)** | Infrastructure | EF `DbContext` coordinates saves; avoid redundant UoW wrapper unless needed |

**Avoid for v1:** Full CQRS/MediatR, event sourcing, generic repository over-abstraction.

---

## Unit test expectations (xUnit + Moq)

Write tests for **Application**, **Infrastructure**, and **API** layers. TDD preferred — failing test first where possible.

### Application / service tests

- `CreateTask_PastDueDate_Returns400`
- `UpdateStatus_TodoToOnHold_Succeeds`
- `UpdateStatus_FromCompletedToInProgress_Returns400`
- `Reactivate_CompletedTask_ReturnsTodo`
- `Reactivate_NonTerminalTask_Returns400`
- `GetTask_OtherUsersTask_Returns403`

### Infrastructure tests

- Repository CRUD and filter queries (search, status, date ranges)
- LINQ filter logic for combined AND filters

### API tests

- Protected endpoint without token → **401**
- Register / login happy path
- CRUD happy path with mocked or test DB
- Filter query params return correct subset

### Integration tests (API)

- Use **`WebApplicationFactory`** in `BlaInterview.Api.Tests` for end-to-end HTTP tests against the real pipeline (with test SQLite DB)
- Cover: auth flow, CRUD, 401/403/404, filters, reactivate, terminal status rules

---

## Frontend requirements

### Auth pages (minimal dark theme — shadcn/ui)

- App branding: **Simple Tasks**
- Login and Register — separate from dashboard; built with **shadcn/ui** components
- Email + password only on register
- After register → **auto-login** → redirect to board
- Store JWT in **`localStorage`**; send `Authorization: Bearer {token}` on all API calls
- **TanStack Query** for API calls, caching, loading, and error states
- Redirect unauthenticated users to login
- **Logout** in sidebar — **no confirmation modal**; instant logout (see Logout section)
- **Loading states:** spinner or skeleton while fetching tasks, during login/register, and on drag-drop API calls
- **Error states:** show API validation errors to user (toast or inline)

### Kanban dashboard (simplified dark reference UI)

- Fixed sidebar + main board area
- Dark theme with accent color (neon green from reference)
- Six Kanban columns matching status enum
- Task cards: title, description snippet, priority tag, due date
- FAB or **+** button for create task
- Modal / slide-over for create and edit
- `@dnd-kit` for drag-and-drop between columns
- Done and Canceled: drop targets only; cards not draggable out; **Move to To Do** button calls reactivate
- Delete task: card menu → confirmation dialog (shadcn Dialog)

### Dev defaults

| Topic | Value |
|-------|--------|
| API URL | `https://localhost:7xxx` (document in README) |
| Vite dev | `http://localhost:5173` |
| CORS | Allow Vite origin with credentials headers as needed |
| SQLite file | `src/BlaInterview.Auth.Api/tasks.db` |
| Migrations + seed | Auto on API startup in **Development** |
| DELETE response | **204 No Content** |

### Responsive — small desktop (~1024px–1280px)

- Board container: `overflow-x: auto`
- Columns: min-width (~280–320px) so they don't collapse
- **Shift + mouse wheel** → horizontal scroll (custom wheel handler)
- Dismissible tip near board: *"Tip: Hold Shift and scroll to move sideways across columns."*

### Filtering (sidebar — S7)

| Control | Behavior |
|---------|----------|
| Search | `Search tasks...` — debounced, title only, case-insensitive |
| Status | Multi-select checkboxes; default all selected; **none selected = treat as all** (show all tasks) |
| Created date | Presets (Any, Today, Last 7 days, Last 30 days) + custom from/to |
| Updated date | Same presets + custom from/to |
| Clear filters | Resets all controls |

**Filtered board behavior:**

- Hide cards that don't match filters
- Column header counts reflect **filtered** visible tasks only
- Drag-and-drop allowed on visible cards; refresh after drop (card may disappear if no longer matches)

Presets resolved to date ranges in frontend before API call. Preset **Any** or empty date range → omit date params (show all).

Empty search string → omit `search` param (no title filter).

---

## Logout

### User story (part of S1 — see [user-stories.md](user-stories.md))

Logged-in user clicks **Logout** in sidebar → session ends → redirect to login. **No confirmation modal.**

### Behavior (stateless JWT + localStorage)

1. User clicks **Logout** in sidebar (instant — no modal)
2. Frontend **removes JWT from localStorage** and clears cached user/task state
3. Redirect to `/login`
4. Protected routes and API calls return **401** until re-login

Optional: `POST /api/auth/logout` returns **204** as a no-op (stateless JWT — no server-side blacklist in v1).

### What to clear on logout

- JWT token from `localStorage`
- React Query / cached task list / auth context

### UX

- Logout button: sidebar bottom (below user email)
- No confirmation modal

---

## Testing auth (Bearer JWT in localStorage)

**Browser (primary demo):** Log in via React → token saved in `localStorage` → DevTools → Application → Local Storage to confirm (demo only — do not do this in production). Network tab shows `Authorization: Bearer ...` on API calls. Logout clears storage; next call returns **401**.

**Swagger:** Click **Authorize**, paste `Bearer {token}` from login response, test protected endpoints.

**Postman:** `POST /login` → copy `token` from body → set Authorization header on collection.

---

## Seed data

Two users with **distinct demo scenarios** — credentials documented in README.

### User 1 — `demo@example.local` / `Demo123!` (primary demo)

**Scenario:** Active professional with a full workflow board.

- Tasks in **every column** (Todo, InProgress, OnHold, InReview, Completed, Cancelled)
- Mix of priorities (Low → Urgent)
- Mix of due dates (future, today, and **past** for overdue/completed demo scenarios)
- At least one task per column for Kanban + drag demo
- Good dataset for **search/filter** demo (varied titles)

### User 2 — `other@example.local` / `Other123!` (ownership / 403 demo)

**Scenario:** Second user with a smaller, distinct board.

- Fewer tasks (e.g. 3–4) with clearly different titles (e.g. prefix `"[Other]"`)
- Different status distribution (e.g. mostly InProgress and Done)
- Used to prove **403 Forbidden** when User 1 tries to access User 2's task by ID
- README documents: log in as `demo@...`, attempt `GET /api/tasks/{other-user-task-id}` → 403

---

## Out of scope (v1)

- Multi-project workspaces and recent projects list
- Team collaboration: assignees, avatars, share/private toggle
- Comments, attachments, progress bars on cards
- Calendar view implementation (sidebar nav stub OK)
- External identity providers (Clerk, Auth0)
- Strict status transition matrix (v1 uses hybrid Option C)
- Search in description (title only in v1)

---

## Explicit "Do NOT" constraints

- Do NOT use Clerk, Auth0, or external identity providers
- Do NOT put business logic in API controllers
- Do NOT put EF Core or Identity in Domain project
- Do NOT put FluentValidation in controllers
- Do NOT skip protected route demo — **401** must be testable
- Do NOT allow drag-out of Done/Canceled without reactivate endpoint
- Do NOT implement until explicitly approved (Phase 2)

---

## Implementation phases

### Phase 2 — Scaffold + core (v1)

- Solution structure (Clean Architecture layers)
- User (Identity) + Task entities, migrations
- Repositories/services, CRUD endpoints, auth endpoints, reactivate
- FluentValidation rules in Application layer
- Core unit tests
- Basic React hookup: auth, Kanban, drag-drop, filters

### Full implementation (after scaffold)

- UI polish matching dark Kanban reference
- Seed data, README with setup + demo credentials
- Edge case coverage and GenAI documentation ([AI-NOTES.md](../AI-NOTES.md), [README GenAI section](../README.md#genai-workflow))

---

## GenAI deliverables (end of project)

1. **One main prompt** — this document
2. **Sample AI output** — representative entity, controller, service, validator, or test
3. **Validation** — `dotnet build`, `dotnet test`, Swagger/Postman, 401 without token
4. **Corrections** — at least 2–3 real things AI got wrong and what changed
5. **Edge cases** — auth, validation, ownership (403), terminal status rules

Maintain [AI-NOTES.md](../AI-NOTES.md) as a short GenAI workflow summary if the process changes materially.

---

**See also:** [README validation & corrections](../README.md#validation-and-corrections) · [README demo flow](../README.md#demo-flow)
