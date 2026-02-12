# Hireology Test ATS Web

Next.js frontend for the Hireology Test ATS app. Recruiter UI (jobs, applicants, settings) and public **Careers** page for testing the Orchestrator chatbot.

- **Port:** 3001 (dev and start).
- **API:** Hireology Test ATS API runs on 5001; set `NEXT_PUBLIC_TEST_ATS_API_URL=http://localhost:5001` in `.env.local` if needed.

## Run

From repo root:

- **Hireology Test ATS only:** `just dev-test-ats` (starts API 5001 + this app 3001).
- **Orchestrator + Hireology Test ATS:** `just dev-all` (starts all four apps).

Or from this directory: `npm install && npm run dev`.

## No database

This app does not connect to a database directly. All data is fetched from the Hireology Test ATS API (which uses the `test_ats` database).
