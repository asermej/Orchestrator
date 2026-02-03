# Test Utilities

This folder contains utilities for test data management.

## Cleanup Strategy

**Tests should only clean up the data they create.** This is achieved by:

1. **Tracking created entity IDs** - Each test tracks the IDs of entities it creates
2. **Targeted deletion in TestCleanup** - Only deletes the specific entities tracked

### Example Pattern

```csharp
[TestClass]
public class MyDomainFacadeTests
{
    private DomainFacade _domainFacade = null!;
    private readonly List<Guid> _createdEntityIds = new();

    [TestInitialize]
    public void TestInitialize()
    {
        _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());
        _createdEntityIds.Clear();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        try
        {
            // Delete ONLY entities created during this test
            foreach (var id in _createdEntityIds)
            {
                await _domainFacade.DeleteEntity(id);
            }
        }
        finally
        {
            _domainFacade?.Dispose();
        }
    }

    [TestMethod]
    public async Task CreateEntity_ValidData_ReturnsEntity()
    {
        var entity = await _domainFacade.CreateEntity(new Entity { /* ... */ });
        _createdEntityIds.Add(entity.Id); // Track for cleanup
        
        Assert.IsNotNull(entity);
    }
}
```

## Manual Cleanup

The `ManualCleanup.cs` class provides **manual** cleanup utilities for:
- Cleaning up orphaned test data after test failures
- Resetting the test database during development

**DO NOT** use `ManualCleanup` or `TestDataCleanup` in regular `TestInitialize`/`TestCleanup` methods.

### Running Manual Cleanup

1. Open Test Explorer in your IDE
2. Find the `ManualCleanup` test class
3. Run `CleanupDatabase_RemoveAllTestData` to clean orphaned data
4. Run `VerifyDatabase_NoTestDataRemains` to verify cleanup

## Files

- `ManualCleanup.cs` - Manual test methods for emergency cleanup
- `TestDataCleanup.cs` - Static utility for pattern-based cleanup (manual use only)
