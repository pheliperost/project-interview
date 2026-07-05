# GenAI prompt vs as-built result

The saved prompt in [genai-scaffold-prompt.md](genai-scaffold-prompt.md) is the **Phase 1 specification** — what was agreed before coding. The table below records intentional deviations found during implementation and testing.

> **See also:** [README](../README.md) · [User stories](user-stories.md) · [AI-NOTES](../AI-NOTES.md) · [Agent handoff](../AGENT-HANDOFF.md)
| Prompt / spec | As built | Why it changed |
|---------------|----------|----------------|
| Single `BlaInterview.Api` host | `Auth.Api` (:5098) + `Tasks.Api` (:5099) + `Api.Shared` | Segregated auth and task APIs per interview exercise |
| AutoMapper | AutoMapper `TaskProfile` | Aligned with LessonsManagement; manual mapper replaced on user request |
| 4 test projects (Domain, Application, Infrastructure, Api) | 2 assemblies (`Unit.Tests`, `Integration.Tests`) | LessonsManagement reference pattern; Moq.AutoMock + Bogus |
| `TaskStatus` enum | `KanbanStatus` | Name clash with `System.Threading.Tasks.TaskStatus` |
| SQLite file `tasks.db` | `bla.db` under `Auth.Api` | Shared DB path chosen during implementation |
| Identity + JWT (both registered) | JWT as default authenticate/challenge scheme | AI scaffold left Identity cookie as default → 401 on protected routes |
| Enum serialization (unspecified) | `JsonStringEnumConverter` + client `normalizeTask()` | API returned `0`–`5`; frontend grouped by `"Todo"`, etc. → empty board |
| Repository sort via LINQ | In-memory sort for `DateTimeOffset` | SQLite does not support `ORDER BY` on `DateTimeOffset` |

## Functional scope

Kanban columns, priorities, due dates, filters (S7), terminal Done/Canceled rules, reactivate endpoint, per-user isolation (403), and JWT auth **match the agreed prompt**. Deviations are mostly structure, tooling, and bug fixes after validation.

## Validation that surfaced fixes

- `dotnet build` / `dotnet test` (dual-host WebApplicationFactory)
- Manual smoke: login, CRUD, drag-and-drop, reactivate, filters, 403 on other user's task
- `npm run build` on the React client

Session log with accept/reject/change notes: [AI-NOTES.md](../AI-NOTES.md).
