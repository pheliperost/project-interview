# Simple Tasks — Technical Interview

## Overview

Full-stack submission for the BLA .NET technical interview (exercise v6): a personal Kanban board where each user manages only their own tasks. Scope comes from the [user story](#user-story) and S1–S7 below, not from a feature checklist.

| | |
|---|---|
| **Stack** | .NET 10, ASP.NET Web API (Clean Architecture), SQLite, EF Core, Identity + JWT, FluentValidation, React 19, Vite, TanStack Query, xUnit |
| **APIs** | Auth `:5098` (register, login, JWT) · Tasks `:5099` (CRUD, filters, reactivate) |
| **Tests** | **159** (95 unit + 64 integration) |
| **GenAI** | [Prompt](docs/genai-scaffold-prompt.md) · [sample output](#sample-output) · [validation](#validation-and-corrections) · [AI-NOTES](AI-NOTES.md) |
| **Demo** | [Run locally](#run-locally) · [Demo flow](#demo-flow) (~5 min) |

**Exercise alignment:** two segregated APIs, data + business layers separate from controllers, CRUD with auth, frontend integration, seeded demo users, README setup — plus Kanban workflow, filters, and demo-only password reset beyond the minimal GenAI task-CRUD example.

## User story

**Informal story** (drives the exercise scope and demo narrative):

> As a professional managing my own work, I want a secure personal Kanban board where I can create tasks with priorities and due dates, drag them across my workflow, and clearly mark work as done or canceled, so that I always know what to focus on next and can deliberately reopen work when plans change, without sharing or mixing tasks with other users.

**Scoped stories** derived from that narrative:

| # | Story | Outcome |
|---|--------|---------|
| S1 | Register, log in, log out, forgot password | JWT session; auto-login after register; demo reset link card |
| S2 | Private task ownership | My tasks only; another user's ID → 403 |
| S3 | Create and edit tasks | Title, description, priority, optional due date; validation on save |
| S4 | Kanban board view | Six columns; priority badges and due-date urgency |
| S5 | Drag-and-drop status | Move cards across active columns; drop into Done/Canceled |
| S6 | Reactivate finished work | Terminal columns: reopen via dedicated endpoint → To Do |
| S7 | Search and filter | Sidebar filters; non-matching cards hidden while filtered |

Full acceptance criteria for each story: [docs/user-stories.md](docs/user-stories.md) (optional deep-dive for reviewers).

## How the APIs work together

```
Browser (localhost:5173)
    │  /api/auth/*  ──►  Auth API :5098  ──►  register / login / logout
    │                         │
    │                         └── issues JWT (shared Jwt:Secret)
    │
    │  /api/tasks/* ──►  Tasks API :5099  ──►  CRUD + reactivate (Bearer required)
    │
    └── both APIs ──►  shared SQLite  (src/BlaInterview.Auth.Api/tasks.db)
```

Auth API owns users (Identity). Tasks API validates the same JWT but never issues tokens. Start **Auth first**, then Tasks, then the client.

## Key design decisions

Structural choices for the interview presentation; CRUD, auth, and per-user data are covered elsewhere in this README.

| Area | Topic | Choice | Rationale |
|------|-------|--------|-----------|
| Architecture | Layering | Clean Architecture | Domain entities; `TaskService` + FluentValidation in Application; EF/repos in Infrastructure; thin controllers |
| Data | Persistence | SQLite + EF Core | Single shared DB for both APIs in demo; migrations for reproducible seed |
| Frontend | Client | React SPA + Web API | PDF mentions MVC; SPA fits Kanban UX and full-stack CRUD integration |
| Product | Kanban workflow | Terminal Done/Canceled | Drag into terminal columns; `POST /api/tasks/{id}/reactivate` to reopen — beyond flat task CRUD |

## Solution layout

```
src/           # Domain, Application, Infrastructure, Api.Shared, Auth.Api, Tasks.Api
tests/         # BlaInterview.Unit.Tests + BlaInterview.Integration.Tests
client/        # React frontend
docs/          # user stories, GenAI prompt, troubleshooting — see [Documentation map](#documentation-map)
```

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

## Configuration (first-time setup)

Copy the example settings so both APIs share the same signing key (PowerShell, from repo root):

```powershell
cd src/BlaInterview.Auth.Api
Copy-Item appsettings.Development.example.json appsettings.Development.json

cd ..\BlaInterview.Tasks.Api
Copy-Item appsettings.Development.example.json appsettings.Development.json
```

`appsettings.Development.json` is gitignored. Committed `appsettings.json` uses a non-functional placeholder — tokens will not work until both APIs share the same `Jwt:Secret` (≥ 32 characters) via Development settings or `Jwt__Secret` env var. Integration tests inject their own JWT settings and do not need this file.

## Run locally

Start **Auth API first** (creates users), then **Tasks API** (seeds tasks), then the client. Use **three terminals** (PowerShell, from repo root):

```powershell
# Terminal 1 — Auth API
cd src/BlaInterview.Auth.Api
dotnet run
# → http://localhost:5098

# Terminal 2 — Tasks API
cd src/BlaInterview.Tasks.Api
dotnet run
# → http://localhost:5099

# Terminal 3 — React client (npm install once)
cd client
npm install
npm run dev
# → http://localhost:5173
```

| API | URL | Swagger |
|-----|-----|---------|
| Auth | http://localhost:5098 | http://localhost:5098/swagger |
| Tasks | http://localhost:5099 | http://localhost:5099/swagger |

The Vite dev server proxies `/api/auth` → 5098 and `/api/tasks` → 5099.

Issues? See [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

## API reference

### Auth API (`:5098`)

| Method | Route | Access |
|--------|-------|--------|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/forgot-password` | Public — demo returns reset link in response when account exists |
| POST | `/api/auth/reset-password` | Public |
| GET | `/api/health` | Public |
| POST | `/api/auth/logout` | Bearer JWT |

### Tasks API (`:5099`) — all routes require Bearer JWT

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/tasks` | Paginated list; optional search, status, date filters |
| GET | `/api/tasks/{id}` | Owner only → 403 if another user's task |
| POST | `/api/tasks` | Default status `Todo` |
| PUT | `/api/tasks/{id}` | Terminal status rules enforced |
| DELETE | `/api/tasks/{id}` | 204 No Content |
| POST | `/api/tasks/{id}/reactivate` | Done/Canceled → Todo |

Unauthenticated calls to Tasks routes return **401**.

## Demo credentials

| User | Email | Password | Purpose |
|------|-------|----------|---------|
| Primary | `demo@example.local` | `Demo123!` | **8 seeded tasks** across all columns ([seed scenarios](docs/user-stories.md#seed-data-scenarios-demo)) |
| Secondary | `other@example.local` | `Other123!` | 4 tasks + 403 ownership demo |

On first start in **Development**, the database is migrated and seeded. The demo account gets **8 tasks** (2 To Do, 2 In Progress, 1 On Hold, 1 In Review, 1 Completed, 1 Cancelled). New registrations start with an **empty board**. Login form is pre-filled with demo credentials.

**If the board is empty after sign-in:**

1. Sign in as **`demo@example.local`** / **`Demo123!`** — not a newly registered user.
2. Start **Auth API first**, then Tasks API (seeding runs at startup).
3. Delete `src/BlaInterview.Auth.Api/tasks.db` and restart both APIs if the demo board is stale.

**403 ownership check:** Log in as demo in Swagger, copy a task ID from `other@example.local`'s list, call `GET /api/tasks/{id}` with demo's token → **403**. Covered by `TaskOwnershipTests`.

### Password reset (demo only)

Uses ASP.NET Core Identity reset tokens (`GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`). **No real email is sent** — a `FakeEmailSender` records the reset link for tests only.

For the interview demo, when the email **exists**, the API includes `resetLink` in the forgot-password response and the UI shows an amber **Demo only** card with a button to open the reset page. Unknown emails get the same generic message with **no card** (no link leaked). In production you would send the link by email and never return it in the API body.

Try it: Sign in page → **Forgot password?** → `demo@example.local` → open reset link → set a new password → sign in.

## Demo flow (~5 min)

Suggested order for a live presentation or smoke test:

1. **Sign in** as demo user → **8 seeded tasks** across columns.
2. **Kanban** — drag between active columns; drop into Done/Canceled; **Move to To Do** (reactivate) on terminal cards.
3. **CRUD** — create, edit, delete a task (validation on save).
4. **Filters** — search by title, filter by status and dates.
5. **Optional** — register a second user → empty board (per-user data).
6. **Optional** — Swagger: login on Auth API → Authorize with JWT → `GET /api/tasks` on Tasks API.

Ownership and auth behavior (403 / 401 / terminal rules) are in [user-stories.md](docs/user-stories.md) (S2, S5, S6) — demonstrate only if the panel asks.

## Tests

```powershell
dotnet test
```

**Last verified:** **159 pass** — 95 unit + 64 integration.

| Assembly | Focus |
|----------|--------|
| `BlaInterview.Unit.Tests` | Domain, services, validators, notifications, mapper, auth/JWT, exception handler |
| `BlaInterview.Integration.Tests` | Dual-host WAF (Auth + Tasks factories), repository, ownership, filters, password reset |

Filter by trait: `dotnet test --filter "Category=Task Service"`. Optional coverage: `dotnet test --collect:"XPlat Code Coverage"`.

Lint: `cd client; npm run verify` (oxlint + TypeScript). CI runs `dotnet build`, `dotnet test`, `npm run verify`, and `npm run build` on every push/PR.

## GenAI workflow

Built with AI assistance (Cursor). The exercise asks for the prompt, sample output, validation, and corrections.

### Prompt excerpt

Phase 1 locked scope (user story, S1–S7, API contract) before any code. Representative opening:

```
Build a personal Kanban task API in ASP.NET Core using Clean Architecture.
- SQLite + EF Core, Identity + JWT, FluentValidation, xUnit tests
- Task: title, description, status (6 Kanban columns), priority, due date, userId
- Segregated concerns: thin controllers, rules in Application layer
- React 19 client: drag-and-drop (@dnd-kit), filters, shadcn/ui
- Per-user isolation; terminal Done/Canceled with reactivate endpoint
```

Full prompt: [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md).

### Sample output

**Representative scaffold (thin controller → service):**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskService _taskService;

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        CreateTaskBody body, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest(UserId, body), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return CreatedAtAction(nameof(GetTask), new { id = task!.Id }, task);
    }
}
```

### Validation and corrections

The saved prompt is the Phase 1 spec in [genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). Below: how output was validated, what diverged from the prompt, and how edge cases / auth / validation were handled.

**How we validated AI output**

- `dotnet build` and `dotnet test` (**159 pass**) — unit + dual-host integration (Auth + Tasks WebApplicationFactory)
- `npm run verify` and `npm run build` on the React client
- Manual smoke: login, CRUD, drag-and-drop, reactivate, filters, 403 on another user's task ([demo flow](#demo-flow))
- Acceptance criteria in [user-stories.md](docs/user-stories.md) (S1–S7)

**Prompt vs as-built** (intentional deviations)

| Prompt / spec | As built | Why it changed |
|---------------|----------|----------------|
| Single `BlaInterview.Api` host | `Auth.Api` (:5098) + `Tasks.Api` (:5099) + `Api.Shared` | Segregated auth and task APIs per interview exercise |
| Manual mapper (early scaffold) | AutoMapper `TaskProfile` | Replaced manual mapper for maintainability |
| 4 test projects (Domain, Application, Infrastructure, Api) | 2 assemblies (`Unit.Tests`, `Integration.Tests`) | Simpler layout; Moq.AutoMock + Bogus |
| `TaskStatus` enum | `KanbanStatus` | Name clash with `System.Threading.Tasks.TaskStatus` |
| Identity + JWT (both registered) | JWT as default authenticate/challenge scheme | AI scaffold left Identity cookie as default → 401 on protected routes |
| Enum serialization (unspecified) | `JsonStringEnumConverter` + client `normalizeTask()` | API returned `0`–`5`; UI grouped by `"Todo"` → empty board |
| Repository sort via LINQ | In-memory sort for `DateTimeOffset` | SQLite does not support `ORDER BY` on `DateTimeOffset` |

Kanban columns, priorities, due dates, filters (S7), terminal Done/Canceled rules, reactivate endpoint, per-user isolation (403), and JWT auth **match the agreed prompt**. Deviations are mostly structure, tooling, and fixes found during validation.

**Edge cases, auth, and validation** (not left to AI defaults)

| Area | Approach |
|------|----------|
| Auth | Identity + JWT Bearer; 401 without token; logout clears `localStorage` |
| Ownership | Task endpoints scoped by `userId`; another user's task → **403**; missing id → **404** |
| Validation | FluentValidation in Application (`CreateTaskBody`, `UpdateTaskBody`, filters, auth DTOs) |
| Terminal status | Done/Canceled: drag in only; status change via update blocked; `POST /reactivate` → Todo |
| Due dates | Past due rejected on create; update rules allow unchanged historical due dates |
| Filters | Invalid date range or page size → **400**; non-matching cards hidden while filtered |

Workflow preview (how AI was used): [AI-NOTES.md](AI-NOTES.md).

## Documentation map

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Setup, API overview, demo flow, design decisions |
| [docs/user-stories.md](docs/user-stories.md) | S1–S7 acceptance criteria |
| [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md) | Full GenAI prompt |
| [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Common local setup issues |
| [AI-NOTES.md](AI-NOTES.md) | GenAI workflow preview (how AI was used) |
