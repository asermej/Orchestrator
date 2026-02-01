# Test Data Cleanup Utility

## Overview

The `TestDataCleanup` utility provides centralized, comprehensive test data cleanup for all acceptance tests. This ensures that no test data is left in the database after test runs, preventing data accumulation and test interference.

## Problem Statement

Previously, each test file had its own cleanup method with inconsistent patterns:
- Some tests looked for `'Test%'` patterns
- Others looked for `'TestPersona%'` patterns  
- Cleanup patterns didn't cover all test data variations
- When tests failed or were interrupted, cleanup might not run
- Test data created by one test file wasn't cleaned up by another

**Example of leftover data:**
```
TestPersona638981699624283760
```

This persona was created by a test but never cleaned up because:
1. The test that created it may have failed before cleanup ran
2. Other test files that ran afterward only cleaned up their own patterns
3. The cleanup pattern was too specific and didn't match all variations

## Solution

The `TestDataCleanup` utility provides:

1. **Centralized Cleanup**: Single source of truth for all test data patterns
2. **Comprehensive Patterns**: Catches ALL test data variations across all entities
3. **Correct Order**: Respects foreign key constraints by deleting in proper order
4. **Used Everywhere**: ALL test files now use the same cleanup utility

## Usage

### In Test Files

Every test file should use `TestDataCleanup` in both `TestInitialize` and `TestCleanup`:

```csharp
using Platform.AcceptanceTests.TestUtilities;

[TestClass]
public class DomainFacadeTestsYourFeature
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
        
        // If your tests create training files:
        TestDataCleanup.CleanupTestTrainingFiles();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
            TestDataCleanup.CleanupAllTestData(_connectionString);
            TestDataCleanup.CleanupTestTrainingFiles(); // If applicable
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
        }
        finally
        {
            _domainFacade?.Dispose();
        }
    }
}
```

### Methods

#### `CleanupAllTestData(string connectionString)`

Comprehensive cleanup of ALL test data across all entities. Automatically handles:
- Chat messages
- Chats  
- Persona topics
- Persona categories
- Topic tags
- Personas
- Topics
- Tags
- Categories (test data only)
- Users (test data only)
- Images (test data only)

**Cleanup Order:** Deletes in reverse order of foreign key dependencies to avoid constraint violations.

**Test Data Patterns:** Identifies test data by:
- Prefixes: `Test%`, `test%`, `Search%`, `Update%`, `Delete%`, `Lifecycle%`
- Email domains: `@example.com`, `@test.com`, `@unittest.com`
- Specific test names: `TestPersona`, `TestChat`, `TestCategory`, etc.
- Content patterns: Test-specific strings in content fields

#### `CleanupTestTrainingFiles()`

Cleans up test training files that may have been created during persona tests.

**Usage:** Call this in addition to `CleanupAllTestData()` if your tests create training files.

#### `AggressiveCleanup(string connectionString)`

More aggressive cleanup for database reset scenarios. Use with caution - removes ANY data that looks like test data using permissive patterns.

**Usage:** Only use for manual database cleanup, not in regular tests.

## Test Data Naming Conventions

To ensure your test data is properly cleaned up, follow these naming conventions:

### Personas
- `display_name` should start with: `Test`, `Search`, `Update`, `Delete`, `Lifecycle`
- Examples: `TestPersona638981699624283760`, `SearchPersona123`

### Chats
- `title` should start with: `Test`, `Search`, `Update`, `Delete`, `Lifecycle`
- Examples: `TestChat about using the Force 638981699624283760`

### Topics, Tags, Categories
- `name` should start with: `Test`, `Search`, `Update`, `Delete`, `Lifecycle`
- Examples: `TestTopic123`, `TestTag456`, `TestCategory789`

### Users
- `email` should use test domains: `@example.com`, `@test.com`, `@unittest.com`
- `first_name` or `last_name` should start with: `Test`, `Search`, etc.
- Examples: `john.doe@example.com`, `TestUser123`

### Chat Messages
- `content` should start with: `Test`, or contain: `test`, `simulated`
- Examples: `Test message 638981699624283760`

## Why Cleanup in Both TestInitialize and TestCleanup?

**TestInitialize (Before):**
- Ensures a clean slate before the test starts
- Handles leftover data from failed previous tests
- Guarantees test isolation

**TestCleanup (After):**
- Cleans up data created by the current test
- Runs even if test passes
- Prevents accumulation of successful test data

**Together:** These provide 100% test isolation and prevent any test data accumulation.

## Troubleshooting

### Test data still not being cleaned up?

1. **Check your naming conventions**: Ensure test data follows the patterns above
2. **Add to TestDataCleanup**: If you're using a new pattern, add it to `TestDataCleanup.CleanupAllTestData()`
3. **Verify test is using TestDataCleanup**: Check that both TestInitialize and TestCleanup call the utility
4. **Check for exceptions**: Look for cleanup warnings in test output

### How to add a new test data pattern?

Edit `TestDataCleanup.cs` and add your pattern to the appropriate DELETE statement:

```csharp
// In CleanupAllTestData(), add to the relevant section:
var personasDeleted = connection.Execute(@"
    DELETE FROM personas 
    WHERE display_name LIKE 'Test%'
       OR display_name LIKE 'YourNewPattern%'  // Add your pattern here
       ...
```

### Manual cleanup needed?

If you need to manually clean up the database:

```csharp
// Run this once to aggressively clean up all test data
var connectionString = "your_connection_string";
TestDataCleanup.AggressiveCleanup(connectionString);
```

## Benefits

✅ **Prevents test data accumulation**: No more leftover test data  
✅ **Ensures test isolation**: Each test starts with a clean slate  
✅ **Consistent across all tests**: Same cleanup logic everywhere  
✅ **Handles foreign keys correctly**: Deletes in proper order  
✅ **Catches failed test cleanup**: Runs before AND after each test  
✅ **Easy to maintain**: One place to update cleanup patterns  
✅ **Comprehensive patterns**: Covers all test data variations  

## Migration Notes

All test files have been migrated to use this centralized utility. The old individual `CleanupAllTestData()` methods in each test file have been removed.

**Before:**
```csharp
private void CleanupAllTestData()
{
    // Each test file had its own cleanup logic
    var deleteSql = "DELETE FROM personas WHERE display_name LIKE 'Test%'";
    // ...
}
```

**After:**
```csharp
// All tests now use the centralized utility
TestDataCleanup.CleanupAllTestData(_connectionString);
```

## Future Enhancements

Potential improvements:
- Add metrics/logging for cleanup operations
- Add configurable patterns via configuration file
- Add database transaction-based test isolation (if needed)
- Add automatic pattern detection from test assertions

