# Phase 1 – Database Schema Design

**Goal**: Gather information about your entity's fields, types, relationships, and constraints.

**Reference**: [liquibase-best-practices.mdc](mdc:.cursor/rules/database-rules/liquibase-best-practices.mdc) | [database-workflow.mdc](mdc:.cursor/rules/database-rules/database-workflow.mdc)

---

## Before You Begin

Review existing tables in [`database-schema-summary.md`](mdc:docs/technical/meta-data/database-schema-summary.md) for patterns and potential relationships.

> Standard audit fields (id, created_at, updated_at, is_deleted, etc.) are included automatically.

---

### Step 1 – Define Fields

Skip if adding endpoints to an existing entity.

Propose business fields using simple names:

> For **{{entityName}}**, I'd suggest these fields:
> - {{guessedField1}}
> - {{guessedField2}}
> - {{guessedField3}}
>
> What fields does your entity need? (Just list names, I'll handle the types)

**Wait for user's field list. Store as `{{fieldNames}}`.**

---

### Step 2 – Propose Types

Convert field names to database types:

> Here are my type guesses for **{{entityName}}**:
>
> | Field | Type | Notes |
> |-------|------|-------|
> | {{field1}} | {{guessedType1}} | {{notes1}} |
> | {{field2}} | {{guessedType2}} | {{notes2}} |
>
> Any adjustments?

**Type inference patterns:**
- `name`, `title`, `label` → `varchar(100)`
- `description`, `content` → `varchar(500)` or `text`
- `email` → `varchar(255)`
- `phone` → `varchar(20)`
- `url`, `image_url` → `varchar(500)`
- `price`, `amount`, `cost` → `decimal(10,2)`
- `count`, `quantity` → `integer`
- `is_*`, `has_*` → `boolean`
- `*_at`, `*_date` → `timestamp`

**Wait for user confirmation. Store as `{{columns}}`.**

---

### Step 3a – Belongs-to Relationships

**CRITICAL**: Analyze existing tables from database schema and suggest only relevant belongs-to relationships. For example, if creating "Message", suggest "Chat" if Chat table exists. Only include options that make logical sense.

Check existing tables and suggest:

> Does a **{{entityName}}** belong to a single:
> - a. {{suggestedEntity1}}? (e.g., User, if User table exists and makes sense)
> - b. {{suggestedEntity2}}? (e.g., Chat, if Chat table exists and entity is Message)
> - c. {{suggestedEntity3}}? (only if relevant)
> - d. None
>
> *(Adds a foreign key column to your table)*

**Wait for answer. Store as `{{belongsToRelationships}}`.**

---

### Step 3b – Has-many Relationships

**CRITICAL**: Analyze both existing tables AND fields from Step 1. Suggest:
- Existing entities that this entity might have multiple of (e.g., if "Document" table exists and entity is "Applicant", suggest "Documents")
- Fields from Step 1 that should be separate tables (e.g., if "notes" was suggested as a field, recommend it as a Notes table)

Check existing tables AND Step 1 fields to suggest:

> Should **{{entityName}}** have multiple:
> - a. {{existingEntity}}? (if {{existingEntity}} table exists and makes sense)
> - b. {{fieldFromStep1}}? (if {{fieldFromStep1}} should be a separate table, e.g., "notes" → Notes)
> - c. {{otherSuggestion}}? (only if relevant)
> - d. None
>
> *(Creates separate tables with {{entity_name}}_id foreign keys)*

**Wait for answer. Store as `{{hasManyRelationships}}`.**

---

### Step 3c – Many-to-many Relationships

**CRITICAL**: Analyze existing tables to suggest only relevant many-to-many relationships. For example, if "Tag" table exists, suggest it; otherwise don't suggest them. Only include options that make logical sense.

Check existing tables to suggest relevant many-to-many relationships:

> Does a **{{entityName}}** have many-to-many relationships?
> - a. Yes, with {{suggestedEntity1}} (if {{suggestedEntity1}} table exists and makes sense)
> - b. Yes, with {{suggestedEntity2}} (if {{suggestedEntity2}} table exists and makes sense)
> - c. Yes, with {{suggestedEntity3}} (only if relevant)
> - d. No
>
> *(Creates linking tables: {{entity_name}}_{{suggestedEntity1}}, etc.)*

**Wait for answer. Store as `{{manyToManyRelationships}}`.**

**Combine all relationship choices into `{{relationships}}`.**

---

### Step 4 – Propose Constraints

Suggest which fields should be required, unique, or have defaults:

> Here's what I'm thinking for constraints:
>
> | Field | Required | Unique | Default |
> |-------|----------|--------|---------|
> | {{field1}} | yes | no | — |
> | {{field2}} | no | no | — |
> | {{field3}} | yes | yes | — |
>
> Any changes?

**Wait for user confirmation. Store as `{{constraints}}`.**

---

> **Phase 1 of 5 — Database Schema Design: ✅ Complete.**

Return to orchestrator for Phase 2 (Liquibase migration generation).

