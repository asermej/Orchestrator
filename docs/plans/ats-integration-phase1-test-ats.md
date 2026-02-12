# Phase 1: Hireology Test ATS – Full ATS-Like App + Careers Page (Detailed)

This document details **Phase 1** of the ATS Integration and Test Plan. The goal is a **Hireology Test ATS app that works like an actual ATS**: its own backend, database, recruiter UI, sync to Orchestrator, and a **careers page** where we can test the Orchestrator chatbot in a realistic context. Work is broken into small steps, but we don’t stop until the test app behaves like a real ATS.

---

## Vision: What the hireology-test-ats becomes

- **Recruiter side:** Jobs and applicants stored in the hireology-test-ats database; UI to manage them and sync to Orchestrator; create interviews (job + applicant + agent) and get interview links.
- **Candidate side:** A **careers page** (like a real company career site) that lists jobs and embeds the **Orchestrator chatbot widget** so visitors can ask questions about roles and the company. This is the place to test the chatbot we’re building.
- **Infrastructure:** C# backend (same stack as Orchestrator where it helps), own database, frontend (Next.js). Same repo root justfile to start/stop everything.

---

## Architecture: C# backend + DB + frontend

### Why backend + database

- **Behave like a real ATS:** Jobs and applicants live in the hireology-test-ats first; we sync to Orchestrator. That lets us test sync behavior, webhooks later, and realistic flows.
- **Test everything:** We can test Orchestrator’s ATS API, the chatbot on the careers page, and (later) webhooks and user provisioning, all from one test app.
- **Same infrastructure patterns:** C# API + Liquibase + same justfile patterns make it familiar and consistent with the rest of the repo.

### Layout under `apps/hireology-test-ats/`

- **`apps/hireology-test-ats/HireologyTestAts.Api/`** – ASP.NET Core API. Serves recruiter CRUD (jobs, applicants), sync-to-Orchestrator (calls Orchestrator ATS API with API key), and any endpoints the careers page needs (e.g. list jobs for the public page). API key and Orchestrator URL live in config (server-side only).
- **`apps/hireology-test-ats/HireologyTestAts.Database/`** – Liquibase migrations for hireology-test-ats schema (e.g. jobs, applicants, sync state). Uses its own DB name (e.g. `test_ats`) or a separate PostgreSQL database so it doesn’t touch Orchestrator’s DB.
- **`apps/hireology-test-ats/HireologyTestAts.Web/`** – Next.js app: recruiter UI (jobs, applicants, create interview, settings) and **careers page** (public job list + embedded chatbot).
- **Solution:** Either a small `HireologyTestAts.sln` under `apps/hireology-test-ats/` (Api + Database) or add these projects to the main solution; Web stays npm. Keep it simple so the repo root justfile can `dotnet run` the API and `npm run dev` the Web.

### Ports

- Orchestrator: API **5000**, Web **3000**.
- Hireology Test ATS: API **5001**, Web **3001**.

So **`dev-all`** runs four processes: 5000, 3000, 5001, 3001; **`dev-test-ats`** runs 5001 + 3001 (and assumes Orchestrator is up if we need to sync or use the widget).

---

## Careers page (chatbot testing)

- **Purpose:** A place to test the Orchestrator chatbot in a realistic “career site” context.
- **Location:** A route in HireologyTestAts.Web, e.g. **`/careers`** or **`/jobs`**, that:
  - Lists job openings (from hireology-test-ats API, which reads from hireology-test-ats DB and/or Orchestrator as needed).
  - Embeds the **Orchestrator chatbot widget** (the same `widget.js` from Orchestrator.Web, or loaded from Orchestrator’s origin) with the correct `data-agent-id` (and optionally `data-api-base-url` pointing at Orchestrator API). So a visitor on the careers page can open the chat and talk to the AI about jobs and the company.
- **Config:** Hireology Test ATS needs to know which Orchestrator agent ID to use for the widget (e.g. from hireology-test-ats config or from the job/company settings). Orchestrator’s widget endpoint is public; the careers page is public; no API key in the browser.
- **Definition of done (for this slice):** Careers page loads, shows at least one job, and the embedded widget opens and gets responses from Orchestrator’s `/api/v1/chat/widget` so we can test the chatbot end-to-end.

---

## Justfile (single file at repo root)

- All commands stay in the **repo root `justfile`**. No separate justfile under `apps/hireology-test-ats/`.

**Targets:**

| Target | Behavior |
|--------|----------|
| **`dev-test-ats`** | Start only Hireology Test ATS: kill 5001 and 3001, then start HireologyTestAts.Api (5001) and HireologyTestAts.Web (3001). Same pattern as `dev` (prefixed logs, trap on Ctrl+C). Assumes Orchestrator is running if you need sync or widget. |
| **`dev-all`** | Start Orchestrator API (5000) + Orchestrator Web (3000) + Hireology Test ATS API (5001) + Hireology Test ATS Web (3001). One Ctrl+C stops all four. Optional `open` for Swagger, both webs, and hireology-test-ats careers page. |
| **`start-all`** | Build everything (including hireology-test-ats), then run all four. |

Keep **`dev`** and **`start`** as Orchestrator-only (5000 + 3000). Add **`db-test-ats-update`** (or similar) to run Liquibase for Hireology Test ATS DB when we have migrations.

---

## Phased breakdown (we can break further as we build)

### Phase 1a – Scaffold: backend + DB + frontend, run/stop

- **HireologyTestAts.Api:** Minimal ASP.NET Core API (e.g. one health or config endpoint). Config: Orchestrator base URL, API key, connection string for hireology-test-ats DB.
- **HireologyTestAts.Database:** Liquibase setup, one small table or placeholder migration so the DB exists and is runnable.
- **HireologyTestAts.Web:** Next.js app on 3001: home page (“Hireology Test ATS”) and placeholder nav for Jobs, Applicants, Settings, **Careers**.
- **Justfile:** `dev-test-ats` (run API 5001 + Web 3001), `dev-all` (run all four). Definition of done: `just dev-all` starts all four; hireology-test-ats home at http://localhost:3001; hireology-test-ats API at http://localhost:5001.

#### Phase 1a – Concrete task list (implementation order)

1. **Create `apps/hireology-test-ats/` directory** and decide solution layout: either a new `HireologyTestAts.sln` in `apps/hireology-test-ats/` containing only HireologyTestAts.Api (and optionally a placeholder for HireologyTestAts.Database as a “tools” project with no .csproj, just Liquibase files), or add Hireology Test ATS projects to the main `Orchestrator.sln`. Recommendation: keep a small `HireologyTestAts.sln` under `apps/hireology-test-ats/` so the main solution stays Orchestrator-only; justfile still runs `dotnet run` from the hireology-test-ats API project.

2. **HireologyTestAts.Api (minimal):**
   - Create `apps/hireology-test-ats/HireologyTestAts.Api/` with an ASP.NET Core 9 Web API project (same style as Orchestrator.Api: `Microsoft.NET.Sdk.Web`, net9.0).
   - Configure Kestrel to listen on **port 5001** (e.g. in `Program.cs` or `appsettings.json`).
   - Add `appsettings.json` and `appsettings.Development.json` with placeholders: `ConnectionStrings__HireologyTestAts` (for hireology-test-ats DB), `HireologyAts__BaseUrl`, `HireologyAts__ApiKey`.
   - Add a **health** endpoint (e.g. `GET /health` or `GET /api/health`) that returns 200 and optionally app name / version so we can confirm the API is up.
   - Add Swagger so we can open http://localhost:5001/swagger when the API runs.
   - No database access or Domain layer in 1a; just config + health + Swagger.

3. **HireologyTestAts.Database:**
   - Create `apps/hireology-test-ats/HireologyTestAts.Database/` with Liquibase layout mirroring Orchestrator.Database: `changelog/db.changelog-master.xml`, `changelog/changes/`, `liquibase/config/liquibase.properties`.
   - In `liquibase.properties` set `url: jdbc:postgresql://localhost:5432/test_ats` (and username/password as needed). Use a **separate database name** (`test_ats`) so it doesn’t touch the `orchestrator` DB.
   - Add one initial changeset (e.g. `20260209120000-initial-schema.xml`) that creates a single small table (e.g. `schema_version` or a placeholder `test_ats_metadata` with `id`, `created_at`) so the DB is created and migrations run. Follow the project’s Liquibase naming (timestamp in filename and changeset id).
   - Document in `apps/hireology-test-ats/HireologyTestAts.Database/README.md` (or in the main plan) that the user must create the database once (e.g. `createdb test_ats`) before first run.

4. **Justfile target `db-test-ats-update`:**
   - In the repo root `justfile`, add a target that runs Liquibase for hireology-test-ats (e.g. `liquibase --defaultsFile=apps/hireology-test-ats/HireologyTestAts.Database/liquibase/config/liquibase.properties --changelog-file=apps/hireology-test-ats/HireologyTestAts.Database/changelog/db.changelog-master.xml update`). This can be used after creating the DB and whenever we add migrations.

5. **HireologyTestAts.Web (Next.js):**
   - Create `apps/hireology-test-ats/HireologyTestAts.Web/` with Next.js (same major version as Orchestrator.Web, e.g. Next 15). Use `create-next-app` or copy the minimal structure from Orchestrator.Web (app router: `app/layout.tsx`, `app/page.tsx`).
   - Set dev server to **port 3001** (e.g. in `package.json`: `"dev": "next dev -p 3001"`).
   - Home page: title “Hireology Test ATS” and a short blurb that this is the test ATS app. Add a simple nav or links: **Jobs**, **Applicants**, **Settings**, **Careers** (routes can be placeholders that render “Coming soon” or the same layout with a message).
   - Add `.env.local.example` with `NEXT_PUBLIC_TEST_ATS_API_URL=http://localhost:5001` (and any other vars the frontend will need later). Add a brief `README.md` in `HireologyTestAts.Web` describing how to run and that the API runs on 5001.

6. **Justfile targets `dev-test-ats` and `dev-all`:**
   - **`dev-test-ats`:** Kill processes on 5001 and 3001 (if any). Start HireologyTestAts.Api (e.g. `cd apps/hireology-test-ats/HireologyTestAts.Api && dotnet run` with prefixed log output like `[🟠 API]`). In the same script, start HireologyTestAts.Web (e.g. `cd apps/hireology-test-ats/HireologyTestAts.Web && npm install && npm run dev` with prefixed output `[🟠 WEB]`). Use a trap so Ctrl+C kills both. Do not start Orchestrator.
   - **`dev-all`:** Kill 5000, 3000, 5001, 3001. Start Orchestrator API (5000), Orchestrator Web (3000), Hireology Test ATS API (5001), Hireology Test ATS Web (3001), all in background with prefixed logs. Single trap to kill all four on Ctrl+C. Optionally `open` http://localhost:5000/swagger, http://localhost:3000, http://localhost:5001/swagger, http://localhost:3001 (and maybe http://localhost:3001/careers as a bookmark for later).

7. **Optional: `build` and `start-all`:**
   - Extend the main `just build` (if it exists) to build the hireology-test-ats API (`dotnet build apps/hireology-test-ats/HireologyTestAts.sln` or the path to the hireology-test-ats solution). Ensure `just start-all` builds then runs all four processes with the same trap behavior as `dev-all`.

8. **Definition of done (Phase 1a):**
   - Create the `test_ats` database once (e.g. `createdb test_ats`), run `just db-test-ats-update` once.
   - `just dev-test-ats`: HireologyTestAts API responds at http://localhost:5001 (and /swagger), HireologyTestAts Web at http://localhost:3001 with “Hireology Test ATS” and nav to Jobs, Applicants, Settings, Careers.
   - `just dev-all`: All four apps run; one Ctrl+C stops all; Orchestrator and hireology-test-ats both work.

### Phase 1b – Jobs and applicants in hireology-test-ats + sync to Orchestrator

- **DB:** Jobs and applicants tables in hireology-test-ats; API to create/list/update (recruiter CRUD).
- **Sync:** “Sync to Orchestrator” flow: call Orchestrator `POST /api/v1/ats/jobs` and `POST /api/v1/ats/applicants` (with X-API-Key). Store Orchestrator external IDs if needed.
- **UI:** Recruiter pages to manage jobs and applicants and trigger sync. Agent ID: config or text field until we have list-agents from Orchestrator.

### Phase 1c – Create interview + careers page with chatbot

- **Interview:** Recruiter flow to create an interview (pick job, applicant, agent), call Orchestrator `POST /api/v1/ats/interviews`, show interview link (Orchestrator token URL).
- **Careers page:** Public route (e.g. `/careers`) that lists jobs (from hireology-test-ats API) and embeds the Orchestrator chatbot widget (agent ID from config). Verify widget loads and chats against Orchestrator so we can test the chatbot like on a real career site.

### Later (still “test like a real ATS”)

- **Webhooks:** Orchestrator calls hireology-test-ats when e.g. interview is completed; hireology-test-ats stores or displays it.
- **User provisioning:** Hireology Test ATS “users” provisioned to Orchestrator; optional SSO/login.
- **Agents/configs:** When Orchestrator exposes ATS APIs for agents and interview configs, hireology-test-ats uses them (list agents, set questions, link to interviews).

---

## Configuration summary

- **HireologyTestAts.Api:** `appsettings` / env: DB connection for hireology-test-ats, `HireologyAts__BaseUrl`, `HireologyAts__ApiKey` (never exposed to browser).
- **HireologyTestAts.Web:** Env or API call for Orchestrator base URL and agent ID for the careers page widget (only the widget’s `data-agent-id` and `data-api-base-url` need to be set; no API key in the frontend).

---

## Definition of done (overall Phase 1)

- Hireology Test ATS has a C# backend and its own database, runs via the repo root justfile alongside Orchestrator.
- Recruiter UI: manage jobs and applicants in hireology-test-ats, sync to Orchestrator, create interviews and get interview links.
- Careers page: public page listing jobs and embedding the Orchestrator chatbot; we can test the chatbot in a realistic career-site context.
- We can break implementation into smaller steps (e.g. 1a → 1b → 1c) as we build, but the target is a test app that works like an actual ATS and includes a careers page for chatbot testing.
