# AI Notes — Technical Interview

> **Purpose (interview):** Short **preview** of how GenAI (Cursor) was used on this project — not a live session log. Full prompt, sample output, validation tables, and corrections → [README — GenAI workflow](README.md#genai-workflow) and [Validation and corrections](README.md#validation-and-corrections). Agreed pre-code prompt → [genai-scaffold-prompt.md](docs/genai-scaffold-prompt.md). **Current tests:** **159 pass** (95 unit + 64 integration).
>
> **See also:** [README](README.md) · [User stories](docs/user-stories.md)

---

## How I worked with AI

1. **Scope first** — Iterated on user story, S1–S7, API contract, and edge cases *before* coding; saved the final prompt in `genai-scaffold-prompt.md` after approval.
2. **Generate, then verify** — Used AI for scaffold and features; always ran `dotnet build`, `dotnet test`, and `npm run build` / `npm run verify` before accepting changes.
3. **Push back when needed** — Did not accept the first suggestion when it conflicted with the exercise spec, SQLite limits, or UX (examples below).
4. **Document deltas** — Kept prompt vs as-built corrections in the README so reviewers see what changed and why.

## What I accepted vs changed

| AI suggested | What I did |
|--------------|------------|
| Informal user story only | Expanded to S1–S7 with acceptance criteria, filters, terminal columns, reactivate endpoint |
| Single API host | Split **Auth API** (:5098) + **Tasks API** (:5099) per interview spec |
| HttpOnly cookie for JWT | **JWT in localStorage** for demo simplicity (tradeoff documented in README) |
| `TaskStatus` enum name | Renamed to **`KanbanStatus`** (clash with `System.Threading.Tasks.TaskStatus`) |
| Identity cookie as default auth | Set **JWT Bearer** as default scheme (fixed 401 on protected routes) |
| Integer enum JSON | Added **`JsonStringEnumConverter`** + client `normalizeTask()` (fixed empty board) |
| SQLite `DateTimeOffset` in SQL | **In-memory** sort/filter where SQLite cannot translate |

## Bugs found after AI output (examples)

- **Empty Kanban after login** — API returned status `0`–`5`; UI expected `"Todo"`, etc.
- **401 with token still in UI** — Client did not check expiry or clear session on 401.
- **Seed cards missing** — Per-user seeding and demo credentials documented in README.

## How I validated

- **Automated:** xUnit unit + integration tests (dual-host WebApplicationFactory, in-memory SQLite for repository tests); CI runs `dotnet test`, `npm run verify`, `npm run build`.
- **Manual:** Login, CRUD, drag-and-drop, reactivate, filters, 403 ownership — [demo flow](README.md#demo-flow).
- **Criteria:** [user-stories.md](docs/user-stories.md) S1–S7 acceptance criteria.

---

*This file is a static summary for the interview. Detailed implementation lives in the codebase and README.*
