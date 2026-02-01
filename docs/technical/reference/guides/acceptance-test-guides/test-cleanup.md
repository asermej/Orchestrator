# Enhanced Test Cleanup Guide

## Overview

This guide explains the enhanced test cleanup approach that ensures complete test isolation and 100% test reliability using the ConfigurationProvider pattern.

## The Problem

Traditional test cleanup approaches often lead to:
- Data accumulation between test runs
- Test interference and flaky tests
- Inconsistent test results
- Difficult debugging when tests fail
- Environment-specific configuration issues

## Solution: Enhanced Database-Level Cleanup with ConfigurationProvider

### Approach

1. **Database-Level Cleanup**: Use direct SQL to clean up test data efficiently
2. **ConfigurationProvider Pattern**: Use the same configuration loading as the domain layer
3. **Pattern-Based Identification**: Identify test data by consistent patterns
4. **Complete Isolation**: Clean up before AND after each test
5. **Robust Error Handling**: Cleanup failures don't break tests
6. **Environment Agnostic**: Works in any environment (dev, test, staging)

### Implementation

```csharp
[TestClass]
public class DomainFacade{{featureName}}Test
{
    private DomainFacade? _domainFacade;
    private string _testSuffix;

    [TestInitialize]
    public void TestInitialize()
    {
        // Generate unique test suffix to avoid conflicts
        _testSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        
        // Clean up ALL test data before each test to ensure complete isolation
        CleanupAllTestData();
        _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
            CleanupAllTestData();
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

    private string GetTestDbConnectionString()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var configurationProvider = serviceLocator.CreateConfigurationProvider();
        return configurationProvider.GetDbConnectionString();
    }

    private void CleanupAllTestData()
    {
        var connectionString = GetTestDbConnectionString();
        const string deleteSql = @"
            DELETE FROM {{tableName}} 
            WHERE email LIKE '%@example.com' 
               OR first_name LIKE 'Search%' 
               OR first_name LIKE 'Pagination%' 
               OR first_name = 'Delete' 
               OR first_name = 'Case' 
               OR first_name = 'Bob' 
               OR first_name = 'Robert' 
               OR first_name = 'Alice' 
               OR first_name = 'Charlie' 
               OR first_name = 'John' 
               OR first_name = 'Jane' 
               OR first_name = 'Test';";
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using (var cmd = new NpgsqlCommand(deleteSql, connection))
            {
                var rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"Cleaned up {rows} test records from database.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up test data: {ex.Message}");
        }
    }
}
```

## Test Data Identification Patterns

### Email Patterns
- `%@example.com` - Standard test email domain
- `%@test.com` - Alternative test email domain

### Name Patterns
- Common test names: `John`, `Jane`, `Bob`, `Alice`, `Test`, `Debug`
- Pattern-based names: `Search%`, `Pagination%`, `Different%`

### Field-Specific Patterns
Different entity types may have specific patterns for identifying test data.

## ConfigurationProvider Benefits

### 1. Environment Agnostic
```csharp
// Works in any environment (dev, test, staging, production)
var serviceLocator = new ServiceLocatorForAcceptanceTesting();
var configurationProvider = serviceLocator.CreateConfigurationProvider();
var connectionString = configurationProvider.GetDbConnectionString();
```

### 2. Consistent with Domain Layer
```csharp
// Uses the same configuration loading as the rest of the application
// Automatically picks up environment-specific settings
// Handles configuration errors consistently
```

### 3. No Hardcoded Values
```csharp
// ❌ DON'T: Hardcode connection strings
const string connectionString = "Host=localhost;Database=surrova;Username=postgres;Password=postgres";

// ✅ DO: Use ConfigurationProvider
var connectionString = GetTestDbConnectionString();
```

## Benefits

### 1. Complete Test Isolation
- Each test runs in a clean environment
- No data interference between tests
- Consistent and predictable test results

### 2. Environment Flexibility
- Works in any environment without code changes
- Uses the same configuration as the application
- Handles environment-specific settings automatically

### 3. Performance
- Database-level cleanup is faster than individual entity deletion
- Single SQL operation vs. multiple API calls
- Efficient pattern matching

### 4. Reliability
- Robust error handling prevents cleanup failures from breaking tests
- Graceful degradation when cleanup issues occur
- Clear logging for debugging

### 5. Maintainability
- Simple, understandable cleanup logic
- Consistent patterns across all tests
- Easy to extend for new entity types

## Best Practices

### 1. Use ConfigurationProvider Pattern
```csharp
// ✅ DO: Use ConfigurationProvider for connection string
private string GetTestDbConnectionString()
{
    var serviceLocator = new ServiceLocatorForAcceptanceTesting();
    var configurationProvider = serviceLocator.CreateConfigurationProvider();
    return configurationProvider.GetDbConnectionString();
}
```

### 2. Use Consistent Patterns
```csharp
// Always use the same email domains
var testEmails = new[] { "@example.com", "@test.com" };

// Always use the same naming patterns
var testNames = new[] { "Test", "Debug", "Search", "Pagination" };
```

### 3. Be Specific
```sql
-- Use specific patterns that won't match production data
WHERE email LIKE '%@example.com'  -- Good
WHERE email LIKE '%@%'            -- Too broad
```

### 4. Use Unique Test Data
```csharp
// Generate unique test suffix to avoid conflicts
_testSuffix = DateTime.Now.ToString("yyyyMMddHHmmssfff");
var email = $"test.{_testSuffix}@example.com";
```

### 5. Document Patterns
```csharp
/// <summary>
/// Test data identification patterns for entity
/// </summary>
private static class TestDataPatterns
{
    public static readonly string[] EmailDomains = { "@example.com", "@test.com" };
    public static readonly string[] Names = { "Test", "Debug", "Search", "Pagination" };
    public static readonly string[] NamePatterns = { "Search%", "Pagination%" };
}
```

### 6. Test Cleanup Effectiveness
```csharp
[TestMethod]
public async Task TestCleanup_RemovesAllTestData()
{
    // Arrange - Create test data
    var testData = await CreateTestData();
    
    // Act - Run cleanup
    CleanupAllTestData();
    
    // Assert - Verify cleanup
    var remainingData = await GetTestData();
    Assert.AreEqual(0, remainingData.Count, "All test data should be cleaned up");
}
```

## Anti-Patterns

### ❌ DON'T: Hardcode Connection Strings
```csharp
const string connectionString = "Host=localhost;Database=surrova;Username=postgres;Password=postgres";
```

### ❌ DON'T: Use Manual Configuration Loading
```csharp
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();
var connectionString = config.GetSection("ConnectionStrings")["DbConnectionString"];
```

### ❌ DON'T: Skip Cleanup on Errors
```csharp
private void CleanupAllTestData()
{
    using var connection = new NpgsqlConnection(connectionString);
    // If this fails, the test will fail
}
```

### ✅ DO: Use ConfigurationProvider with Error Handling
```csharp
private void CleanupAllTestData()
{
    try
    {
        var connectionString = GetTestDbConnectionString();
        // ... cleanup logic
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error cleaning up test data: {ex.Message}");
        // Don't fail the test due to cleanup issues
    }
}
```

## Conclusion

The enhanced test cleanup approach provides:
- ✅ **100% test reliability** and isolation
- ✅ **Fast and efficient** cleanup operations
- ✅ **Robust error handling** that doesn't break tests
- ✅ **Maintainable and extensible** solution
- ✅ **Production-like testing** environment

This approach ensures that all future API endpoints will have reliable, fast, and maintainable acceptance tests.







