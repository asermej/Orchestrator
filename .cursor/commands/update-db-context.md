# Update Database Context

**Purpose**: Regenerates the database schema summary from the live PostgreSQL database.

---

## When to Use

Run this command:
- After creating a new Liquibase changeset
- After running `just db-update` to apply migrations
- Before starting a new entity to see current schema state
- Anytime you need fresh database context

---

## Execution

Run the following command:

```bash
just update-db-context
```

This will:
1. Connect to the PostgreSQL database
2. Extract schema information from `information_schema`
3. Generate `docs/technical/meta-data/database-schema-summary.md`

---

## After Execution

Confirm success by checking:
- [ ] `docs/technical/meta-data/database-schema-summary.md` was updated
- [ ] The "Generated" timestamp reflects the current time
- [ ] New tables/columns appear in the summary

---

## Environment Variables

If connecting to a non-local database, set these before running:

| Variable | Default | Description |
|----------|---------|-------------|
| `PGHOST` | `localhost` | Database host |
| `PGUSER` | `postgres` | Database user |
| `PGPASSWORD` | `postgres` | Database password |
| `PGDATABASE` | `starter` | Database name |

---

## Related Files

- **Generated Output**: [`docs/technical/meta-data/database-schema-summary.md`](mdc:docs/technical/meta-data/database-schema-summary.md)
- **Context Reference**: [`docs/technical/meta-data/database-meta-data.md`](mdc:docs/technical/meta-data/database-meta-data.md)
- **Shell Script**: `scripts/generate-schema-context.sh`