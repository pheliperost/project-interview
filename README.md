# Simple Tasks — Technical Interview

Personal Kanban task board built with **Clean Architecture** (.NET) and **React 19**.

## How this maps to the BLA exercise (PDF v6)

- **User story & demo** — informal narrative below + [presentation outline](docs/interview-walkthrough.md)
- **Two APIs** — Auth (`:5098`) issues JWT; Tasks (`:5099`) CRUD + ownership (**403**)
- **Clean Architecture** — Domain → Application (rules/validation) → Infrastructure (EF) → API hosts
- **Tests** — **154** (91 unit + 63 integration); expanded after GenAI scaffold, not full TDD on every story
- **GenAI deliverables** — [prompt](docs/genai-scaffold-prompt.md), [sample output](#sample-output), [validation & fixes](#validation-and-corrections)
- **Beyond PDF minimum** — six-column Kanban, search/filters, reactivate; password reset is demo-only (no email)

> **Presentation outline:** [docs/interview-walkthrough.md](docs/interview-walkthrough.md)

## At a glance

- **Product:** Six-column Kanban — drag-and-drop, priorities, due dates, search/filters, terminal Done/Canceled with reactivate
- **Backend:** Segregated **Auth API** (`:5098`) and **Tasks API** (`:5099`) sharing one SQLite database
- **Quality:** **154 tests** (91 unit + 63 integration), FluentValidation, ASP.NET Core Identity + JWT
- **GenAI:** Scope aligned before coding — [prompt](docs/genai-scaffold-prompt.md), [prompt vs result](docs/genai-prompt-vs-result.md), [session log](AI-NOTES.md)

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

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10, ASP.NET Web API, Clean Architecture |
| Database | SQLite, EF Core, migrations |
| Auth | ASP.NET Core Identity + JWT (localStorage for demo) |
| Validation | FluentValidation |
| Tests | xUnit, Moq, WebApplicationFactory |
| Frontend | React 19, Vite, TypeScript, TanStack Query, Tailwind, @dnd-kit |

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

| Topic | Choice | Rationale |
|-------|--------|-----------|
| API layout | Auth + Tasks hosts | Matches segregated-API exercise; clear auth vs domain boundary |
| Persistence | EF Core + migrations | Migrations and LINQ over hand-written SQL for this scope |
| Kanban rules | Terminal Done/Canceled | Drag in only; `POST /api/tasks/{id}/reactivate` to reopen |
| Multi-user | Per-user task isolation | Another user's task ID → **403** (not 404) |
| JWT storage | `localStorage` (demo) | Simple for React + Swagger; HttpOnly cookies preferred in production |
| Secrets in git | Placeholder in `appsettings.json` | Real `Jwt:Secret` only in gitignored `appsettings.Development.json` |

## Solution layout

```
src/           # Domain, Application, Infrastructure, Api.Shared, Auth.Api, Tasks.Api
tests/         # BlaInterview.Unit.Tests + BlaInterview.Integration.Tests
client/        # React frontend
docs/          # user stories, GenAI prompt, interview walkthrough — see [Documentation map](#documentation-map)
```

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

## Configuration (first-time setup)

Copy the example settings so both APIs share the same signing key:

```bash
cd src/BlaInterview.Auth.Api
cp appsettings.Development.example.json appsettings.Development.json   # PowerShell: Copy-Item ...

cd ../BlaInterview.Tasks.Api
cp appsettings.Development.example.json appsettings.Development.json
```

`appsettings.Development.json` is gitignored. Committed `appsettings.json` uses a non-functional placeholder — tokens will not work until both APIs share the same `Jwt:Secret` (≥ 32 characters) via Development settings or `Jwt__Secret` env var. Integration tests inject their own JWT settings and do not need this file.

## Run locally

Start **Auth API first** (creates users), then **Tasks API** (seeds tasks), then the client.

```bash
cd src/BlaInterview.Auth.Api && dotnet run    # → http://localhost:5098
cd src/BlaInterview.Tasks.Api && dotnet run   # → http://localhost:5099
cd client && npm install && npm run dev       # → http://localhost:5173
```

| API | URL | Swagger |
|-----|-----|---------|
| Auth | http://localhost:5098 | http://localhost:5098/swagger |
| Tasks | http://localhost:5099 | http://localhost:5099/swagger |

The Vite dev server proxies `/api/auth` → 5098 and `/api/tasks` → 5099.

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

**If you don't see cards:**

1. Sign in as **`demo@example.local`** / **`Demo123!`** — not a newly registered user.
2. Restart both APIs after pulling changes (seeding runs at startup).
3. Delete `src/BlaInterview.Auth.Api/tasks.db` and restart both APIs if the demo board is stale.

**403 ownership check:** Log in as demo in Swagger, copy a task ID from `other@example.local`'s list, call `GET /api/tasks/{id}` with demo's token → **403**. Covered by `TaskOwnershipTests`.

### Password reset (demo only)

Uses ASP.NET Core Identity reset tokens (`GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`). **No real email is sent** — a `FakeEmailSender` records the reset link for tests only.

For the interview demo, when the email **exists**, the API includes `resetLink` in the forgot-password response and the UI shows an amber **Demo only** card with a button to open the reset page. Unknown emails get the same generic message with **no card** (no link leaked). In production you would send the link by email and never return it in the API body.

Try it: Sign in page → **Forgot password?** → `demo@example.local` → open reset link → set a new password → sign in.

## Tests

```bash
dotnet test
```

**Last verified:** **154 pass** — 91 unit + 63 integration.

| Assembly | Focus |
|----------|--------|
| `BlaInterview.Unit.Tests` | Domain, services, validators, notifications, mapper, auth/JWT, exception handler |
| `BlaInterview.Integration.Tests` | Dual-host WAF (Auth + Tasks factories), repository, ownership, filters |

Filter by trait: `dotnet test --filter "Category=Task Service"`. Optional coverage: `dotnet test --collect:"XPlat Code Coverage"`.

## Lint and static analysis

**Frontend** (`client/`):

```bash
cd client
npm run lint        # oxlint (correctness, React, TypeScript, a11y, import)
npm run lint:fix    # auto-fix where supported
npm run typecheck   # tsc -b
npm run verify      # typecheck + lint
```

**Backend** (repo root): `Directory.Build.props` enables .NET analyzers (`AnalysisLevel=latest`, `AnalysisMode=Recommended`). Style rules live in `.editorconfig`.

CI runs `dotnet build`, `dotnet test`, `npm run verify`, and `npm run build` on every push/PR.

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

Full prompt: [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). Deviations from prompt: [docs/genai-prompt-vs-result.md](docs/genai-prompt-vs-result.md).

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

Full API contract and domain rules: [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). Story acceptance criteria: [docs/user-stories.md](docs/user-stories.md).

### Validation and corrections

- `dotnet build`, `dotnet test` (**154 pass**), `npm run build`, manual smoke on Kanban flows
- **JWT 401 fix** — Identity cookie overrode Bearer; set default scheme to JWT Bearer
- **Enum JSON fix** — added `JsonStringEnumConverter` (API returned `0`–`5`, UI expected `"Todo"`)
- Other: SQLite `DateTimeOffset` sort in memory; `KanbanStatus` rename; Auth/Tasks split

Session log: [AI-NOTES.md](AI-NOTES.md).

## Presentation outline

Suggested demo flow for the interview panel (~5–10 min): [docs/interview-walkthrough.md](docs/interview-walkthrough.md) — user story, live demo, architecture, and GenAI deliverables (aligned with the BLA exercise PDF).

## Documentation map

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Setup, API overview, design decisions |
| [docs/interview-walkthrough.md](docs/interview-walkthrough.md) | Presentation outline (demo + architecture) |
| [docs/user-stories.md](docs/user-stories.md) | S1–S7 acceptance criteria |
| [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md) | Full GenAI prompt |
| [docs/genai-prompt-vs-result.md](docs/genai-prompt-vs-result.md) | Prompt vs as-built delta |
| [AI-NOTES.md](AI-NOTES.md) | AI session log (what changed and why) |
| [AGENT-HANDOFF.md](AGENT-HANDOFF.md) | Operational context for continued development |
