# Hireology Test ATS

Test applicant tracking system for Orchestrator integration. Mirrors real ATS behavior: own backend, database, recruiter UI, sync to Orchestrator, and a **careers page** for testing the Orchestrator chatbot.

## Layout

- **HireologyTestAts.Api** — ASP.NET Core API (port 5001). Recruiter endpoints and sync to Orchestrator.
- **HireologyTestAts.Database** — Liquibase migrations for the `test_ats` database.
- **HireologyTestAts.Web** — Next.js app (port 3001). Recruiter UI and public careers page.

## First-time setup

1. Create the test-ats database:
   ```bash
   createdb test_ats
   ```

2. Run migrations (from repo root):
   ```bash
   just db-test-ats-update
   ```

3. **Seed the Orchestrator API key** (so Hireology Test ATS can sync jobs):
   ```bash
   just seed-ats-api-key
   ```
   This inserts an organization with `api_key = 'hireology-ats-key'` into the Orchestrator DB. Safe to run more than once.

## Run

From the **repo root**:

- **Hireology Test ATS only:** `just dev-test-ats` (API 5001 + Web 3001).
- **Orchestrator + Hireology Test ATS:** `just dev-all` (all four apps).
- **Build then run all:** `just start-all`.

Hireology Test ATS API: http://localhost:5001/swagger  
Hireology Test ATS Web: http://localhost:3001  
Careers page: http://localhost:3001/careers

## Config

### Hireology ATS key (for job sync)

The Hireology Test ATS calls Orchestrator's ATS API using a **Hireology ATS key** (`HireologyAts:ApiKey`). In Orchestrator's database this is stored as an organization's `api_key`.

**Quick:** If you ran `just seed-ats-api-key` in first-time setup, you're done — the Test ATS is already configured to use `hireology-ats-key` in `appsettings.Development.json`.

**Manual:** To create the org by hand: connect with `just db-connect`, then run the SQL in `scripts/seed-ats-api-key.sql`, or run:
   ```sql
   INSERT INTO organizations (id, name, api_key, is_active, created_at, updated_at, is_deleted)
   VALUES (gen_random_uuid(), 'Hireology Test ATS', 'hireology-ats-key', true, NOW(), NOW(), false);
   ```
If you use a different key, set it in both the DB and in **`apps/hireology-test-ats/HireologyTestAts.Api/appsettings.Development.json`** under `HireologyAts:ApiKey`.

If you prefer a different key value, set it in both the database (`api_key` column) and in `appsettings.Development.json`:

```json
"HireologyAts": {
  "BaseUrl": "http://localhost:5000",
  "ApiKey": "your-key-here"
}
```

Restart the Hireology Test ATS API after any config change. Jobs you create in the Hireology Test ATS will then sync to that organization in Orchestrator.

- **HireologyTestAts.Web:** Optional `.env.local`: `NEXT_PUBLIC_TEST_ATS_API_URL=http://localhost:5001`.

### Auth0 (login and protected API)

The recruiter UI uses **Auth0** for login. Users are created in the test-ats DB on first login and (when configured) provisioned to Orchestrator.

1. **Create an Auth0 application** (e.g. "Hireology Test ATS") and an **API** in your Auth0 tenant.
2. **API (`HireologyTestAts.Api`):**
   - In `appsettings.json` or `appsettings.Development.json` set:
     - `Auth0:Authority` — e.g. `https://your-tenant.auth0.com/`
     - `Auth0:Audience` — your Auth0 API identifier (e.g. `https://test-ats-api`)
   - If these are missing, the API runs but all protected endpoints return 401.
3. **Web (`HireologyTestAts.Web`):**
   - Copy `.env.local.example` to `.env.local` and set:
     - `APP_BASE_URL` — e.g. `http://localhost:3001`
     - `AUTH0_SECRET` — a long random string (min 32 chars)
     - `AUTH0_DOMAIN` — your Auth0 tenant (e.g. `your-tenant.auth0.com`)
     - `AUTH0_CLIENT_ID` and `AUTH0_CLIENT_SECRET` — from your Auth0 application
     - `AUTH0_AUDIENCE` — same as the API’s Auth0 API identifier

After Auth0 is configured, open http://localhost:3001; you’ll be redirected to log in. Then use **Management** to add groups, locations (organizations), and assign yourself (or other users) to groups or locations. The **location switcher** in the top-right uses your current selection to scope jobs.

### Groups and organizations (hierarchy)

- **Groups** — e.g. a dealership group. Create them under Management → Groups.
- **Locations (organizations)** — belong to a group. Create under Management → Locations.
- **Users** — get access by being assigned to one or more **groups** (see all locations in that group) and/or specific **organizations** (see only that location). Assign under Management → Users.
- **Jobs** — are created for a specific organization. The jobs list is scoped by your selected location (set in the header switcher).

See [docs/plans/ats-integration-phase1-test-ats.md](../../docs/plans/ats-integration-phase1-test-ats.md) and [docs/plans/test-ats-auth0-hierarchy.md](../../docs/plans/test-ats-auth0-hierarchy.md) for the full plan.

---

## What you still need to do (after repo setup)

1. **Database:** Create the `test_ats` database and run `just db-test-ats-update` (if not done).
2. **Orchestrator API key:** Run `just seed-ats-api-key` so the test ATS can sync jobs to Orchestrator.
3. **Auth0 (to log in and use protected pages):**
   - In the [Auth0 Dashboard](https://manage.auth0.com/), create a **new Application** (e.g. "Hireology Test ATS", Single Page or Regular Web) and an **API** (identifier = your audience URL).
   - **Web:** Edit `apps/hireology-test-ats/HireologyTestAts.Web/.env.local` and set:
     - `AUTH0_DOMAIN` — your tenant, e.g. `your-tenant.auth0.com`
     - `AUTH0_CLIENT_ID` and `AUTH0_CLIENT_SECRET` — from the new Application
     - `AUTH0_AUDIENCE` — the API identifier (e.g. `https://test-ats-api`)
     - `AUTH0_SECRET` — a random string of at least 32 characters (for session cookies)
   - **API:** Edit `apps/hireology-test-ats/HireologyTestAts.Api/appsettings.Development.json` and set:
     - `Auth0:Authority` — `https://your-tenant.auth0.com/`
     - `Auth0:Audience` — same as `AUTH0_AUDIENCE` above
   - Restart the API and Web after changing config.
4. **First login:** After Auth0 is set, open http://localhost:3001, log in, then go to **Management** → create a Group, then Locations, then **Users** → assign yourself to a group or location so the location switcher and jobs work.
