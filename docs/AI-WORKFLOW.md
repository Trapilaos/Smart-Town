# AI-Assisted Improvement Workflow

This file records how Smart-Town was modernised with Claude Code. Each phase went to a model picked for that job, and every phase is logged here so the repo itself shows the process.

| Phase | Model | Role | Status |
|---|---|---|---|
| 1. Repository map | Claude Haiku (subagent) | Fast, read-only survey of the codebase | Done |
| 2. Audit + improvement plan | Claude Opus | Check the map, rank risks, write the plan | Done (human approved .NET 10 + latest Angular) |
| 3. Implementation | Claude Sonnet (parallel subagents, one worktree each) | Upgrades, tests, CI/Docker | Done |
| 3b. UI overhaul | Opus plans → Haiku builds → Sonnet tests | Fix the UI after a hands-on browser review | Done (3 rounds) |
| 3c. Municipality platform | Opus plans → Haiku builds → Sonnet tests | Maps, bins, computed lighting, public parking, events and invoices, security | Done (2 fix rounds) |
| 4. Review | Claude Opus | Review the merged result end to end | Done: 10 findings, all fixed and retested |
| 5. Documentation | Claude Sonnet (Opus review) | New README, screenshots, architecture diagrams | Done |

Ground rules: make small, focused commits on the `ai-workflow` branch; a human approves the plan before Phase 3; nothing is pushed without explicit approval.

---

## Phase 1: Repository map (Haiku)

**Stack as found**
- **API**: ASP.NET Core Web API, `net8.0` (SDK pinned to 8.0.204 in `global.json`), EF Core 8 + SQLite, ASP.NET Identity + JWT (HS512, 7-day tokens), AutoMapper 12, Newtonsoft.Json. `Identity.EntityFrameworkCore` is **7.0.20**, mixed with EF 8.
- **Client**: Angular **16.2** (NgModule-based), Bootstrap 5 + Bootswatch, ngx-bootstrap 11, ngx-toastr 18, Karma/Jasmine.
- **Domain**: 11 controllers covering accounts, users and admin, events, comments, parking reservations, traffic, waste bins (route optimisation in the style of PSO), smart lighting (WeatherAPI), and invoice payment.
- **Tests / CI / Docker**: none. The one Angular spec is the stale CLI default and fails.
- The root `package.json` pins `@angular/animations@18`. It belongs to nothing.

## Phase 2: Audit (Opus)

Findings from the Phase 1 map. The high-severity items were checked by hand against the source.

### High: security
1. **Committed secrets and artefacts**: JWT signing key (`API/appsettings.Development.json`), WeatherAPI key (`client/src/environments/environment.development.ts`), TLS private key (`client/ssl/localhost-key.pem`), and the SQLite database (`API/Smarttown.db*`). All are in git history.
2. **Missing authorisation on state-changing endpoints**: `PUT /api/waste/{id}`, `POST /api/traffic/update`, `POST /api/payment`, `POST /api/parking/reserve`.
3. **Data exposure**: any signed-in user can read any user's email and phone number (`GET /api/users/id/{id}`). Anonymous `GET /api/comments` returns user IDs.
4. **Mass assignment**: Members can post a comment with `Seen=true` and skip moderation. The same entity-as-input pattern appears in events and parking.
5. `ExceptionMiddleware` returns exception messages in every environment. `BuggyController` ships to production.
6. Login has no lockout or rate limit, and its error messages allow user enumeration ("Invalid Username!" vs "Invalid Password"). Issuer and audience validation are off.

### High: correctness
7. **Parking double-decrement**: an expiring reservation decrements `CurrentVehicles` twice (`ParkingService.cs:116` and `:138`). The decrement also runs fire-and-forget on a scoped `DbContext` after the request ends.
8. With no base `appsettings.json` (it is git-ignored), `TokenKey` is null outside Development and startup crashes.
9. The JWT interceptor turns HTTP errors into successful emissions (`catchError(() => of(error))`), so the error interceptor never sees them.
10. `TrafficDataBackgroundService` uses `async void`, so an unhandled exception kills the process. It also wipes the traffic data on every start.

### Medium
- AutoMapper throws a null reference when a user has no main photo. Seed JSON `DateOfBirth` doesn't match the `DateofBirth` property, so birth dates are silently dropped.
- `AccountController` register returns `result.Errors` instead of `roleResult.Errors`.
- Payment returns `isPaid: true` / amount 0 on unexpected errors.
- Duplicate DI and authorisation registration. Mappings are split across two profiles. Migrations overlap.
- Client: hard-coded hosts, a placeholder `wss://example.com` socket opened on every visit, a leaked `visibilitychange` listener, services that import `environment.development` directly, and dead links (`/profile`, `/home/learn-more`).

### Low: hygiene
Dead code (`IUnitOfWork`, unused DTOs, `traffic-data.json`, stale `API.http`), Console logging, no Swagger, font-awesome 4.7 and 5 loaded together, hot-linked images.

### Environment constraints (this machine)
- Only the **.NET 8 SDK** is installed. Upgrading to .NET 10 (current LTS) needs the .NET 10 SDK.
- **Docker is not installed**. The Dockerfiles and compose file can be written and built in CI (GitHub Actions), but not run locally until Docker Desktop is installed.
- Node 24 / npm 11 are fine for current Angular.

---

## Phase 2: Improvement plan (proposed, waiting for approval)

Phase 3 runs as **three parallel Sonnet subagents, each in its own git worktree**, merged in the order below. Each workstream makes small commits.

### Workstream A: Backend (.NET)
1. Security fixes first, on .NET 8, so the history shows them apart from the upgrade:
   - move secrets to user-secrets / environment variables and add an `appsettings.json` with safe placeholders
   - untrack the `.db` files and the TLS key, and update `.gitignore`
   - add `[Authorize]` to the four open endpoints, use input DTOs for comments, events and parking, and restrict user lookup to self or admin
   - hide exception details and the `BuggyController` outside Development, and turn on lockout with a generic login error
2. Fix bugs 7, 8 and 10, the AutoMapper null reference, the register error, and the payment fallback.
3. Upgrade to **.NET 10 LTS**: `global.json` with `rollForward: latestFeature`, all `Microsoft.*` packages on matching 10.x versions, and Newtonsoft replaced by System.Text.Json. Add Swagger/OpenAPI for Development.

### Workstream B: Frontend (Angular)
1. Fix the interceptor bug, the environment imports, the listener leak, the placeholder socket and the dead links.
2. Upgrade **Angular 16 → current** one major version at a time with `ng update` (16→17→18→19→20…), with a commit per major. Move to the `application` builder. Standalone components are optional and out of scope unless approved.
3. Delete the stray root `package.json` / `package-lock.json`.

### Workstream C: Tests + CI + Docker
1. `API.Tests` (xUnit + `WebApplicationFactory` + in-memory SQLite): auth/login, authorisation on the fixed endpoints, parking reserve/expire (a regression test for bug 7), payment, and the waste route.
2. Angular unit tests for `AccountService`, the guards and the interceptors, replacing the stale spec. Use headless Chrome for CI.
3. `Dockerfile` for the API (multi-stage, `mcr.microsoft.com/dotnet/aspnet:10.0`), `Dockerfile` for the client (build, then nginx), and `docker-compose.yml`.
4. GitHub Actions: build and test the API, build and test the client, and build the Docker images on every PR.

C depends on A and B for final versions, so C writes its tests against the current API contract and rebases last.

### Out of scope unless you ask
- Rewriting git history to purge the leaked secrets. Rotating the keys is enough, since they are dev keys.
- A real payment provider, a real PSO implementation, or SignalR.
- Switching from SQLite to Postgres.

### Decisions for the human
- Target versions: **.NET 10 LTS + latest Angular** (recommended), or a smaller step (.NET 9 / Angular 18)?
- Should the agent install the .NET 10 SDK via winget, or will you?
- Approve Workstreams A–C as written?

---

## Phase 3: Implementation (Sonnet, three parallel worktrees)

The approved plan ran as three Sonnet subagents. Each worked in its own git worktree and owned a strict list of files, so the branches merged without conflicts.

| Workstream | Result |
|---|---|
| A: Backend | Security fixes first, on .NET 8: secrets moved to user-secrets, the DB and TLS key untracked, `[Authorize]` added to the open endpoints, input DTOs, lockout with a generic login error, exception details only in Development. Then the bug fixes: parking occupancy derived from reservations, a proper `BackgroundService`, seed fixes. Then the upgrade to **.NET 10**, System.Text.Json and the built-in OpenAPI. |
| B: Frontend | Interceptor, environment and listener-leak fixes, then **Angular 16 → 22**, one major per commit, with a passing build after each. Moved to the `application` builder and added a dev proxy. |
| C: Tests + CI | `API.Tests` (xUnit + `WebApplicationFactory`, in-memory SQLite), Angular specs for the services, guards and interceptors, Dockerfiles + compose, and a GitHub Actions CI. The tests were written against the *planned* behaviour, so they failed before A merged and passed after. |

Merge notes: the tests had to be updated for A's constructor and mapping changes. A new test exposed a design flaw: capacity counted *future* reservations as occupying the space now. That fix is left to the human as a `TODO(human)` in `ParkingService.ReserveParkingSpaceAsync`.

## Phase 3b: UI overhaul (Opus plans, Haiku builds, Sonnet tests)

The human asked for one model per role: Opus plans, Haiku builds, Sonnet tests. The full plan and findings are in [`UI-PLAN.md`](UI-PLAN.md).

| Round | Plan (Opus) | Build (Haiku) | Test (Sonnet) |
|---|---|---|---|
| 0 | Traced "[object Object]" on registration to five different API error-body shapes | A shared `getErrorMessages()` parser, inline register errors, a `SKIP_ERROR_TOAST` context token | Covered in round 1 |
| 1 | A browser survey of every page as admin and as member, at 1280px and 375px: 13 findings (F1–F13) | Three parallel worktrees: shell and error handling, citizen pages, admin pages | The flows, registration and F1–F12 passed; found 8 defects (D1–D8) |
| 2 | Root causes for D1–D8, e.g. a dead mobile menu because Bootstrap JS was never loaded, and guards returning `false` blanking the page on a direct load | 8 commits, one per defect, plus an API DTO for comment usernames | D1–D5, D7 and D8b/c passed; contrast and a toast leak remained |
| 3 | Flatly's `dark` is grey, and skip-toast was only honoured for 400 and 401 | One commit plus an interceptor spec | All checks passed, no defects |

Lesson worth keeping: a QA pass at 375px caught a mobile menu that had never worked, even in the original app. Desktop-only checks would have missed it.

## Phase 3c: Municipality platform (Opus plans, Haiku builds, Sonnet tests)

The human asked for a municipality admin platform: a map with the municipality's borders, bins placed on it, lighting from an algorithm instead of an API, public access to parking, events and invoices, and security throughout. The plan is in [`MUNICIPALITY-PLAN.md`](MUNICIPALITY-PLAN.md).

- **The human's decision:** fetch the boundary **automatically** from OpenStreetMap (Nominatim) instead of uploading it. It uses a generic User-Agent, 1 request per second, a simplified polygon, and it's stored once. An approximate outline is the fallback.
- **The parking `TODO(human)`:** written by Opus. Capacity is the peak number of cars parked at once within the requested window.
- **Build order:**
  1. backend B1 (municipality, geo, bins, route, lighting), then B2 (parking, events, invoices, security, audit), in sequence because they share the migrations
  2. frontend F1 (the map foundation), running in parallel with B2
  3. F2 (lighting, events) and F3 (parking, invoices, dashboard), in parallel worktrees
- **Testing:** Sonnet's first pass passed every flow and every security check, and found 6 defects plus one gap. Fix round 1 resolved D2–D7: OSM place results are now resolved to their municipality through a reverse lookup, and audit amounts are culture-invariant, among others. Fix round 2 fixed map fitting (an animated `fitBounds` was being counted as a user move). The final map check had 0 of 74 markers out of view across 12 page and size combinations.

## Phase 4: Review (Opus)

Opus reviewed the whole branch against `main` (102 commits, 294 files) and found 10 issues. Two were release blockers that local testing couldn't reveal:

| # | Finding | Fix |
|---|---|---|
| R1 | The admin was seeded with the public demo password in every environment | Demo data is Development-only. Production creates the admin only from `Seed__AdminPassword`, and without it logs a warning and creates nothing |
| R2 | Behind nginx, every request came from the proxy's IP, so the whole town shared one rate-limit bucket | `UseForwardedHeaders`, trusting only the compose network (`172.28.0.0/24`) and loopback |
| R3 | Anyone could lock out the admin | A per-(username, IP) login throttle replaces account lockout; the "admin" alias is removed |
| R4 / R6 / R8 | Check-then-write races: parking overbooking, duplicate invoice numbers, duplicate event interest | A `BEGIN IMMEDIATE` transaction plus a semaphore; retry on a unique-key clash; idempotent on a duplicate key. Each has a 10-way concurrency test |
| R5 | The anonymous parking list wrote to the DB on every request | Read-only, with occupancy computed at read time and expired reservations purged by a hosted service |
| R7 | The client decoded the JWT with `atob` and never checked expiry | A base64url-safe decoder; expired sessions log out cleanly |
| R9 / R10 | Startup continued after a failed migration; the invoice summary loaded every row into memory | Fail fast; aggregate in SQL |

The fixes were planned by Opus, built by two Haiku agents in parallel (backend and client), and tested by Sonnet.

## Phase 5: Documentation (Sonnet, reviewed by Opus)

Sonnet wrote the new README: features, 13 screenshots taken with headless Chrome (puppeteer-core) from the running app, two Mermaid diagrams (system and domain), key design decisions, a configuration table taken from the code, the security summary, test counts measured from real runs (241 API, 113 client) and this workflow.

Opus's review found that the documented `npm start` could not work: `ng serve` forced TLS with a private key that the security cleanup had removed. Instead of documenting a workaround, Opus changed the dev setup to match production. The development environment now uses the same relative `api/` URL, and `ng serve` proxies `/api` to the API over plain HTTP, the way nginx does in Docker. A fresh clone now runs with `dotnet run` plus `npm start`, with no certificates or CORS setup.
