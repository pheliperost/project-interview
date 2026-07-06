# Troubleshooting

Common setup issues when running Simple Tasks locally (Windows / PowerShell). Commands assume **repo root**.

> **See also:** [README — Run locally](../README.md#run-locally) · [Demo credentials](../README.md#demo-credentials)

---

## Build fails: `MSB3021` / file locked by `BlaInterview.Auth.Api` or `BlaInterview.Tasks.Api`

Both APIs share the same library projects (`Infrastructure`, `Application`, `Domain`). While either API is running, `dotnet build` cannot overwrite DLLs in `bin\Debug` — **Ctrl+C in the terminal does not always stop the process**.

1. Stop both APIs, then build once:

```powershell
Get-NetTCPConnection -LocalPort 5098,5099 -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }

dotnet build
```

2. Start again: **Auth API first**, then Tasks API, then the client.

Do not run `dotnet build` on the solution while the APIs are still listening on 5098 or 5099.

## Login works but tasks fail (401)

Both APIs must share the same `Jwt:Secret` (≥ 32 characters). The committed `appsettings.json` uses a placeholder — copy the example file on **both** hosts ([Configuration](../README.md#configuration-first-time-setup)), restart Auth and Tasks. Symptom: login returns a token, but `GET /api/tasks` returns **401**.

## Client cannot reach the API (connection refused / proxy error)

- Confirm Auth is on **http://localhost:5098** and Tasks on **http://localhost:5099** (`GET /api/health` on Auth).
- Run the React app with **`npm run dev`** (port **5173**) so Vite proxies `/api/auth` and `/api/tasks`.

## Board is empty after sign-in

See [Demo credentials — empty board](../README.md#demo-credentials) in the README.

## Pre-filled demo login fails (`Invalid email or password`)

The demo password may have been changed via forgot-password, or the account may be locked after failed attempts. In **Development**, restarting the **Auth API** restores `Demo123!` and clears lockout; otherwise use the password you set or request a new reset link.
