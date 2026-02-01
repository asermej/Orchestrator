# Database Context Reference

This document provides context for understanding the database schema and domain model patterns.

---

## Quick Reference

| Resource | Location | Purpose |
|----------|----------|---------|
| Schema Summary | [`database-schema-summary.md`](database-schema-summary.md) | Current database tables and columns |
| Entity Base Class | [`Entity.cs`](../../../apps/api/Orchestrator.Domain/Managers/Models/Entity.cs) | Base class all entities inherit from |
| Domain Models | `apps/api/Orchestrator.Domain/Managers/Models/` | C# entity definitions with column mappings |
| Liquibase Changelog | `apps/api/Orchestrator.Database/changelog/changes/` | Schema migration history |

---

## A. Schema Summary

**File**: [`database-schema-summary.md`](database-schema-summary.md)

Contains the current database schema extracted directly from PostgreSQL. Shows all tables, columns, types, and constraints.

**Regenerate after Liquibase changes:**
```bash
just update-db-context
```

---

## B. Entity Base Class

**File**: [`Entity.cs`](../../../apps/api/Orchestrator.Domain/Managers/Models/Entity.cs)

All domain entities **must inherit from `Entity`**. This base class provides standard audit and soft-delete fields:

| Property | Type | Column | Description |
|----------|------|--------|-------------|
| `Id` | `Guid` | `id` | Primary key (auto-generated UUID) |
| `CreatedAt` | `DateTime` | `created_at` | When the record was created |
| `UpdatedAt` | `DateTime?` | `updated_at` | When the record was last updated |
| `CreatedBy` | `string?` | `created_by` | Who created the record |
| `UpdatedBy` | `string?` | `updated_by` | Who last updated the record |
| `IsDeleted` | `bool` | `is_deleted` | Soft delete flag (default: false) |
| `DeletedAt` | `DateTime?` | `deleted_at` | When the record was soft deleted |
| `DeletedBy` | `string?` | `deleted_by` | Who soft deleted the record |

Your Liquibase migration **must include these columns** in the table definition.

---

## C. Domain Model Patterns

**Location**: `apps/api/Orchestrator.Domain/Managers/Models/`

Examine existing models to understand naming conventions and patterns.

**Table Naming:**
- Use `[Table("snake_case_plural")]` attribute
- Example: `[Table("users")]`, `[Table("chat_topics")]`

**Column Naming:**
- Use `[Column("snake_case")]` attribute on every property
- Example: `[Column("first_name")]`, `[Column("profile_image_url")]`

---

## D. Liquibase Files

| File | Location |
|------|----------|
| Master Changelog | `apps/api/Orchestrator.Database/changelog/db.changelog-master.xml` |
| Change Files | `apps/api/Orchestrator.Database/changelog/changes/` |

Files are named with timestamps: `YYYYMMDDHHMMSS-description.xml`

---

## E. Type Mappings

| C# Type | PostgreSQL Type | Notes |
|---------|-----------------|-------|
| `Guid` | `uuid` | Use `gen_random_uuid()` default |
| `string` | `varchar(n)` | Specify max length |
| `string?` | `varchar(n)` | Nullable |
| `int` | `integer` | |
| `long` | `bigint` | |
| `decimal` | `numeric(p,s)` | Specify precision and scale |
| `bool` | `boolean` | |
| `DateTime` | `timestamp` | Without time zone |
| `DateTimeOffset` | `timestamptz` | With time zone |


