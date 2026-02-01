# Phase 5 – API Layer Workflow

**Goal**: Expose the verified domain logic via a public HTTP endpoint, strictly using the provided templates.

For each file, generate code **only** by populating the specified `.hbs` template.  
Do **not** use existing files as informal examples.

When this workflow is active, follow these steps **in order**, waiting for user responses where indicated.

---

### Step 1 – Resource Model

> "First, the **Resource Model**:
>
> * **File**: `{{entityName}}Resource.cs`
> * **Location**: `Orchestrator.Api/ResourcesModels/`
> * **Purpose**: Defines the JSON shape for API requests/responses.
> * **Template**: `docs/technical/templates/api-templates/ResourceModel.hbs`
> * **Best Practices**: See [ResourceModel.mdc](mdc:.cursor/rules/class-rules/api-class-rules/ResourceModel.mdc)
>
> Proceed with the **Resource Model**?"

**Wait for confirmation, then generate the file.**

---

### Step 2 – Mapper

> "Next, the **Mapper**:
>
> * **File**: `{{entityName}}Mapper.cs`
> * **Location**: `Orchestrator.Api/Mappers/`
> * **Purpose**: Maps between the Domain Model and the Resource Model.
> * **Template**: `docs/technical/templates/api-templates/Mapper.hbs`
> * **Best Practices**: See [Mapper.mdc](mdc:.cursor/rules/class-rules/api-class-rules/Mapper.mdc)
>
> Proceed with the **Mapper**?"

**Wait for confirmation, then generate the file.**

---

### Step 3 – Controller

> "Finally, the **Controller**:
>
> * **File**: `{{entityName}}Controller.cs`
> * **Location**: `Orchestrator.Api/Controllers/`
> * **Purpose**: Public HTTP entry point for the `{{entityName}}` feature.
> * **Template**: `docs/technical/templates/api-templates/Controller.hbs`
> * **Best Practices**:
>   * [Controller.mdc](mdc:.cursor/rules/class-rules/api-class-rules/Controller.mdc)
>   * [Error Handling](mdc:docs/technical/reference/guides/api-guides/error-handling.md)
>   * [Pagination](mdc:docs/technical/reference/guides/common-guides/pagination.md)
>
> **Authentication will be applied as:**
> * `Public` → No `[Authorize]` attribute (use `[AllowAnonymous]` if other controllers require auth).
> * `Authenticated` → `[Authorize]` at controller level.
> * `Roles: X,Y` → `[Authorize(Roles = "X,Y")]` at controller level.
> * `Policy: X` → `[Authorize(Policy = "X")]` at controller level.
>
> I'll generate the controller strictly from the template, wiring up CRUD, search, and pagination operations based on the earlier choices.
>
> Proceed with the **Controller**?"

**Wait for confirmation, then generate the file.**

---

After all API files are generated, say:

> "**Phase 5 of 5 — API Layer: ✅ Complete.**"

Return control to the orchestrator to announce workflow completion.

