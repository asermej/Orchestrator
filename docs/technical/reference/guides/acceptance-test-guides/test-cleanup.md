# Test Cleanup Guide

## Standard: Centralized SQL Cleanup

**All acceptance tests use `TestDataCleanup.CleanupAllTestData()` in both TestInitialize and TestCleanup.**

### How It Works

- **Organization-scoped**: All test entities live under organizations whose name starts with `TestOrg_`. Cleanup finds those org IDs and deletes all child rows in **reverse FK order** (children before parents).
- **User-scoped**: Test users are identified by the `@example.com` email pattern.
- **Crash-proof**: TestInitialize cleanup catches orphans from previous failed/killed runs. TestCleanup is a courtesy cleanup for the current run.
- **Scales with new entity types**: When a new entity type is added, add a single DELETE statement to `TestDataCleanup.CleanupAllTestData()` in the correct FK position.

### Required Conventions

- All test organization names **MUST** start with `TestOrg_`.
- All test user emails **MUST** use `@example.com`.

### Pattern

```csharp
[TestInitialize]
public void TestInitialize()
{
    TestDataCleanup.CleanupAllTestData();
    _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());
}

[TestCleanup]
public void TestCleanup()
{
    try { TestDataCleanup.CleanupAllTestData(); }
    catch (Exception ex) { Console.WriteLine($"Warning: {ex.Message}"); }
    finally { _domainFacade?.Dispose(); }
}
```

### FK Dependency Order (Delete Sequence)

1. `follow_up_selection_logs` (child of interview_responses)
2. `follow_up_templates` (child of interview_configuration_questions)
3. `interview_responses` (child of interviews)
4. `interview_results` (child of interviews)
5. `interviews` (child of jobs, applicants, agents)
6. `interview_configuration_questions` (child of interview_configurations)
7. `interview_configurations` (child of organizations, agents)
8. `applicants` (child of organizations)
9. `jobs` (child of organizations)
10. `consent_audit` (child of agents)
11. `webhook_deliveries` / `webhook_configs` (child of organizations)
12. `agents` (child of organizations)
13. `organizations` (root)
14. `users` (standalone, by email pattern)

### Adding a New Entity Type

When adding a new entity type that acceptance tests write to:

1. Add a DELETE statement in `TestDataCleanup.CleanupAllTestData()` at the correct FK position.
2. Add a verification check in `ManualCleanup.VerifyDatabase_NoTestDataRemains`.
3. Ensure test data uses `TestOrg_` organizations so it is scoped by the cleanup queries.

## Manual / Emergency Cleanup

`ManualCleanup.cs` provides manual cleanup for emergencies (e.g. data left from a process kill). It calls the same `TestDataCleanup.CleanupAllTestData()` method.

Run `VerifyDatabase_NoTestDataRemains` after cleanup to confirm the database is clean.

## Failure Behavior

- **Test fails (exception/assertion)**: TestCleanup still runs; centralized cleanup removes all test data.
- **Process killed / crash**: TestCleanup does not run. Next test run's TestInitialize cleans orphaned data automatically.
- **Manual recovery**: Run `ManualCleanup.CleanupDatabase_RemoveAllTestData` and then `VerifyDatabase_NoTestDataRemains`.
