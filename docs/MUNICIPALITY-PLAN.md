# Municipality Platform Plan

**Planned by:** Claude Opus 5.5. **Built by:** Claude Haiku agents. **Tested by:** Claude Sonnet.

Goal: turn Smart-Town into a municipality admin platform with a public citizen side. The municipality staff (Admin role) manage the map, bins, parking, events and invoices. Citizens can browse events, parking availability and the lighting schedule, and pay a bill **without an account**. An account adds comments, event interest, parking reservations and "My invoices".

Decisions (Opus, defaults the human can override):
- **Map:** Leaflet 1.9 with OpenStreetMap tiles. Free, no API key.
- **Municipality boundary:**
  - Stored in the DB as GeoJSON (Polygon/MultiPolygon). The admin uploads or pastes it on a settings page; it can be exported from OpenStreetMap.
  - The seed ships an **approximate** Glyfada outline, labelled as approximate. No runtime calls to external geo services.
- **Lighting:** a deterministic algorithm with no external API. NOAA solar position gives sunrise and sunset for the municipality centre in its time zone, and a fixed dimming policy is applied. WeatherAPI is removed.
- **Public invoice payment:** invoice number + **payment code** (8 characters, printed on the bill), so invoices can't be enumerated. Payment stays a mock, but records `PaidAt` and a receipt reference.
- **Timezone:** `Europe/Athens` (config). Everything is stored in UTC.

---

## 1. Backend (API)

### 1.1 Municipality
- **Entity** `Municipality` (single row):
  - `Id`, `Name`
  - `CenterLatitude`, `CenterLongitude`, `DefaultZoom`
  - `TimeZoneId`
  - `BoundaryGeoJson` (text)
  - `BoundaryIsApproximate` (bool)
  - `StreetlightCount` (int, default 1200), `StreetlightWatts` (int, default 60)
  - `UpdatedAt`
- **Endpoints:**
  - `GET /api/municipality` (anonymous): name, centre, zoom, boundary GeoJSON, approximate flag.
  - `PUT /api/admin/municipality` (Admin): name, centre, zoom, streetlight count/watts, and the boundary GeoJSON.
- **Boundary validation:**
  - It must parse as a GeoJSON `Polygon`, `MultiPolygon`, `Feature` or `FeatureCollection` containing one of those.
  - Max 2 MB.
  - Every coordinate must be within lat [-90, 90] and lng [-180, 180].
  - Rings must be closed with at least 4 positions.
  - Store it normalised as a Polygon or MultiPolygon geometry.
- **`GeoService` (pure, unit-tested):**
  - `IsInside(lat, lng, geometry)`: ray casting, holes respected, MultiPolygon supported.
  - `HaversineMeters`.
- **Seed:** Glyfada, centre 37.8650, 23.7530, zoom 14. An approximate polygon of about 12–20 vertices around the municipality, with `BoundaryIsApproximate = true`.

### 1.2 Waste bins
- **`WasteBin` adds:**
  - `Latitude`, `Longitude`
  - `Type` (enum stored as a string: `General`, `Recycling`, `Glass`, `Organic`, `Paper`)
  - `IsActive`
  - Keep `Location` as an address label, and keep `Capacity`, `CurrentFillLevel` and `LastEmptied`.
- **Endpoints:**
  - `GET /api/waste/bins?type=` (anonymous): active bins with fill %.
  - `POST /api/admin/waste/bins`, `PUT /api/admin/waste/bins/{id}`, `DELETE /api/admin/waste/bins/{id}` (Admin; delete is a soft delete via `IsActive = false`).
  - Create and update reject points outside the boundary with 400 "Bin must be inside the municipality boundary".
  - `PUT /api/admin/waste/bins/{id}/empty` (Admin): fill = 0, `LastEmptied` = now.
- **Collection route** (`GET /api/admin/waste/route?type=&minFillPercent=60`, Admin):
  - Select the active bins at or above the threshold (optionally filtered by type).
  - Build a nearest-neighbour tour from the depot (the municipality centre) with haversine distance, improve it with **2-opt** until no gain, and return to the depot.
  - Response: `{ stops: [{binId, location, type, lat, lng, fillPercent, order}], totalDistanceKm, depot: {lat, lng} }`.
  - Replaces the hard-coded PSO graph. Delete `WasteManagementService`'s graph and PSO code, and `Particle.cs`.
- **Seed:** about 24 bins of mixed types at coordinates inside the seeded polygon, with varied fill levels. Seed only when the table is empty.
- **Keep or adapt** the existing `GET /api/waste` and `PUT /api/waste/{id}` for compatibility, or remove them and update the client.

### 1.3 Lighting (algorithm only)
- **`SolarCalculator` (pure, static, unit-tested):** NOAA sunrise and sunset for a date, latitude and longitude, returning UTC times. Also civil dawn and dusk (sun at -6°).
- **`LightingScheduleService`:** the policy for each local date:
  - Lights **on at 100%** at sunset − 10 min.
  - **Dim to 50%** from 00:30 to 05:00 local.
  - **Off** at sunrise + 10 min.
  - Handle the night crossing midnight correctly.
  - Energy for the night = `StreetlightCount × StreetlightWatts × Σ(hours × level)` / 1000, in kWh.
- **Endpoints (anonymous):**
  - `GET /api/lighting/schedule?days=7` (1–31): `[{ date, sunrise, sunset, lightsOn, dimStart, dimEnd, lightsOff, litHours, estimatedKwh }]` in local ISO times with offset.
  - `GET /api/lighting/now`: `{ status: "On" | "Dimmed" | "Off", brightnessPercent, nextChange: {at, toStatus} }`.
- **Remove:** `SmartLightingService`, `WeatherResponse`, the WeatherApi config, the HttpClient registration, and the old `/api/lighting/status`.
- **Tests:**
  - Athens (37.98, 23.73), 2026-06-21: sunrise about 06:02 and sunset about 20:51 local (±5 min).
  - 2026-12-21: sunrise about 07:38, sunset about 17:09 (±5 min).
  - The schedule's on/dim/off ordering is valid for every day of a year.

### 1.4 Parking
- **`ParkingSpace` adds:**
  - `Latitude`, `Longitude`
  - `Address`
  - `Type` (`Street`, `Lot`, `Disabled`, `EV`)
  - `HourlyRate` (decimal), `IsActive`
- **Endpoints:**
  - `GET /api/parking` (anonymous): active spaces with `freeNow` = MaxVehicles − reservations active *now* (start ≤ now < end). Keep `currentVehicles` for compatibility.
  - Admin CRUD at `/api/admin/parking` (the point must be inside the boundary).
  - `GET /api/admin/parking/{id}/reservations` (Admin): upcoming and active reservations with the username (no email or phone).
  - Reserving keeps requiring login, and keeps the peak-occupancy capacity rule already in `ParkingService`. Don't weaken it.
- **Seed:** add coordinates and types to the existing spaces, plus a few more, all inside the polygon.

### 1.5 Events
- **`Event` adds:**
  - `Location` (text), `Latitude?`, `Longitude?`
  - `EndDate?`, `Category` (`Culture`, `Sports`, `Council`, `Environment`, `Other`)
  - `IsPublished`, `CreatedAt`
- **Public:**
  - `GET /api/events?from=&category=&includePast=false`: published only.
  - `GET /api/events/{id}`: published only, otherwise 404.
  - Response DTO: `{ id, title, description, photoUrl, location, lat, lng, date, endDate, category, interestedCount, isInterested }`. **Never return user ids.** `isInterested` is only true for a logged-in caller.
- **Member:**
  - `POST /api/events/{id}/interest` (idempotent).
  - `DELETE /api/events/{id}/interest` (withdraw).
- **Admin:**
  - `GET /api/admin/events` (all, including unpublished).
  - `POST /api/admin/events`, `PUT /api/admin/events/{id}`, `DELETE /api/admin/events/{id}`.
  - `PhotoUrl` must be https if present.
  - Remove the duplicate `POST /api/admin/create-event` and `POST /api/events`, and update the client.

### 1.6 Invoices
- **`Invoice` adds:**
  - `Description`, `IssuedAt`, `DueDate`, `PaidAt?`
  - `PaymentCode` (8 characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`, cryptographic RNG)
  - `PayerName` (free text for non-registered payers)
  - `ReceiptReference?`
  - `UserId` becomes optional.
  - `InvoiceNumber` stays unique, and invoice numbers are generated as `INV-{yyyy}-{6 digits}`.
- **Public:**
  - `POST /api/public/invoices/lookup {invoiceNumber, paymentCode}` returns `{ invoiceNumber, description, amount, dueDate, isPaid, paidAt }`. **No user data.**
  - `POST /api/public/invoices/pay {invoiceNumber, paymentCode}` marks the invoice paid and returns `{ receiptReference, paidAt }`.
  - Both return the same `404 "Invoice not found or payment code is incorrect"` for an unknown number *or* a wrong code.
  - Compare codes with `CryptographicOperations.FixedTimeEquals`. Paying an already-paid invoice returns 409.
- **Member:** `GET /api/invoices/mine` (with the payment code, since it's theirs), and `POST /api/invoices/{invoiceNumber}/pay` for the owner.
- **Admin:**
  - `GET /api/admin/invoices?status=paid|unpaid|overdue`.
  - `POST /api/admin/invoices {userId?, payerName, description, amount (0.01–100000), dueDate}` returns the invoice including `paymentCode`.
  - `GET /api/admin/invoices/summary` returns `{ unpaidCount, unpaidTotal, overdueCount, paidThisMonthTotal }`.
- **Remove** the old `/api/payment` endpoints and update the client.
- **Seed:** give existing invoices descriptions, due dates and payment codes.

### 1.7 Security (all of it is required)
- **Rate limiting** (built-in `Microsoft.AspNetCore.RateLimiting`, partitioned by client IP):
  - `auth`: 10 requests per minute on login and register.
  - `public-invoice`: 5 requests per minute on the public lookup and pay.
  - A global limiter of 300 per minute.
  - Respond 429 with a `Retry-After` header.
  - The limits come from config, so tests can raise them. Tests must still prove the 429 for `public-invoice`.
- **Security headers middleware** on every API response:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
  - `Permissions-Policy: geolocation=(), camera=(), microphone=()`
  - HSTS outside Development.
- **Input validation:** DataAnnotations on every new DTO, plus explicit coordinate ranges, string lengths, the https photo URL, the GeoJSON size and shape, and the amount range.
- **Least data:**
  - Public responses never include user ids, emails or phone numbers.
  - Admin reservation lists show usernames only.
- **Authorization:** every `/api/admin/*` endpoint uses `[Authorize(Policy = "RequireAdminRole")]` at the class level.
- **Audit log:**
  - Entity `AuditEntry { Id, UserId, Username, Action, EntityType, EntityId, AtUtc, Details (≤500 chars) }`.
  - Written for every admin create, update and delete, and for municipality changes.
  - `GET /api/admin/audit?take=50` (Admin).
- **Login:** keep the generic message and lockout.

### 1.8 Tests (API.Tests)
- **GeoService:** inside, outside, a point in a hole, MultiPolygon.
- **Route:** visits every eligible bin once, starts and ends at the depot, and on a small set 2-opt is no longer than nearest-neighbour alone.
- **Sun calculator and schedule:** the cases in 1.3.
- **Invoices:**
  - lookup with a wrong code and with an unknown number both return the identical 404 body
  - paying twice returns 409
  - the 6th lookup within a minute returns 429
- **Events:** the public list has no user ids, and unpublished events are hidden from the public.
- **Admin endpoints:** 401 anonymous and 403 Member, for a sample of the new admin routes.
- **Bins:** creating outside the boundary returns 400.
- **Keep all existing tests passing.**

### 1.9 Migration
- One EF migration, `MunicipalityPlatform`.
- Use a local tool manifest (`dotnet new tool-manifest` + `dotnet tool install dotnet-ef --version 10.*`) if `dotnet ef` is missing.
- The running API locks `API/bin`, so build with `-p:OutDir=<temp>/` or use `--no-build` patterns as needed.

---

## 2. Frontend (Angular 22, NgModule style)

### 2.1 Foundation (agent F1)
- Install `leaflet@1.9` and `@types/leaflet`, and add `node_modules/leaflet/dist/leaflet.css` to the angular.json styles.
- Replace the Font Awesome CDN link with `@fortawesome/fontawesome-free` (npm, CSS in angular.json styles), so the app has no third-party script or style origins.
- **Shared `MapComponent`** (`client/src/app/_shared/map/`), selector `app-map`, declared in AppModule:
  - **Inputs:**
    - `boundary: GeoJSON.Geometry | null`
    - `markers: MapMarker[]`, where `MapMarker = { id: number|string; lat; lng; color: string; iconClass: string; title: string; subtitle?: string }`
    - `route: {lat,lng}[] | null` (polyline)
    - `editable = false`
    - `height = '420px'`
    - `selectedId`
  - **Outputs:** `mapClick: {lat,lng}`, `markerClick: id`.
  - **Behaviour:**
    - fits to the boundary, or to the centre and zoom
    - draws the boundary outline and lightly dims outside it if easy
    - markers are `L.divIcon` coloured circles containing the FA icon (no image assets)
    - popups are built with DOM `textContent`, **never innerHTML with data**
  - Uses OpenStreetMap tiles with attribution.
- **`MunicipalityService`** (caches `GET /api/municipality`).
- **Routing and nav skeleton for every page in 2.2–2.4.** F1 creates *all* new routes and placeholder components (title only) so F2 and F3 only fill in component files.
  - **Public:** `/` home, `/events`, `/events/:id`, `/parking`, `/waste`, `/lighting`, `/pay`.
  - **Member:** `/member` (community), `/my/invoices`, `/traffic` (unchanged).
  - **Admin:** `/admin` (dashboard), `/admin/municipality`, `/admin/bins`, `/admin/route`, `/admin/parking`, `/admin/events`, `/admin/events/new`, `/admin/events/:id/edit`, `/admin/invoices`, `/admin/invoices/new`, `/admin/users`, `/admin/audit`.
  - Remove the old `/trash`, `/admin/create-event` and `/admin/user-management` routes, and redirect them to the new ones.
  - **Nav:**
    - Public links: Events, Parking, Waste map, Lighting, Pay a bill.
    - Member extras in the user menu: Community, My invoices.
    - An Admin dropdown with all admin pages.
- **F1 pages:**
  - `/waste`: a public bins map with a type filter, a legend, and a fill colour by percentage (status colours).
  - `/admin/municipality`: a settings form plus a boundary upload (file input or paste) with a live map preview before saving, and a note on how to export the boundary from OpenStreetMap.
  - `/admin/bins`: a map-click to add a bin, with a side panel form for type, address, capacity and fill. Click a marker to edit, Empty, or Remove (with a confirm). Show the server's outside-boundary error inline.
  - `/admin/route`: type and min-fill filters, then a route polyline on the map plus an ordered stop list, total km, and a print button.

### 2.2 Lighting and Events (agent F2)
- **`/lighting` (public):**
  - a "now" status card (On/Dimmed/Off, brightness, next change)
  - a 7-day schedule table: sunrise, sunset, on, dim, off, lit hours, kWh
  - a small bar chart of lit hours per day, in pure CSS
  - a short "How it's calculated" explainer
- **`/events` (public):**
  - category filter chips, upcoming/past toggle
  - cards with date, location and interested count
  - "I'm interested" and "Withdraw" for logged-in users; anonymous users see "Log in to show interest"
- **`/events/:id`:** detail, with a small map when coordinates exist.
- **`/admin/events`:** a table with a published toggle, edit and delete.
- **`/admin/events/new` and `/:id/edit`:** a shared form (title, description, category, location, a pick-a-point mini map, start and end, photo https URL, published), with validation.

### 2.3 Parking, Invoices, Dashboard (agent F3; starts after F1 is merged, since it uses MapComponent)
- **`/parking` (public):** a map of spaces coloured by free/full, with a list next to it (free now, type, rate). Logged-in users reserve from the list (the existing reservation flow and validation). Anonymous users see "Log in to reserve".
- **`/admin/parking`:** map CRUD like the bins page (name, address, type, capacity, hourly rate), plus a reservations drawer per space.
- **`/pay` (public):**
  - invoice number + payment code, then the invoice details card, then Pay, then a receipt (reference, paid at, print)
  - clear errors and a "where to find your code" hint
  - logged-in users also see a link to My invoices
- **`/my/invoices`:** a list with status badges and Pay.
- **`/admin/invoices`:** status filter tabs and a table.
- **`/admin/invoices/new`:** the form. On success it shows the **payment code once**, with a print-friendly "bill" view.
- **`/admin` dashboard:**
  - KPI cards: bins ≥ 80% full, free parking now, unpaid total and overdue count, upcoming events
  - quick links
  - the last 10 audit entries, linking to `/admin/audit` (a table)
  - keep the comment moderation queue
- **`/admin/users`:** the existing user-management component, moved.

### 2.4 Rules for every frontend agent
- `standalone: false` and the existing changeDetection style, template-driven forms, and the shared CSS classes (`.page`, `.page-header`, `.empty-state`, `.loading-state`, `.card-elevated`, `.text-on-warning`).
- Errors through `getErrorMessages` and the interceptor. Use `SKIP_ERROR_TOAST` when you show errors inline.
- **No `innerHTML` with API data. No new third-party origins** except `https://*.tile.openstreetmap.org` for tiles.
- `npx ng build` and the headless tests must pass. Add specs for `MapComponent` input handling (F1), the schedule formatting helpers (F2), and the pay flow component (F3).
- Small commits with conventional messages. **No Co-Authored-By or attribution trailers.** Never push.

### 2.5 Deployment security (F1)
`client/nginx.conf` adds:
- `Content-Security-Policy: default-src 'self'; img-src 'self' data: https://*.tile.openstreetmap.org; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: geolocation=(), camera=(), microphone=()`

---

## 3. Build order
1. **B1 (Haiku):** backend 1.1–1.3 (municipality, geo, bins, route, lighting) + tests + migration part 1.
2. **B2 (Haiku):** backend 1.4–1.7 (parking, events, invoices, security, audit) + tests + migration part 2. B1 and B2 are sequential because they share `Program.cs`, `DataContext` and the migrations.
3. **F1 and F2 (Haiku, parallel worktrees).** F2 only fills its own component and service files. F1 owns `app.module.ts`, routing, nav and `styles.css`.
4. **F3 (Haiku)** after F1 merges.
5. **Sonnet** end-to-end test as anonymous, member and admin, at 1280 and 375. Then fix rounds (Opus plans, Haiku builds, Sonnet retests) until clean.
6. **Phase 4:** Opus review of the whole branch.

## 4. Test plan (Sonnet)
- **Anonymous:**
  - home, events list and detail, parking map, waste map, lighting schedule
  - pay a bill with a seeded invoice number + code; a wrong code gives the generic error; repeated wrong lookups eventually return 429 with a friendly message
  - admin pages redirect home
- **Member:** event interest and withdraw; reserve parking (capacity respected); My invoices; community comment.
- **Admin:**
  - upload or replace the boundary (paste the seeded GeoJSON back)
  - add a bin inside (OK) and outside (inline error)
  - empty a bin, compute a route (polyline + stops)
  - create, edit and unpublish an event (hidden from the public)
  - create a parking space; issue an invoice (the code is shown once) and pay it publicly
  - the audit log shows each action
- **Security checks:**
  - API response headers present
  - public event JSON has no user ids
  - the public invoice responses contain no user data
  - no console errors, and no horizontal overflow at 375px
