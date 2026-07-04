# Simple Tasks — BLA Technical Interview

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
cd src/BlaInterview.Api
cp appsettings.Development.json.example appsettings.Development.json   # PowerShell: Copy-Item ...
```

`appsettings.Development.json` is gitignored. The committed `appsettings.json` uses a non-functional placeholder JWT secret — **the API will not issue valid tokens until you add `appsettings.Development.json` or set `Jwt__Secret` in the environment** (minimum 32 characters).

Integration tests inject their own in-memory JWT settings and do not need this file.

## Run the API

```bash
cd src/BlaInterview.Api
dotnet run
```

- API: http://localhost:5098
- Swagger: http://localhost:5098/swagger
- SQLite DB: `src/BlaInterview.Api/bla.db` (auto-migrated and seeded in Development)

## Run the frontend

```bash
cd client
npm install
npm run dev
```

- App: http://localhost:5173 (proxies `/api` to the backend)

## Demo credentials

| User | Email | Password | Purpose |
|------|-------|----------|---------|
| Primary | `demo@bla.local` | `Demo123!` | **8 seeded tasks** across all columns |
| Secondary | `other@bla.local` | `Other123!` | 4 tasks + 403 ownership demo |

### Seeded tasks (demo user only)

On first API start in **Development**, the database is migrated and seeded. The **demo** account gets **8 tasks** (2 To Do, 2 In Progress, 1 On Hold, 1 In Review, 1 Completed, 1 Cancelled). Newly registered users start with an **empty board**.

**If you don't see cards:**

1. Sign in as **`demo@bla.local`** / **`Demo123!`** (login form is pre-filled). Registering a new account will not show seed data.
2. Restart the API after pulling changes (seeding runs at startup).
3. If you previously logged in as demo with an empty DB, delete `src/BlaInterview.Api/bla.db` and restart — seed runs per user when that user has zero tasks.

You can confirm seed data in Swagger: `POST /api/auth/login` → Authorize with the token → `GET /api/tasks` should return 8 items with `"status": "Todo"` (string enums, not numbers).

### 403 demo

1. Log in as `demo@bla.local`
2. In Swagger, `GET /api/tasks` and copy a task ID from the other user (or use seeded `[Other]` titles via second login)
3. `GET /api/tasks/{id}` with demo token → **403 Forbidden**

## Tests

```bash
dotnet test
```

## JWT demo tradeoff

For interview/demo purposes, JWT is stored in `localStorage`. In production, prefer HttpOnly cookies or short-lived tokens with refresh.

## Project structure

```
src/           # Domain, Application, Infrastructure, Api
tests/         # BlaInterview.Unit.Tests + BlaInterview.Integration.Tests
client/        # React frontend
docs/          # genai-scaffold-prompt.md, user-stories.md
```

## GenAI workflow

This project was scaffolded and implemented with AI assistance (Cursor). Below is the workflow expected by the BLA exercise.

### Prompt used

The full agreed specification lives in [docs/genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). Phase 1 aligned scope (user story, S1–S7, stack, API contract, seed users) before any code was written.

### Sample output

AI produced: Clean Architecture solution layout, Domain/Application/Infrastructure/Api layers, Identity + JWT auth, FluentValidation, EF Core + SQLite migrations, xUnit tests, and a React 19 + Vite client with Kanban drag-and-drop, filters, and shadcn/ui components.

### Validation performed

- `dotnet build` and `dotnet test` (unit + WebApplicationFactory integration)
- Manual smoke: login, CRUD, drag between columns, reactivate from Done/Canceled, filters, 403 on other user's task
- Frontend `npm run build`

### Corrections applied (examples)

1. **JWT 401** — Identity cookie scheme overrode Bearer; set default authenticate/challenge to JWT.
2. **Enum JSON** — API returned numeric enums; frontend expected strings → added `JsonStringEnumConverter` + client-side normalization.
3. **SQLite sorting** — `ORDER BY` on `DateTimeOffset` unsupported → sort in memory in repository.

### Edge cases captured

- Terminal columns (Completed/Cancelled): drag in only; reopen via `POST /api/tasks/{id}/reactivate`.
- Per-user isolation: 403 when accessing another user's task ID.
- Filters hide non-matching cards but drag-and-drop still allowed while filtered.
- JWT in localStorage documented as demo tradeoff (XSS vs simplicity).

Session log: [AI-NOTES.md](AI-NOTES.md). For project continuity when resuming development, see [AGENT-HANDOFF.md](AGENT-HANDOFF.md).
