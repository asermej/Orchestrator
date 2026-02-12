# Phase 4 – Acceptance Tests Workflow

**Goal**: Create and verify acceptance tests for the new domain logic, ensuring it works correctly before building the API.

**CRITICAL TESTING BOUNDARY**: Tests MUST ONLY interact with the `DomainFacade` interface.  
Do **NOT** create tests for individual managers, data managers, or validators.

**Cleanup**: Generated tests MUST follow the ID-tracked cleanup pattern from [DomainFacadeTests.mdc](mdc:.cursor/rules/class-rules/acceptance-tests-class-rules/DomainFacadeTests.mdc) and [Test Cleanup](mdc:docs/technical/reference/guides/acceptance-test-guides/test-cleanup.md): track created entity IDs in lists; in TestCleanup, delete only those entities via DomainFacade in reverse dependency order. No pattern-based or SQL cleanup in TestInitialize/TestCleanup.

When this workflow is active, follow these steps **in order**, waiting for user responses where indicated.

---

### Step 1 – Generate Test File

> "Now for the **Acceptance Test**.
>
> I'll generate the test code with:
> * **File**: `DomainFacadeTests.{{entityName}}.cs`
> * **Location**: `Orchestrator.AcceptanceTests/Domain/`
> * **Purpose**: Test the entire `{{entityName}}` feature via `DomainFacade` ONLY, against a real test database (including CRUD and relevant search/pagination where applicable).
> * **Template**: `docs/technical/templates/acceptance-test-templates/DomainFacadeTests.Base.hbs`
> * **Best Practices**: See [DomainFacadeTests.mdc](mdc:.cursor/rules/class-rules/acceptance-tests-class-rules/DomainFacadeTests.mdc)
> * **Related Guides**: [Test Cleanup](mdc:docs/technical/reference/guides/acceptance-test-guides/test-cleanup.md)
> * **Cleanup**: Use ID-tracked cleanup only; no pattern-based/SQL cleanup in test lifecycle. Delete in reverse dependency order in TestCleanup.
>
> Ready to generate the **acceptance test file**?"

**Wait for confirmation, then generate the test file.**

---

### Step 2 – Run & Report Tests

> "After generating the test file, I'll run the acceptance tests (`dotnet test`) to verify the domain logic.
>
> I'll report:
> * Whether the tests passed or failed.
> * Any failing test names and their error messages.
>
> Proceed with running the acceptance tests now?"

**Wait for confirmation, then run the tests.**

After running, summarize:

> "**Phase 4 of 5 — Acceptance Tests: ✅ Complete** (all tests passing)."

(If tests fail, report and pause until the user addresses issues.)

Return control to the orchestrator to ask if the user wants to continue to Phase 5.

