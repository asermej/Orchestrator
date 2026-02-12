# Test Utilities

This folder contains utilities for test data management.

## Cleanup Strategy

**All acceptance tests use centralized SQL cleanup via `TestDataCleanup.CleanupAllTestData()`.**

This method is called in **both** `TestInitialize` (to clean orphans from previous crashed/failed runs) **and** `TestCleanup` (courtesy cleanup after each test).

### How It Works

1. **Organization-scoped**: All test entities live under organizations whose name starts with `TestOrg_`. Cleanup finds those org IDs and deletes all child rows in reverse FK order.
2. **User-scoped**: Test users are identified by the email pattern `@example.com`.
3. **Safe**: Uses `TryExecute` so missing tables/columns don't fail the cleanup.
4. **Crash-proof**: Because TestInitialize runs cleanup before each test, orphaned data from a previous killed/crashed run is automatically cleaned on the next run.

### Required Convention

- **All test organization names MUST start with `TestOrg_`** (e.g. `TestOrg_{Guid.NewGuid():N}`).
- **All test user emails MUST use `@example.com`** (e.g. `test_{uniqueId}@example.com`).

### Pattern

```csharp
[TestClass]
public class DomainFacadeTestsMyEntity
{
    private DomainFacade _domainFacade = null!;

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
}
```

### Adding a New Entity Type

When adding a new entity type that acceptance tests write to:

1. **Add a DELETE statement** in `TestDataCleanup.CleanupAllTestData()` in the correct FK position (children before parents).
2. **Add a verification check** in `ManualCleanup.VerifyDatabase_NoTestDataRemains`.
3. **Ensure test data uses `TestOrg_` organizations** so it's scoped by the cleanup queries.

## Manual Cleanup

The `ManualCleanup.cs` class provides **manual/emergency** cleanup (e.g. after failed runs that left orphaned data). It calls the same `TestDataCleanup.CleanupAllTestData()` method.

### Running Manual Cleanup

1. Open Test Explorer in your IDE
2. Find the `ManualCleanup` test class
3. Run `CleanupDatabase_RemoveAllTestData` to clean orphaned data
4. Run `VerifyDatabase_NoTestDataRemains` to verify cleanup

## Files

- `ManualCleanup.cs` - Manual test methods for emergency cleanup and verification
- `TestDataCleanup.cs` - Static utility for centralized SQL cleanup
