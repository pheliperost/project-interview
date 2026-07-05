# Simple Tasks — Technical Interview

Personal Kanban task board built with **Clean Architecture** (.NET) and **React 19**.

## User story

As a professional managing my own work, I want a secure personal Kanban board where I can create tasks with priorities and due dates, drag them across my workflow, and clearly mark work as done or canceled, so that I always know what to focus on next and can deliberately reopen work when plans change, without sharing or mixing tasks with other users.

See [docs/user-stories.md](docs/user-stories.md) for scoped stories S1–S7.

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10, ASP.NET Web API, Clean Architecture |
| Database | SQLite, EF Core, migrations |
| Auth | ASP.NET Core Identity + JWT (localStorage for demo) |
| Validation | FluentValidation |
| Tests | xUnit, Moq, WebApplicationFactory |
| Frontend | React 19, Vite, TypeScript, TanStack Query, Tailwind, @dnd-kit |

> **Note:** This machine uses .NET 10 SDK. Patterns match .NET 8 requirements from the exercise.

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

## Configuration (first-time setup)

For local development, copy the example settings file so the API can sign JWTs:

```bash
cd src/BlaInterview.Auth.Api
cp appsettings.Development.json.example appsettings.Development.json   # PowerShell: Copy-Item ...

cd ../BlaInterview.Tasks.Api
cp appsettings.Development.json.example appsettings.Development.json
```

`appsettings.Development.json` is gitignored on both APIs. The committed `appsettings.json` uses a non-functional placeholder JWT secret — **tokens will not work until both APIs share the same `Jwt:Secret`** (minimum 32 characters) via Development settings or `Jwt__Secret` env var.

Integration tests inject their own in-memory JWT settings and do not need this file.

## Run the APIs

Start **Auth API first** (creates users), then **Tasks API** (seeds tasks).

```bash
cd src/BlaInterview.Auth.Api
dotnet run
```

```bash
cd src/BlaInterview.Tasks.Api
dotnet run
```

| API | URL | Swagger |
|-----|-----|---------|
| Auth | http://localhost:5098 | http://localhost:5098/swagger |
| Tasks | http://localhost:5099 | http://localhost:5099/swagger |

- SQLite DB: `src/BlaInterview.Auth.Api/tasks.db` (shared by both APIs)

## Run the frontend

```bash
cd client
npm install
npm run dev
```

- App: http://localhost:5173 (proxies `/api/auth` → 5098, `/api/tasks` → 5099)

## Demo credentials

| User | Email | Password | Purpose |
|------|-------|----------|---------|
| Primary | `demo@example.local` | `Demo123!` | **8 seeded tasks** across all columns |
| Secondary | `other@example.local` | `Other123!` | 4 tasks + 403 ownership demo |

### Seeded tasks (demo user only)

On first API start in **Development**, the database is migrated and seeded. The **demo** account gets **8 tasks** (2 To Do, 2 In Progress, 1 On Hold, 1 In Review, 1 Completed, 1 Cancelled). Newly registered users start with an **empty board**.

**If you don't see cards:**

1. Sign in as **`demo@example.local`** / **`Demo123!`** (login form is pre-filled). Registering a new account will not show seed data.
2. Restart the API after pulling changes (seeding runs at startup).
3. If you previously logged in as demo with an empty DB, delete `src/BlaInterview.Auth.Api/tasks.db` and restart both APIs — seed runs per user when that user has zero tasks.

You can confirm seed data in Swagger: `POST /api/auth/login` → Authorize with the token → `GET /api/tasks` should return 8 items with `"status": "Todo"` (string enums, not numbers).

### 403 demo

Automated in `TaskOwnershipTests` (demo token → other user's task → 403). Manual check:

1. Log in as `demo@example.local`
2. Log in as `other@example.local` in Swagger, `GET /api/tasks`, copy a task ID
3. With demo token, `GET /api/tasks/{id}` → **403 Forbidden**

## Tests

```bash
dotnet test
```

**Last verified:** **105 pass** — 60 unit + 45 integration.

| Assembly | Focus |
|----------|--------|
| `BlaInterview.Unit.Tests` | Domain, `TaskService`, validators, notifications, mapper, **AuthService**, **JwtTokenService**, **AppExceptionHandler** (Moq.AutoMock + Bogus) |
| `BlaInterview.Integration.Tests` | Dual-host WAF (`AuthAppFactory` + `TasksAppFactory`, shared SQLite), repository (in-memory SQLite) |

### Integration API tests (by host)

| File | Host | Examples |
|------|------|----------|
| `AuthTests`, `AuthExtendedTests` | Auth (5098) | login, register, logout, health, 401 |
| `TaskTests`, `TaskCrudTests`, `TaskQueryTests` | Tasks (5099) | CRUD, filters, pagination, 400/404 |
| `TaskOwnershipTests` | Tasks + Auth JWT | demo user → other user's task → **403** |
| `AuthExtendedTests` (cross-host) | Both | JWT from Auth accepted by Tasks |

### Filter by category

```bash
dotnet test --filter "Category=Task Service"
dotnet test --filter "Category=Integration Web - Tasks"
dotnet test --filter "Category=Integration Web - Auth"
dotnet test --filter "Category=Task Repository"
```

### Coverage (optional)

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

## JWT demo tradeoff

For interview/demo purposes, JWT is stored in `localStorage`. In production, prefer HttpOnly cookies or short-lived tokens with refresh.

## Project structure

```
src/           # Domain, Application, Infrastructure, Api.Shared, Auth.Api, Tasks.Api
tests/         # BlaInterview.Unit.Tests + BlaInterview.Integration.Tests
client/        # React frontend
docs/          # genai-scaffold-prompt.md, genai-prompt-vs-result.md, user-stories.md
```

## GenAI workflow

This project was scaffolded and implemented with AI assistance (Cursor). Below is the workflow expected by the technical interview exercise.

### Prompt used

The full agreed specification lives in [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). Phase 1 aligned scope (user story, S1–S7, stack, API contract, seed users) before any code was written.

**Prompt vs as-built:** The prompt targeted a single API host and AutoMapper. The final solution split Auth and Tasks APIs and used a manual mapper. See [docs/genai-prompt-vs-result.md](docs/genai-prompt-vs-result.md) for the full delta and reasons.

### Sample output

AI produced: Clean Architecture solution layout, Domain/Application/Infrastructure/Api layers, Identity + JWT auth, FluentValidation, EF Core + SQLite migrations, xUnit tests, and a React 19 + Vite client with Kanban drag-and-drop, filters, and shadcn/ui components.

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
        CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateTaskAsync(UserId, request, cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return CreatedAtAction(nameof(GetTask), new { id = task!.Id }, task);
    }
}
```

Business rules, validation, and ownership checks live in `TaskService` — not in the controller.

### Validation performed

- `dotnet build` and `dotnet test` (**105 pass** — 60 unit + 45 integration, dual-host WAF)
- Manual smoke: login, CRUD, drag between columns, reactivate from Done/Canceled, filters, 403 on other user's task
- Frontend `npm run build`

### Corrections applied (examples)

AI output needed fixes after testing. Two representative examples:

**1. JWT 401 — Identity cookie overrode Bearer**

```csharp
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(/* ... */);
```

**2. Enum JSON — API returned `0`–`5`, frontend expected `"Todo"`, etc.**

```csharp
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

Other fixes: SQLite `DateTimeOffset` sort in memory; `TaskStatus` renamed to `KanbanStatus`; single API split into Auth + Tasks hosts.

### Edge cases captured

- Terminal columns (Completed/Cancelled): drag in only; reopen via `POST /api/tasks/{id}/reactivate`.
- Per-user isolation: 403 when accessing another user's task ID.
- Filters hide non-matching cards but drag-and-drop still allowed while filtered.
- JWT in localStorage documented as demo tradeoff (XSS vs simplicity).

Session log: [AI-NOTES.md](AI-NOTES.md). Interview talking points: [docs/interview-walkthrough.md](docs/interview-walkthrough.md). For project continuity when resuming development, see [AGENT-HANDOFF.md](AGENT-HANDOFF.md).
