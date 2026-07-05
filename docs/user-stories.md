# User Stories — Technical Interview (v1)

**App name:** Simple Tasks  
**UI:** React 19 + Vite + TypeScript + **shadcn/ui** + TanStack Query

> **See also:** [README](../README.md) (setup & demo) · [GenAI prompt](genai-scaffold-prompt.md) · [Demo script](interview-walkthrough.md) · [Session log](../AI-NOTES.md)

## Product narrative

As a professional managing my own work, I want a secure personal Kanban board where I can create tasks with priorities and due dates, drag them across my workflow, and clearly mark work as done or canceled, so that I always know what to focus on next and can deliberately reopen work when plans change, without sharing or mixing tasks with other users.

---

## Scoped stories

### S1 — Register, log in, and log out

**As a** new user,  
**I want to** register and log in with my email and password,  
**So that** I can access my task board securely.

**As a** logged-in user,  
**I want to** log out,  
**So that** my session ends and others cannot use my account on this device.

**Acceptance criteria — register & login**

- I can register with email and password (public route).
- Password must meet security policy (min 8 chars, upper, lower, digit, special character).
- I can log in and receive a JWT in the response (public route); token stored in `localStorage` for demo (see [README](../README.md) for production tradeoff).
- Invalid credentials are rejected with a clear error.
- After successful register, I am **auto-logged in** and redirected to the board.
- Duplicate email on register returns **400** or **409**.
- Protected API routes return **401** without a valid token.
- Loading indicator shown during login and register requests.

**Acceptance criteria — logout**

- I see a **Logout** control in the sidebar (below my email).
- Clicking Logout is **instant** — no confirmation modal.
- Logout removes JWT from `localStorage` and clears cached task/auth state.
- I am redirected to the login page.
- Navigating back to the board without logging in redirects to login (**401** / auth guard).
- Protected API calls after logout return **401**.

---

### S2 — Private task ownership

**As a** logged-in user,  
**I want** my tasks to be visible and editable only by me,  
**So that** my work stays private.

**Acceptance criteria**

- I only see my own tasks on the board.
- If I try to access another user's task by ID, the API returns **403 Forbidden**.
- If a task ID does not exist, the API returns **404 Not Found**.
- All task endpoints require authentication.

---

### S3 — Create and edit tasks

**As a** logged-in user,  
**I want to** create and edit tasks with title, description, priority, and due date,  
**So that** I can capture and keep my work up to date.

**Acceptance criteria**

- Title is required; validation errors return **400**.
- Priority is one of: Low, Medium, High, Urgent.
- Due date is **optional** (date only — compared by UTC calendar day in the API).
- **Create:** past due dates are rejected (**400**).
- **Update:** changing the due date to a different past date is rejected (**400**); clearing the due date is allowed.
- **Update:** if the due date is unchanged (same calendar day), updates are allowed even when the task is overdue — so Kanban drag/edit and completed late work are not blocked by historical due dates.
- **Update:** changing the due date to today or a future date is allowed.
- Dates are stored as ISO 8601 (`DateTimeOffset`, UTC); the UI date picker works with calendar dates only.
- New tasks default to **To Do** status and **Medium** priority if not specified.
- I can update and delete my own tasks.
- Delete asks for confirmation (dialog) before removing.
- `CreatedAt` and `UpdatedAt` are set automatically by the server.

---

### S4 — Kanban board view

**As a** logged-in user,  
**I want to** see my tasks organized in Kanban columns by status,  
**So that** I can understand my workflow at a glance.

**Acceptance criteria**

- Sidebar shows app name **Simple Tasks** and my email.
- Board shows six columns: To Do, In Progress, On Hold, In Review, Done, Canceled.
- Each card shows at least title, priority, and due date.
- Layout works on smaller desktop screens (horizontal scroll when columns don't fit).
- A tip explains **Shift + scroll** for horizontal navigation.

---

### S5 — Drag-and-drop status updates

**As a** logged-in user,  
**I want to** drag tasks between columns,  
**So that** updating status feels natural and fast.

**Acceptance criteria**

- I can drag freely among: To Do, In Progress, On Hold, In Review.
- I can drag into Done and Canceled from any active column.
- Dragging updates task status via the API.
- I cannot drag tasks **out of** Done or Canceled (blocked in UI; **400** if bypassed via API).
- I can still edit title, description, priority, and due date on Done/Canceled cards without reactivating.

---

### S6 — Reactivate finished or canceled work

**As a** logged-in user,  
**I want to** move a completed or canceled task back to To Do using an explicit action,  
**So that** I only reopen work intentionally.

**Acceptance criteria**

- Done and Canceled cards show a **Move to To Do** action.
- `POST /api/tasks/{id}/reactivate` moves **Completed** or **Cancelled** → **Todo**.
- Reactivate on an active task returns **400**.
- Reactivate on another user's task returns **403**.

---

### S7 — Search and filter tasks

**As a** logged-in user,  
**I want to** search tasks by title and filter by status and create/update dates,  
**So that** I can quickly find work on a busy board.

**Acceptance criteria**

- Search matches title partially, case-insensitive.
- Status filter supports multi-select; default is all statuses selected; **none selected = show all** (same as no filter).
- Empty search applies no title filter (show all matching other criteria).
- Created and updated filters support presets (Today, Last 7 days, Last 30 days) and custom from/to ranges.
- Active filters combine with AND logic.
- Non-matching cards are hidden; column counts reflect filtered results.
- Drag-and-drop works on visible cards; board refreshes after drop.
- Clear filters restores the full board.
- Invalid date range returns **400**.
- Loading indicator shown while tasks are fetched and when filters are applied.

---

## Seed data scenarios (demo)

| User | Credentials | Scenario |
|------|-------------|----------|
| Primary | `demo@example.local` / `Demo123!` | Full board — tasks in all columns, mixed priorities/dates; use for happy-path demo, drag, search/filter |
| Secondary | `other@example.local` / `Other123!` | Smaller distinct board; use to demo **403** when accessing another user's task by ID |

---

## Out of scope (v1)

- Multi-project workspaces and recent projects list
- Team collaboration: assignees, avatars, share/private toggle
- Comments, attachments, and progress bars on cards
- Calendar view (sidebar nav stub OK; no implementation)
- External identity providers (Clerk, Auth0)
- Strict status transition matrix
- Search in task description (title only)

---

**See also:** [README](../README.md) · [GenAI prompt](genai-scaffold-prompt.md) · [Prompt vs result](genai-prompt-vs-result.md) · [AI-NOTES](../AI-NOTES.md)
