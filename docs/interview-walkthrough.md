# Interview demo walkthrough — Simple Tasks

A 5–10 minute script for presenting the BLA technical interview project.

---

## Before you start

1. Start the API: `cd src/BlaInterview.Api` → `dotnet run` → http://localhost:5098
2. Start the client: `cd client` → `npm run dev` → http://localhost:5173
3. Open Swagger: http://localhost:5098/swagger (optional, for API discussion)
4. Demo login is pre-filled: **demo@bla.local** / **Demo123!**

---

## 1. Product overview (1 min)

- Personal Kanban board — one user's tasks, never shared
- Columns: To Do → In Progress → On Hold → In Review → Done / Canceled
- Priorities, due dates, search/filters, drag-and-drop

---

## 2. Auth flow (1 min)

- Log in as demo user → board loads **8 seeded tasks**
- Mention: JWT in localStorage (demo tradeoff vs HttpOnly cookies)
- Optional: register a new user → empty board (per-user data isolation)
- Logout is instant (no confirmation)

---

## 3. Board & Kanban (2 min)

- Point out **priority badges** and **due-date urgency** (overdue / due soon)
- **Drag a card** between active columns → status updates via `PUT /api/tasks/{id}`
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

```
React (TanStack Query) → ASP.NET API → Application (TaskService) → Infrastructure (EF + SQLite)
```

Highlights to mention:

| Topic | Implementation |
|-------|----------------|
| Clean Architecture | Domain / Application / Infrastructure / Api |
| Auth | ASP.NET Identity + JWT Bearer |
| Validation | FluentValidation in Application layer |
| API errors | Global `IExceptionHandler` → `{ error: "..." }` |
| Layer boundary | `TaskFilterRequest` (HTTP) → `TaskQuery` (persistence) |
| Pagination | `GET /api/tasks` returns `{ items, totalCount, page, pageSize }` |
| Tests | 24+ tests — unit (Moq) + integration (WAF + in-memory SQLite) |
| CI | GitHub Actions — `dotnet test` + `npm run build` |

---

## 6. Security talking points (if asked)

- **403** when accessing another user's task; **404** when task doesn't exist
- Terminal status rules enforced server-side (not only in UI)
- Demo JWT in localStorage — document XSS tradeoff for production

---

## 7. Possible extensions (if time)

- Rich domain model on `TaskItem` (discuss tradeoffs)
- HttpOnly cookie auth for production
- PostgreSQL + server-side sort/pagination at scale
- E2E tests with Playwright

---

## Quick verification

```powershell
dotnet test
cd client; npm run build
```

Demo user should always see 8 cards. If not: delete `src/BlaInterview.Api/bla.db` and restart API.
