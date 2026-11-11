# UI Improvement Plan

**Planned by:** Claude Opus 5.5. **Built by:** three parallel Claude Haiku agents. **Tested by:** Claude Sonnet.

This plan comes from walking through every page in a browser, as an admin and as a member, at 1280px and 375px wide.

## Findings

| # | Page | Problem |
|---|---|---|
| F1 | Traffic | **The page never renders.** `GET /api/parking/active-reservation` returns 404 when the user has no reservation, and the error interceptor sends every 404 to `/not-found`. |
| F2 | Lighting | **A missing WeatherAPI key sends you to `/server-error`**, which shows a full .NET stack trace to the user. |
| F3 | All | **The browser tab title is "Client".** There is no footer, and the content often sits right against the navbar. |
| F4 | Nav | The "Errors" debug page is linked for admins and ships to production. Admins also have no links to the citizen pages. |
| F5 | Home | Hot-linked carousel images: one rendered as a washed-out white box. There is no call to action, and the page doesn't explain what the app does. |
| F6 | Community (/member) | The "Events" heading has no content and no empty state. The comment box has no length limit and no feedback after posting. |
| F7 | Trash | Fill bars are a narrow fixed-width block, **green even when the bin is 100% full**, and the bin icons are always red. The collection path wraps into a broken arrow chain. On mobile, each bin card takes a whole screen. |
| F8 | Payments | You must already know an invoice number. There is no validation, and results are poorly laid out. |
| F9 | Admin dashboard | "Manage Events" and "Manage Comments" are bare headings with no lists and no empty states. The "Manage Users" button looks disabled (grey). |
| F10 | Create Event | No validation. A bright `#007bff` button clashes with the Flatly theme. No success feedback. |
| F11 | User management | Role chips and icon buttons are cramped. Nothing shows the user's current role. The table overflows on mobile. |
| F12 | Error pages | Not Found and Server Error are unstyled. The stack trace shows in production builds. |
| F13 | Traffic | When a reservation fails you get two toasts: one from the interceptor and one from the component. |

## Shared conventions (all three agents follow these; Agent H1 defines them)

The global styles go in `client/src/styles.css`. Agents H2 and H3 use these class names before H1 has committed them; they work together once the branches are merged.

- **Page layout:**
  - `.page`: a container with `padding-block: 1.5rem 3rem`.
  - `.page-header`: holds an `h1.page-title` (1.75rem, theme dark) and an optional `p.page-subtitle` (muted).
- **States:**
  - `.empty-state`: centred and muted, with a Font Awesome icon (`fas fa-...`, 2rem) above one line of text.
  - `.loading-state`: a centred Bootstrap `spinner-border text-primary` with "Loading…" text.
- **Cards:** `.card-elevated` is a Bootstrap `.card` with `border: 0` and a soft shadow (`0 2px 8px rgba(0,0,0,.08)`).
- **Status colours:**
  - Use Bootstrap utilities only, never hard-coded hex: `bg-success` below 50%, `bg-warning` from 50% to 79%, `bg-danger` at 80% and above.
  - Buttons use the theme classes (`btn-primary` is Flatly's dark navy, `btn-success` its teal). Never use `#007bff`.
- **Spacing:** use Bootstrap spacing utilities. Component CSS only covers what utilities can't.
- **Icons:** Font Awesome 5 (`fas`), which is already loaded from the CDN in `index.html`.

## Work packages (each agent works in its own worktree and touches only the files listed for it)

### H1: Shell, foundations and error handling
Files: `client/src/styles.css`, `client/src/index.html`, `app.component.{html,css}`, `nav/*`, `home/*`, `errors/not-found/*`, `errors/server-error/*`, `_interceptors/error.interceptor.ts` (+ its spec).
- Define the shared classes above. Give the body a light background (`#f5f7fa`), and add a sticky footer: "© {current year} Smart-Town".
- Set the `<title>` to "Smart-Town" and add a `meta description`.
- **Nav:**
  - Remove the Errors link.
  - Admins see the citizen links plus an "Admin" dropdown (Dashboard, Create Event, Manage Users).
  - Collapse the mobile menu after a link is clicked.
  - Show the logged-in username in the user dropdown, with Logout.
- **Home:**
  - Replace the carousel with a hero: a title, a one-line pitch, and a Login / Sign up call to action for anonymous users, or "Go to dashboard" when logged in.
  - Add a grid of five feature cards (Community, Lighting, Traffic & Parking, Waste, Payments), each with an icon, one sentence and a link.
  - Use no external images.
  - Keep the register toggle working: the register form from the previous fix opens from the hero call to action.
- **Error interceptor (fixes F1 and F2):**
  - 404 must NOT navigate. Toast nothing and rethrow, so components decide.
  - 500 toasts "Something went wrong on our side. Please try again." and rethrows. Don't navigate.
  - Keep the 400/401/403 behaviour.
  - Update the interceptor spec to match.
- **Error pages:**
  - Restyle Not Found and Server Error as friendly centred cards with a "Back to home" button.
  - Server Error shows details only when `!environment.production`.

### H2: Citizen pages
Files: `member/*`, `traffic/*`, `trash/*`, `pay/*`, `lighting/*`, `_services/traffic-and-parking.service.ts`, `_services/lighting.service.ts`, `_services/waste-management.service.ts`, `_services/payment.service.ts`.
- Every page uses `.page` and `.page-header`, shows `.loading-state` while loading, and `.empty-state` when a list is empty.
- **Community:**
  - Show events as responsive cards: photo if present, title, date, description, and an "I'm interested" toggle with a count.
  - The comment form has a 2000-character counter and limit, a disabled button while empty or submitting, a success toast, and clears after posting.
- **Traffic (F1, F13):**
  - In the service, map a 404 from active-reservation to `null` (`catchError` on status 404 → `of(null)`). Show "No active reservation" in that case.
  - Remove the component's own failure toast.
  - Lay out traffic and parking as cards, with an occupancy progress bar per parking space using the status colours.
  - The reservation form gets duration validation (1–1440 minutes).
- **Trash (F7):**
  - Each bin is a compact card with a horizontal Bootstrap `progress` bar showing the fill %, using the status colours, plus a percentage label and a "Last emptied" relative date.
  - Use a grid: 1 column on mobile, 2 on md, 3 on lg.
  - Show the collection path as a numbered vertical list (stepper), with the return to the start marked "Back to start".
  - The "Empty bin" button stays admin-only.
- **Payments (F8):**
  - Invoice number input is required, with an inline error.
  - Show the result as a card with the amount, a paid/unpaid badge, and a Pay button only when unpaid.
  - Add a helper line: "Find the invoice number on your municipal bill."
- **Lighting (F2):**
  - Show a town input (default "Glyfada, Athens") with a Check button.
  - On error, show an inline `alert-warning` ("Lighting data is unavailable right now") in place of navigating.
  - On success, show a status card (lights on/off, the sunrise/sunset fields the API returns, and the weather condition).

### H3: Admin pages
Files: `admin/admin-dashboard/*`, `admin/event-create/*`, `admin/user-management/*`, `_services/admin.service.ts`, `_services/event.service.ts`, `_services/comment.service.ts`.
- **Dashboard (F9):**
  - Quick-action cards that all look active.
  - "Events": a list with date, title, interested count and a Delete button behind a confirm.
  - "Comments": the unseen-comment queue, with user, date and text plus a "Mark as seen" button.
  - Both lists get empty and loading states.
- **Create Event (F10):**
  - Template-driven validation: title required (max 100), description max 1000, photo URL optional but must be a valid http(s) URL, date required and not in the past.
  - `btn-primary`, a disabled state while invalid or submitting, a success toast, and a redirect to the dashboard.
- **User management (F11):**
  - Roles as Bootstrap badges.
  - The action buttons become a `btn-group` of "Member" / "Admin" with the user's current role active.
  - Hide actions on the logged-in admin's own row.
  - On mobile (<768px) the table becomes stacked cards (use the `d-none d-md-table` / `d-md-none` pattern).

## Rules for all agents
- Use Angular 22 with the NgModule style. Keep `standalone: false` and the existing changeDetection setting on each component. Use template-driven forms (FormsModule is already imported) unless the component already uses reactive forms.
- Don't change API contracts or any file under `API/`. Don't touch `API/Services/ParkingService.cs` (it has the user's uncommitted work).
- `npx ng build` must pass. Update existing specs you break, and don't delete tests to make them pass.
- Make small commits with conventional messages (`feat(client): ...`, `fix(client): ...`). **No Co-Authored-By or other attribution trailers.** Never push.

## Test plan (Sonnet)
Run against the merged branch with the live dev servers:
- **Users:** test as anonymous, as member `lisa`, and as `admin`, at 1280px and 375px.
- **Fixes:** check F1–F13 one by one: the traffic page renders, lighting shows the inline warning, there are no stack traces, and there are no duplicate toasts.
- **Registration:** a weak password shows the inline list of problems, and a valid one succeeds.
- **Flows:**
  - create an event (admin), then see it on Community (member) and mark interest
  - post a comment (member), then see it in the admin queue and mark it seen
  - reserve parking, then see the active reservation
  - check an invoice
- **Console:** no errors in the console on any page.
- **Report:** pass/fail per item, with screenshots of failures.
