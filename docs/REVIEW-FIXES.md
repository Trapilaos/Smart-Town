# Phase 4 Review Fixes

**Planned by:** Claude Opus 5.5. **Built by:** Claude Haiku (backend and client, in parallel). **Tested by:** Claude Sonnet.

The Phase 4 review of `ai-workflow` against `main` found 10 issues. The human chose to fix all of them before merging.

| # | Sev | Finding | Fix |
|---|---|---|---|
| R1 | Blocker | The admin and demo users are seeded with the public password `Pa$$w0rd` in every environment, including Docker/Production | See R1 below |
| R2 | Blocker | Rate limits partition by `RemoteIpAddress`. Behind nginx that is the proxy's IP, so the whole town shares one bucket | See R2 |
| R3 | High | Anyone can lock the admin account out forever (5 wrong passwords every 10 min, plus the `admin` username alias) | See R3 |
| R4 | High | Parking capacity check-then-insert races, so concurrent bookings overfill a space | See R4 |
| R5 | Med | Anonymous `GET /api/parking` writes to the DB on every request (`SyncReservationsAsync`) | See R5 |
| R6 | Med | Concurrent invoice creates collide on `INV-yyyy-nnnnnn`, causing a 500 | See R6 |
| R7 | Med | Client: the JWT is decoded with `atob` (base64url breaks it), and token expiry is never checked | See R7 |
| R8 | Low | A concurrent double POST to event interest violates the PK, causing a 500 | See R8 |
| R9 | Low | Migration or seed failures are only logged; the app still serves | See R9 |
| R10 | Low | The invoice summary loads every invoice into memory | See R10 |

## Backend (agent B: API/, API.Tests/, docker-compose.yml, .env.example)

**R1: Seeding by environment.**
- New config:
  - `Seed:DemoData`, default `true` only when the environment is Development, else `false`.
  - `Seed:AdminUsername`, default `"admin"`.
  - `Seed:AdminPassword`, no default.
- **Demo data** means the demo residents from `UserSeedData.json` and the demo invoices. Seed it **only** when `Seed:DemoData` is true.
- **Municipality content** (municipality, bins, parking spaces, events, traffic) is always seeded when its table is empty.
- **The admin account:**
  - **Development:** keep the current behaviour (admin / `Pa$$w0rd`) so local dev and tests keep working.
  - **Other environments:** create the admin only if no Admin-role user exists. The password comes from `Seed:AdminPassword`. If it's missing, or fails Identity's password rules, **don't create the admin**: log a clear warning ("Set Seed__AdminPassword to create the first admin") and continue.
  - Never log the password.
- `docker-compose.yml`: add `Seed__AdminPassword: ${ADMIN_PASSWORD:?Set ADMIN_PASSWORD in .env}`.
- `.env.example`: add `ADMIN_PASSWORD=` with a comment about the password rules.
- **Tests** (a factory with environment Production):
  - no demo users are created and no `Pa$$w0rd` admin exists
  - with `Seed:AdminPassword` set, the admin exists and that password works
  - the existing Development-based tests keep passing unchanged

**R2: Real client IP behind the proxy.**
- Configure `ForwardedHeadersOptions`:
  - `ForwardedHeaders = XForwardedFor | XForwardedProto`
  - `ForwardLimit = 1`
  - `KnownNetworks` and `KnownProxies` read from config `ForwardedHeaders:KnownNetworks` (CIDR strings) and `ForwardedHeaders:KnownProxies` (IPs)
  - Clear the defaults only when config provides values; otherwise keep the loopback default.
- Call `app.UseForwardedHeaders()` **first**, before the security headers and **before `UseRateLimiter`**.
- **Never trust X-Forwarded-For from unknown sources.** That's the default behaviour; keep it.
- `docker-compose.yml`: give the network a fixed subnet (`smarttown`, `172.28.0.0/24`) and set `ForwardedHeaders__KnownNetworks__0: 172.28.0.0/24` on the api service.
- **Tests:**
  - With TestServer the remote IP is null. Add a tiny test-only startup filter in the test project, or use `TestServer`'s `HttpContext` customisation, to set `RemoteIpAddress` to a configured proxy IP.
  - Then two different `X-Forwarded-For` values get **separate** public-invoice buckets: 5 each, the 6th from the same client gets 429, and the other client still passes.
  - A forwarded header from an **untrusted** remote IP is ignored, so both values share one bucket.

**R3: No account-wide lockout DoS.**
- Remove the special case that maps username "Admin" to the first Admin-role user. The username must match exactly (lower-cased as today).
- Replace Identity account lockout for login with a **per (username, client IP)** failure throttle:
  - `LoginThrottle` service, a singleton on `IMemoryCache`
  - 5 failures within 10 min for the same username+IP blocks that pair for 10 min
  - Return the same generic 401 while blocked
  - A success resets the pair
- Call `CheckPasswordSignInAsync(..., lockoutOnFailure: false)`, and set `Lockout.AllowedForNewUsers = false`.
- The `auth` rate limit (per IP) stays as the global brake.
- **Tests:**
  - Replace `Repeated_failed_logins_lock_the_account` with:
    - after 5 failures from IP A, the correct password from IP A gets 401
    - the correct password from IP B gets 200 (the real admin isn't locked out)
    - after the window, IP A works again (inject a clock, or make the window configurable)
  - Use the R2 forwarded-IP test helper.

**R4: Atomic parking reservation.**
- Run `ReserveParkingSpaceAsync`'s read-check-insert in a single **write transaction that takes the lock up front**.
  - On SQLite: `BEGIN IMMEDIATE`, via `_context.Database.BeginTransactionAsync()`. Check what Microsoft.Data.Sqlite issues by default; if needed, use `context.Database.GetDbConnection()` with `BeginTransaction(deferred: false)` and `UseTransaction`.
  - Add a process-wide `SemaphoreSlim(1,1)` around the critical section as a second guard, since the app is single-instance. Note this in a comment.
- Keep the peak-occupancy rule and the user-overlap rule exactly as they are.
- **Test:** a space with MaxVehicles=1; 10 concurrent reserve calls (separate DbContexts or scopes over the same SQLite file or a shared in-memory connection) produce exactly 1 success and 9 failures.

**R5: Read-only public parking.**
- `GetParkingSpacesAsync` must not write.
- Compute `currentVehicles` and `freeNow` at read time from reservations active now. Stop persisting `CurrentVehicles`: you may keep the column but ignore it, and document that.
- The reserve path doesn't need the sync either; the peak check already ignores expired rows.
- Purge expired reservations in a hosted `BackgroundService` every 15 min, with try/catch and logging.
- **Test:** `GET /api/parking` issues no `SaveChanges`. Assert that `ChangeTracker`/DB row counts are unchanged, or use a counting `SaveChangesInterceptor` in the test factory.

**R6: Invoice numbers and R10: summary.**
- `CreateAsync`: retry on `DbUpdateException` caused by the unique index on `InvoiceNumber`, up to 5 times. Detach the failed entity, then recompute the number.
- Compute the max number in SQL: `OrderByDescending(InvoiceNumber).Select(...).FirstOrDefaultAsync()` on the prefix. This works because the numbers are zero-padded fixed width.
- `GetSummaryAsync`: counts in SQL. SQLite can't SUM decimals in SQL, so sum `(double)Amount` in SQL and round to 2 dp as decimal, or store amounts as cents. Choose the cast, keep the entity unchanged, and add a test that the totals match exactly for typical amounts.
- **Test:** 10 concurrent creates produce 10 distinct numbers with no exception.

**R8: Idempotent interest.**
- In `SetInterestAsync`, catch `DbUpdateException` from a duplicate `(EventId, UserId)` insert, detach the entity, and continue: it's already interested.
- **Test:** two concurrent POSTs both return 200 and the count is 1.

**R9: Fail fast on startup.**
- In `Program.cs`, log and then **rethrow** the migration or seed exception, so the process exits non-zero and Docker restarts it, making the failure visible.
- Don't let the boundary auto-fetch (a hosted service) block or fail startup; it already catches its own errors.

## Client (agent C: client/ only)

**R7: Token handling.**
- New `client/src/app/_helpers/jwt.ts`:
  - `decodeJwtPayload(token)` uses a base64url-safe decode: replace `-` with `+` and `_` with `/`, pad with `=`, `atob`, then `TextDecoder` for UTF-8.
  - `isTokenExpired(token, skewSeconds = 30)`.
- `AccountService`:
  - Use `decodeJwtPayload`.
  - `restoreUser()` drops the stored user if the token is expired or can't be decoded.
  - `setCurrentUser` guards against a decode failure: clear and rethrow a clear error.
- `JwtInterceptor`: if the current user's token is expired, log out (clear the user) and send the request **without** the Authorization header.
- `ErrorInterceptor`: on 401 for a request that carried an Authorization header, log out and navigate to `/` with a toast "Your session has expired. Please log in again." Keep the existing 401 behaviour for login itself.
- **Specs:**
  - decode works for a payload containing `-`/`_` (build a token whose payload base64url has those chars, e.g. a username with `?>` characters, or construct the bytes directly)
  - expired detection
  - restore drops an expired user
  - the interceptor logs out on an expired token

## Rules
- Small commits, one per finding (e.g. `fix(api): seed demo users and default admin only in development (R1)`).
- **No Co-Authored-By or attribution trailers.** Never push.
- Backend: all API tests pass (224 before). Client: `ng build` and all specs pass (98 before).
- Build the API with `-p:OutDir=<temp>/` while the dev API is running. Don't start or stop the servers.

## Test plan (Sonnet)
- **API suite and client specs:** run both, and report the counts.
- **Live app (Development):**
  - Login works for `lisa` and `admin`, and the username "Admin" (capitalised) still logs in as `admin` via lower-casing.
  - The public parking page works.
  - A reservation works.
  - "I'm interested" works, including a fast double click.
  - Issuing two invoices quickly gives distinct numbers.
  - The admin dashboard summary shows sane totals.
- **Session expiry:** in the browser, replace the stored token with an expired one (craft one by editing `exp` in a copied payload; the signature doesn't matter to the client check). After a reload the user is logged out, with no 401 toast loop.
- **R2 via curl:**
  - The API trusts loopback by default, so curl from localhost with `X-Forwarded-For: 203.0.113.1` and then `203.0.113.2` on `/api/public/invoices/lookup`: each IP gets its own 5-request budget.
  - Wait for the 1-minute window between runs.
- **R3:** 5 bad passwords for `admin` with `X-Forwarded-For: 203.0.113.9`, then the correct password with `X-Forwarded-For: 203.0.113.10` succeeds.
