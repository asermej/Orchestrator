# Test ATS: Auth0, Groups/Organizations, Admin, and User Provisioning

## Scope

- **App**: `apps/hireology-test-ats/` (HireologyTestAts.Api, HireologyTestAts.Database, HireologyTestAts.Web).
- **Auth**: Auth0 for login (separate Auth0 application for test-ats; same identity used when provisioning users to Orchestrator).
- **Hierarchy**: **Groups** (e.g. Autonation) → **Organizations** (locations/dealerships). Jobs belong to an organization.
- **Access**: Users are assigned either to a **group** (see all orgs in that group) or to a specific **organization** (see only that location). Current context is one selected organization (or “all” for group) for the location switcher.
- **Orchestrator**: Provision users from test-ats to Orchestrator (create/link User by Auth0 sub) so you can test web calls and user creation.

---

## Hierarchy and permissions (important)

**Parent = Group; Children = Organizations.**

- If a user has **group-level access** (assigned to a group), they can see **all child organizations** under that group and **all jobs** in those organizations. No need to assign them to each organization.
- If a user has **organization-level access** only (assigned to one or more orgs, not to the parent group), they see only those organizations and their jobs.

When resolving “what can this user see?”:

1. Collect all organization IDs from every **group** the user has access to (via `user_group_access` → groups → organizations).
2. Add all **organizations** the user has direct access to (via `user_organization_access`).
3. Jobs are filtered to `organization_id IN (that set)`.
4. The location switcher lists: groups (with their orgs as children) and any standalone orgs the user has access to. Selecting a group could mean “all orgs in this group” or the UI can require picking a specific org for “current context” so job create/list is always org-scoped.

This keeps a single rule: **access to a parent (group) implies visibility of all children (organizations) and their jobs.**

---

## 1. Data model (HireologyTestAts.Database)

Add Liquibase migrations (UUID primary keys to match existing jobs and initial schema).

**New tables:**

- **groups** – `id` (uuid), `name`, `created_at`, `updated_at`.
- **organizations** – `id` (uuid), `group_id` (fk), `name`, optional `city`, `state`, `created_at`, `updated_at`.
- **users** – `id` (uuid), `auth0_sub` (unique), `email`, `name`, `created_at`, `updated_at`.
- **user_group_access** – `user_id`, `group_id` (unique on (user_id, group_id)). User can see all orgs (and jobs) in this group.
- **user_organization_access** – `user_id`, `organization_id` (unique on (user_id, organization_id)). User can see only this org and its jobs.

**Alter jobs:**

- Add **organization_id** (fk to organizations, nullable for migration; then backfill or enforce as required).

---

## 2. API: Groups, Organizations, Users, and auth context

**HireologyTestAts.Api:**

- **Auth**: Add Auth0 JWT validation; resolve current user from Bearer token (Auth0 sub). Config in appsettings (Authority, Audience).
- **Context**: Store “current organization id” (e.g. `GET/PUT /api/me/context`) so the location switcher has one place to switch; backend uses this to scope job list/create.
- **Permission resolution**: When scoping jobs or building the switcher list, compute the user’s allowed organization IDs as: (all orgs in groups they have) ∪ (orgs they have direct access to). Apply that set to job filters and to the list of orgs they can switch to.
- **Endpoints**: Groups CRUD; Organizations CRUD (filter by groupId); Users list/create/update and access (groupIds, organizationIds); `GET /api/me` (user + accessible groups/orgs for switcher); `GET/PUT /api/me/context`.
- **Jobs**: List/create/update scoped to current user’s allowed organizations (using the resolved set above) and optionally to current context organization for create.

---

## 3. User provisioning to Orchestrator

On first login or when an admin creates a user, call Orchestrator’s user API (create/upsert by Auth0 sub, email, name) using the test-ats server-side API key so users exist in Orchestrator for integration testing.

---

## 4. Frontend: Auth0, layout, switcher, admin, jobs

- **Auth0**: Protect routes; attach token to API requests.
- **Layout**: Location switcher in top right – list groups with child orgs (and standalone orgs) from `/api/me`; on select, `PUT /api/me/context` with chosen organizationId; refetch data so jobs are scoped to new context.
- **Admin**: Management pages for groups, organizations, users (with group and/or organization assignment). Assigning a user to a group gives them access to all child orgs and their jobs.
- **Jobs**: List and create scoped to current context organization; backend enforces that the context org is in the user’s allowed set (group children + direct org access).

---

## 5. Implementation order (suggested)

1. Database – groups, organizations, users, access tables; organization_id on jobs.
2. API – Groups/Organizations CRUD; Jobs with organization_id and scoping by resolved org set (group children + direct org access).
3. API – Auth0 and /api/me, /api/me/context; user access assignment; permission resolution helper.
4. Web – Auth0, layout with switcher, admin pages, jobs scoped to context.

---

## Diagram (conceptual)

```
Auth0 → JWT → API (current user)
                ↓
    Resolve allowed orgs = (orgs in user’s groups) ∪ (user’s direct orgs)
                ↓
    Jobs filtered by organization_id IN (allowed orgs)
    Switcher shows groups (with children) + direct orgs; selection = current context
```
