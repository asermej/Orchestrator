# Phase 2 – Liquibase Migration

**Goal**: Generate and apply the database migration using Liquibase.

**Reference**: [liquibase-best-practices.mdc](mdc:.cursor/rules/database-rules/liquibase-best-practices.mdc) | [database-workflow.mdc](mdc:.cursor/rules/database-rules/database-workflow.mdc)

---

## Before You Begin

Ensure you have completed Phase 1 (Database Schema Design) and have all the required information:
- `{{fieldNames}}` - Field names
- `{{columns}}` - Column definitions with types
- `{{relationships}}` - Relationship choices
- `{{constraints}}` - Field constraints

---

### Step 5 – Generate Migration

1. Generate timestamp-based filename: `<timestamp>-{{entityName}}.xml`
2. Create changeset in `Orchestrator.Database/changelog/changes/`
3. Register in `db.changelog-master.xml`

---

### Step 6 – Apply Migration

```bash
just db-update
just update-db-context
```

---

> **Phase 2 of 5 — Liquibase Migration: ✅ Complete.**

Return to orchestrator for Phase 3.

