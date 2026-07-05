# Interview demo walkthrough — Simple Tasks

A 5–10 minute script for presenting the technical interview project.

---

## Before you start

Run **three** processes (Auth API first — it creates demo users):

```powershell
# Terminal 1 — Auth API (users + JWT)
cd src/BlaInterview.Auth.Api
dotnet run
# → http://localhost:5098

# Terminal 2 — Tasks API (task CRUD)
cd src/BlaInterview.Tasks.Api
dotnet run
# → http://localhost:5099

# Terminal 3 — React client
cd client
npm run dev
# → http://localhost:5173
```

Optional for API discussion:

| Swagger | URL |
|---------|-----|
| Auth | http://localhost:5098/swagger |
| Tasks | http://localhost:5099/swagger |

Demo login is pre-filled: **demo@example.local** / **Demo123!**

---

## 1. Product overview (1 min)

- Personal Kanban board — one user's tasks, never shared
- Columns: To Do → In Progress → On Hold → In Review → Done / Canceled
- Priorities, due dates, search/filters, drag-and-drop
- User story in [user-stories.md](user-stories.md) (S1–S7)

---

## 2. Auth flow (1 min)

- Log in as demo user → board loads **8 seeded tasks**
- **Auth API** (`:5098`): `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/logout` (protected), `GET /api/health` (public)
- Mention: JWT in localStorage (demo tradeoff vs HttpOnly cookies)
- Optional: register a new user → empty board (per-user data isolation)
- Logout is instant (no confirmation)

---

## 3. Board & Kanban (2 min)

- Point out **priority badges** and **due-date urgency** (overdue / due soon)
- **Drag a card** between active columns → status updates via `PUT /api/tasks/{id}` on **Tasks API** (`:5099`)
- **Done / Canceled** are terminal — drag in only; cards are not draggable out
- **Reactivate** from card menu → `POST /api/tasks/{id}/reactivate` → back to To Do
- Shift + scroll to move horizontally across columns

---

## 4. CRUD & filters (2 min)

- **+** button → create task (validation: title required, due date not in the past)
- Edit / delete from card menu (delete has confirmation dialog)
- Sidebar filters: title search, status multi-select, created/updated date presets
- Filtered board **hides** non-matching cards; drag still works

---

## 5. Architecture (2–3 min)

PDF asks for **data API + second auth API**, **data layer**, **business layer**, and **Clean Architecture**:

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

Highlights to mention:

| Topic | Implementation |
|-------|----------------|
| Two segregated APIs | `BlaInterview.Auth.Api` + `BlaInterview.Tasks.Api` |
| Shared JWT contract | `JwtAuthenticationExtensions` — same Secret/Issuer/Audience; Auth signs, Tasks validates locally |
| Clean Architecture | Domain / Application / Infrastructure / Api (+ `Api.Shared` for HTTP plumbing) |
| Auth | ASP.NET Identity + JWT Bearer |
| Validation | FluentValidation in Application layer |
| API errors | Global `IExceptionHandler` → `{ error: "..." }` |
| Layer boundary | `TaskFilterRequest` (HTTP) → `TaskQuery` (persistence) |
| Pagination | `GET /api/tasks` returns `{ items, totalCount, page, pageSize }` |
| Tests | **79** — 42 unit (Moq.AutoMock) + 37 integration (dual WAF, cross-API JWT, 403 ownership) |
| CI | GitHub Actions — `dotnet test` + `npm run build` |

**PDF “ASP.NET MVC”:** Web API + React SPA (PDF also requires a JS frontend — intentional tradeoff).

---

## 6. Security talking points (if asked)

- **403** when accessing another user's task; **404** when task doesn't exist
- Terminal status rules enforced server-side (not only in UI)
- Demo JWT in localStorage — document XSS tradeoff for production
- Tasks API does not issue tokens — only validates tokens from Auth API

---

## 7. GenAI talking points (if asked)

| PDF asks | Where in repo |
|----------|----------------|
| Prompt used to scaffold | [genai-scaffold-prompt.md](genai-scaffold-prompt.md) |
| Prompt vs as-built delta | [genai-prompt-vs-result.md](genai-prompt-vs-result.md) |
| Sample output code | [README.md](../README.md) — GenAI workflow section |
| How AI output was validated | `AI-NOTES.md` — build/test, gap review, manual fixes |
| Corrections / edge cases | JWT scheme fix, `KanbanStatus` rename, enum JSON, per-user seeding, two-API split |

Example narrative: *“I aligned scope in Phase 1 before coding, used GenAI to scaffold Clean Architecture, then verified with `dotnet test` and fixed issues the AI missed (Identity vs Bearer, SQLite sorting, seed visibility). The prompt targeted a single API; I split auth and tasks to match the exercise and fixed three AI gaps found in testing.”*

---

## 8. Possible extensions (if time)

- `GET /api/auth/me` on Auth API
- RS256 + JWKS (Auth as OIDC authority)
- HttpOnly cookie auth for production
- PostgreSQL + server-side sort/pagination at scale
- E2E tests with Playwright

---

## Quick verification

```powershell
dotnet test
cd client; npm run build
```

Demo user should always see **8 cards**. If not:

1. Sign in as **demo@example.local** / **Demo123!** (not a newly registered user).
2. Start **Auth API first**, then Tasks API, then client.
3. Delete `src/BlaInterview.Auth.Api/tasks.db` and restart both APIs.
