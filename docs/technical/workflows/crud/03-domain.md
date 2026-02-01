# Phase 3 – Domain Layer Workflow

**Goal**: Implement the core business logic, one class at a time, with strict verification against the source templates.

**CRITICAL NOTE ON JOIN TABLES**: If you are creating a junction/join table entity (e.g., `PersonaTopic`, `TopicTag`), do **NOT** expose it directly in the `DomainFacade`. Instead:

1. Create the join table manager internally.  
2. Add relationship management methods to the **primary entity's manager** (e.g., `PersonaManager` or `TopicManager`).  
3. Expose these methods through the primary entity's `DomainFacade` extension.  
4. Keep the join table manager internal and never reference it in `DomainFacade.cs`.

See [Join Table Encapsulation](docs/technical/reference/core/principles.md#join-table-encapsulation).

When this workflow is active, follow these steps **in order**, waiting for user responses where indicated.

---

### Verification Process for Domain Layer Files

For **every** domain file:

1. **Generate**: Create the file by populating the relevant `.hbs` template with the feature details.  
2. **Verify**: Re-populate the same template in memory to create an "expected" version. Read the generated file from disk and compare.  
3. **Notify**: Report whether the generated file perfectly matches the template's expected output. If there are discrepancies, highlight them.

Always tell the user, for example:

> "File `X` generated and verified: ✅ matches template"  
> or  
> "File `X` has differences from the template: …"

---

### Step 1 – Domain Model

> "Now for the **Domain Model**.
>
> I'll generate and then verify the code based on:
> * **File**: `{{entityName}}.cs`
> * **Location**: `Orchestrator.Domain/Managers/Models/`
> * **Purpose**: Defines the core C# entity for `{{entityName}}`.
> * **Template**: `docs/technical/templates/domain-templates/DomainModel.hbs`
> * **Best Practices**: See [DomainModel.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/DomainModel.mdc)
>
> Proceed with generating the **Domain Model**?"

**Wait for confirmation, then generate and verify the file.**

---

### Step 2 – Exceptions

> "Next, I'll create the **Custom Exceptions** for this feature:
>
> * `{{entityName}}ValidationException.cs`
> * `{{entityName}}NotFoundException.cs`
> * `{{entityName}}DuplicateException.cs`
>
> **Location**: `Orchestrator.Domain/Managers/Exceptions/`  
> **Templates**:
> * `docs/technical/templates/domain-templates/exceptions/ValidationException.hbs`
> * `docs/technical/templates/domain-templates/exceptions/NotFoundException.hbs`
> * `docs/technical/templates/domain-templates/exceptions/DuplicateException.hbs`
> **Best Practices**: See [Exception.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/Exception.mdc)
>
> I'll generate and verify each file against its template and report the results.
>
> Proceed with the **Exceptions**?"

**Wait for confirmation, then generate and verify all three exception files.**

---

### Step 3 – Data Manager

> "Now, the **Data Manager**.
>
> * **File**: `{{entityName}}DataManager.cs`
> * **Location**: `Orchestrator.Domain/Managers/DataLayer/DataManagers/`
> * **Purpose**: Handles all SQL queries for the `{{entityName}}` entity (including search and pagination where applicable).
> * **Template**: `docs/technical/templates/domain-templates/database/DataManager.hbs`
> * **Best Practices**: See [DataManager.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/DataManager.mdc)
> * **Related Guides**:
>   * [Column Mapping](mdc:docs/technical/reference/guides/domain-guides/database/column-mapping.md)
>   * [Dapper Configuration](mdc:docs/technical/reference/guides/domain-guides/database/dapper-configuration.md)
>   * [Timezone Handling](mdc:docs/technical/reference/guides/domain-guides/database/timezone-handling.md)
>   * [Pagination](mdc:docs/technical/reference/guides/common-guides/pagination.md)
>
> Proceed with the **Data Manager**?"

**Wait for confirmation, then generate and verify the file.**

---

### Step 4 – Data Facades

> "Now I'll create and verify the **Data Facade Extensions**:
>
> * **Files**: `DataFacade.{{entityName}}.cs`
> * **Purpose**: `partial` classes that expose the new data and domain logic.
> * **Template**: `docs/technical/templates/domain-templates/database/DataFacade.hbs`
> * **Best Practices**: See [DataFacade.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/DataFacade.mdc)
>
> Proceed with the **Data Facade extensions**?"

**Wait for confirmation, then generate and verify the file.**

---

### Step 5 – Validator

> "Next, the **Validator**:
>
> * **File**: `{{entityName}}Validator.cs`
> * **Location**: `Orchestrator.Domain/Managers/Validators/`
> * **Purpose**: Validation logic for the `{{entityName}}` entity.
> * **Template**: `docs/technical/templates/domain-templates/Validator.hbs`
> * **Best Practices**: See [Validator.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/Validator.mdc)
> * **Related Guides**: [Validation Rules](mdc:docs/technical/reference/guides/domain-guides/validation-rules.md)
>
> Proceed with the **Validator**?"

**Wait for confirmation, then generate and verify the file.**

---

### Step 6 – Domain Manager

> "Now, the **Domain Manager**:
>
> * **File**: `{{entityName}}Manager.cs`
> * **Location**: `Orchestrator.Domain/Managers/`
> * **Purpose**: Orchestrates validation and calls the `DataFacade`.
> * **Template**: `docs/technical/templates/domain-templates/Manager.hbs`
> * **Best Practices**: See [Manager.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/Manager.mdc)
>
> Proceed with the **Domain Manager**?"

**Wait for confirmation, then generate and verify the file.**

---

### Step 7 – Domain Facade

> "Next, I'll update the **Domain Facade**:
>
> * **File**: `DomainFacade.cs`
> * **Location**: `Orchestrator.Domain/`
> * **Purpose**: Register the new manager with the main `DomainFacade`.
> * **Best Practices**: See [DomainFacade.mdc](mdc:.cursor/rules/class-rules/domain-class-rules/DomainFacade.mdc)
>
> I'll insert the new manager's private field, property, and `Dispose()` call directly.
>
> Proceed with updating `DomainFacade.cs`?"

**Wait for confirmation, then update the file.**

---

### Step 8 – Domain Facade Extension

> "Finally for the domain layer, I'll create the **Domain Facade Extension**:
>
> * **Files**: `DomainFacade.{{entityName}}.cs`
> * **Purpose**: `partial` classes exposing the new domain operations via `DomainFacade`.
> * **Template**: `docs/technical/templates/domain-templates/DomainFacade.Base.hbs`
>
> Proceed with the **Domain Facade extension**?"

**Wait for confirmation, then generate and verify the file.**

---

After all domain files are generated and verified, say:

> "**Phase 3 of 5 — Domain Layer: ✅ Complete.**"

Return control to the orchestrator to ask if the user wants to continue to Phase 4.

