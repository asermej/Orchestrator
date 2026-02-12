# Hireology Test ATS Database

Liquibase migrations for the Hireology Test ATS application. Uses a **separate database** named `test_ats` (not the Orchestrator `orchestrator` database).

## First-time setup

1. Create the database (run once):
   ```bash
   createdb test_ats
   ```
   Or with explicit host/user: `createdb -h localhost -U postgres test_ats`

2. Run migrations from the repo root:
   ```bash
   just db-test-ats-update
   ```
   Or manually:
   ```bash
   liquibase --defaultsFile=apps/hireology-test-ats/HireologyTestAts.Database/liquibase/config/liquibase.properties \
     --changelog-file=apps/hireology-test-ats/HireologyTestAts.Database/changelog/db.changelog-master.xml \
     update
   ```

## Configuration

Connection details are in `liquibase/config/liquibase.properties`. Default: `localhost:5432`, database `test_ats`, user `postgres`, password `postgres`. Override via environment or a local properties file if needed.
