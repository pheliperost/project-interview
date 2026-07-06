# Presentation outline — Simple Tasks

Suggested flow for the BLA technical interview (~5–10 minutes). Matches the exercise: explain your **user story**, **design choices**, and **architecture**, then **demonstrate** the application.

> **Setup & API details:** [README](../README.md) · **Acceptance criteria:** [user-stories.md](user-stories.md) · **GenAI deliverables:** [genai-scaffold-prompt.md](genai-scaffold-prompt.md)

---

## Before the demo

Start **Auth API first** (creates users), then Tasks API (seeds tasks), then the client:

```powershell
cd src/BlaInterview.Auth.Api && dotnet run    # → http://localhost:5098
cd src/BlaInterview.Tasks.Api && dotnet run   # → http://localhost:5099
cd client && npm run dev                      # → http://localhost:5173
```

| Swagger | URL |
|---------|-----|
| Auth | http://localhost:5098/swagger |
| Tasks | http://localhost:5099/swagger |

Demo login (pre-filled on the sign-in page): **demo@example.local** / **Demo123!**

If the board is empty, see [Demo credentials](../README.md#demo-credentials) in the README.

---

## 1. User story & product (1–2 min)

- Read or paraphrase the informal story in [README](../README.md#user-story) and scoped stories S1–S7 in [user-stories.md](user-stories.md).
- Personal Kanban: six columns, priorities, due dates, one user per board (tasks are not shared).

---

## 2. Live demo (3–4 min)

Walk through the running app in this order:

1. **Sign in** as demo user → **8 seeded tasks** across columns.
2. **Kanban** — drag between active columns; drop into Done/Canceled; **Move to To Do** (reactivate) on terminal cards.
3. **CRUD** — create, edit, delete a task (validation on save).
4. **Filters** — search by title, filter by status and dates.
5. **Optional** — register a second user → empty board (per-user data).
6. **Optional** — Swagger: login on Auth API → Authorize with JWT → `GET /api/tasks` on Tasks API.

Ownership and auth behavior (403 / 401 / terminal rules) are defined in [user-stories.md](user-stories.md) (S2, S5, S6) and covered by integration tests — demonstrate only if the panel asks.

---

## 3. Architecture & design choices (2–3 min)

```
React (TanStack Query)
    ├─ /api/auth/*  → Auth API :5098  (Identity, JWT issuance)
    └─ /api/tasks/* → Tasks API :5099 (JWT validation, task CRUD)
                            ↓
              Application (TaskService, FluentValidation)
                            ↓
              Infrastructure (EF Core, SQLite, repositories)
                            ↓
              Domain (TaskItem, enums — no EF/HTTP)
```

Points to cover (exercise asks for **two APIs**, **data layer**, **business layer**, **Clean Architecture**):

| Topic | Where |
|-------|--------|
| Segregated Auth + Tasks APIs | `BlaInterview.Auth.Api`, `BlaInterview.Tasks.Api` |
| Business rules & validation | `TaskService`, FluentValidation (Application) |
| Data access | `TaskRepository`, EF Core (Infrastructure) |
| Tests | **154** — 91 unit + 63 integration (`dotnet test`) |
| Tradeoffs | JWT in `localStorage` for demo; Web API + React SPA (not server-side MVC views) |

More detail: [Key design decisions](../README.md#key-design-decisions) in the README.

---

## 4. GenAI workflow (1 min)

The exercise asks for your prompt, sample output, validation, and corrections. Point reviewers to:

| Deliverable | Document |
|-------------|----------|
| Prompt | [genai-scaffold-prompt.md](genai-scaffold-prompt.md) |
| Prompt vs as-built | [genai-prompt-vs-result.md](genai-prompt-vs-result.md) |
| Sample output & validation | [README — GenAI workflow](../README.md#genai-workflow) |
| Session log | [AI-NOTES.md](../AI-NOTES.md) |

---

## Verify before submitting

```powershell
dotnet test
cd client; npm run build
```
