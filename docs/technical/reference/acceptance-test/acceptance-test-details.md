# Acceptance Testing Standards - Detailed Reference

> **Note**: For a quick summary of testing principles, see [testing-standards.mdc](../../../.cursor/rules/acceptance-test-rules/testing-standards.mdc).

## Overview

This document provides comprehensive details on acceptance testing standards for the Platform. It covers test double implementations, database cleanup strategies, test data patterns, and anti-patterns to avoid.

---

## DomainFacade-Only Testing

**PHILOSOPHY**: All acceptance tests MUST ONLY interact with the `DomainFacade` interface. This keeps the underlying implementation black-boxed and easily refactorable.

### DO: Test Through DomainFacade Only

```csharp
[TestClass]
public class CustomerFeatureTests
{
    private DomainFacade _domainFacade;

    [TestMethod]
    public async Task CreateCustomer_ValidData_ReturnsValidId()
    {
        // ✅ CORRECT: Test through DomainFacade only
        var customer = new Customer { FirstName = "TestJohn", Email = "john@example.com" };
        var result = await _domainFacade.CreateCustomer(customer);
        
        Assert.IsTrue(result.IsSuccess);
        Assert.AreNotEqual(Guid.Empty, result.Value);
    }
}
```

### DON'T: Test Individual Classes

```csharp
[TestClass]
public class CustomerManagerTests // ❌ WRONG: Don't test managers directly
{
    [TestMethod]
    public async Task CustomerManager_CreateCustomer_CallsDataFacade()
    {
        // ❌ WRONG: Testing internal implementation
        var manager = new CustomerManager(serviceLocator);
        var result = await manager.CreateCustomer(customer);
        // This breaks encapsulation and makes refactoring difficult
    }
}

[TestClass] 
public class CustomerDataManagerTests // ❌ WRONG: Don't test data managers
{
    [TestMethod]
    public async Task CustomerDataManager_Insert_ExecutesCorrectSQL()
    {
        // ❌ WRONG: Testing internal data access implementation
        var dataManager = new CustomerDataManager(connectionString);
        // This couples tests to implementation details
    }
}
```

### Rationale for DomainFacade-Only Testing

1. **Black Box Testing**: Internal implementation can be refactored without breaking tests
2. **Behavioral Focus**: Tests verify business behavior, not implementation details
3. **Maintainability**: Fewer tests to maintain, focused on what matters to users
4. **Refactoring Safety**: Can restructure internal classes without test changes
5. **Integration Confidence**: Tests verify the complete feature works end-to-end

---

## Test Double Types

This project uses **custom test doubles** instead of external mocking frameworks (like Moq or NSubstitute) to maintain clean dependencies and full control over test behavior.

### Stubs: Provide Canned Answers to Calls

```csharp
internal class StubEmailService : IEmailService
{
    public bool SendEmailCalled { get; private set; }
    public string LastEmailSent { get; private set; }
    
    public Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        SendEmailCalled = true;
        LastEmailSent = $"{to}: {subject}";
        return Task.FromResult(true);
    }
}
```

### Fakes: Working Implementations with Shortcuts

```csharp
internal class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = new();
    
    public Task<Customer> GetByIdAsync(Guid id)
    {
        return Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));
    }
    
    public Task<Guid> CreateAsync(Customer customer)
    {
        customer.Id = Guid.NewGuid();
        _customers.Add(customer);
        return Task.FromResult(customer.Id);
    }
}
```

### InternalsVisibleTo Pattern

- **Assembly Attribute**: `[assembly: InternalsVisibleTo("Orchestrator.AcceptanceTests")]` in `Orchestrator.DomainLayer`
- **Purpose**: Allows tests to access internal constructors and methods for test double creation
- **Scope**: Only for Orchestrator.AcceptanceTests, not for production code

---

## Database Test Cleanup Strategy

### Enhanced Test Cleanup Pattern

- **Database-Level Cleanup**: Use direct SQL for efficient test data removal
- **Pattern-Based Identification**: Identify test data by consistent patterns
- **Complete Isolation**: Clean up before AND after each test
- **Robust Error Handling**: Cleanup failures don't break tests

### Required Test Structure

```csharp
[TestClass]
public class CustomerManagerTests
{
    private DomainFacade _domainFacade;

    [TestInitialize]
    public void TestInitialize()
    {
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

    private void CleanupAllTestData()
    {
        // Database-level cleanup using pattern-based identification
        var deleteSql = @"
            DELETE FROM customers 
            WHERE email LIKE '%@example.com' 
               OR email LIKE '%@test.com'
               OR first_name IN ('John', 'Jane', 'Bob', 'Alice', 'Test')
               OR first_name LIKE 'Test%'
               OR first_name LIKE 'Search%'";
        
        // Execute cleanup SQL
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Execute(deleteSql);
    }
}
```

### Performance Considerations

- **Single SQL Operation**: Use one DELETE statement with multiple conditions
- **Avoid Multiple API Calls**: Don't delete through domain layer in cleanup
- **Pattern-Based**: Use LIKE and IN clauses for efficient pattern matching

### Test Isolation

- **Complete Data Isolation**: Each test starts with clean slate
- **No Test Dependencies**: Tests can run in any order
- **Parallel Execution**: Tests don't interfere with each other

---

## Test Data Patterns

### Consistent Test Data Identification

| Pattern Type | Examples |
|--------------|----------|
| Email Domains | `@example.com`, `@test.com`, `@unittest.com` |
| Name Prefixes | `Test`, `Search`, `Pagination`, `Validation` |
| Common Test Names | `John`, `Jane`, `Bob`, `Alice` |
| Status Values | `TestStatus`, `ValidationTest` |

### Test Data Examples

```csharp
// ✅ Good test data - easily identifiable
var testCustomer = new Customer
{
    FirstName = "TestJohn",
    LastName = "TestDoe", 
    Email = "john.doe@example.com"
};

// ✅ Good test data - pattern-based
var searchCustomer = new Customer
{
    FirstName = "SearchCustomer",
    LastName = "ForTesting",
    Email = "search.test@unittest.com"
};
```

---

## Test Assertion Best Practices

### Always Include Error Messages

```csharp
// ✅ DO: Include descriptive error messages
Assert.IsNotNull(result, $"Expected customer to be created, but got null. Error: {result?.Error?.Message}");
Assert.AreEqual(expectedEmail, result.Email, $"Expected email {expectedEmail}, but got {result.Email}");

// ❌ DON'T: Silent assertions
Assert.IsNotNull(result);
Assert.AreEqual(expectedEmail, result.Email);
```

### Log Important Information

```csharp
[TestMethod]
public async Task CreateCustomer_ValidData_ReturnsValidId()
{
    // Arrange
    var customer = new Customer { FirstName = "TestJohn", Email = "john@example.com" };
    
    // Act
    var result = await _domainFacade.CreateCustomer(customer);
    
    // Assert
    Console.WriteLine($"Customer created with ID: {result.Value}");
    Assert.IsTrue(result.IsSuccess, $"Expected success, but got error: {result.Error?.Message}");
    Assert.AreNotEqual(Guid.Empty, result.Value, "Expected a valid Guid for customer ID");
}
```

---

## Test Organization

### File Structure

```
Orchestrator.AcceptanceTests/
├── DomainLayer/
│   ├── CustomerFeatureTests.cs      // ✅ Feature-based test files
│   ├── OrderFeatureTests.cs         // ✅ Test complete features
│   └── DomainFacadeGeneralTests.cs  // ✅ General DomainFacade tests
├── TestDoubles/
│   ├── StubEmailService.cs          // ✅ Custom test doubles only
│   ├── FakePaymentGateway.cs        // ✅ For external dependencies
│   └── TestDataBuilder.cs           // ✅ Helper for test data creation
└── ServiceLocator/
    └── ServiceLocatorForAcceptanceTesting.cs
```

### Test Naming Convention

| Element | Convention | Example |
|---------|------------|---------|
| Class | `{FeatureName}FeatureTests.cs` | `CustomerFeatureTests.cs` |
| Method | `{FeatureName}_{Scenario}_{ExpectedResult}` | `CreateCustomer_ValidData_ReturnsValidId` |

**Method Examples**:
- `CreateCustomer_ValidData_ReturnsValidId`
- `CreateCustomer_DuplicateEmail_ThrowsBusinessException`
- `SearchCustomers_WithPagination_ReturnsCorrectPage`

### What NOT to Create

- ❌ `CustomerManagerTests.cs` - Don't test managers directly
- ❌ `CustomerDataManagerTests.cs` - Don't test data managers directly  
- ❌ `CustomerValidatorTests.cs` - Don't test validators directly
- ❌ `DataFacadeTests.cs` - Don't test data facade directly

---

## Anti-Patterns to Avoid

### External Mocking Frameworks

```csharp
// ❌ DON'T do this
var mockEmailService = new Mock<IEmailService>();
mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(true);
```

### Production Dependencies in Tests

```csharp
// ❌ DON'T do this - no external services in tests
var realEmailService = new SmtpEmailService();
var realPaymentGateway = new StripePaymentGateway();
```

### Testing Internal Classes

```csharp
// ❌ DON'T do this - testing internal implementation
[TestClass]
public class CustomerManagerTests
{
    [TestMethod]
    public async Task CreateCustomer_CallsDataFacade()
    {
        var manager = new CustomerManager(serviceLocator);
        // This breaks the black box principle
    }
}

// ❌ DON'T do this - testing data layer directly
[TestClass] 
public class CustomerDataManagerTests
{
    [TestMethod]
    public async Task Insert_ExecutesSQL()
    {
        var dataManager = new CustomerDataManager(connectionString);
        // This couples tests to implementation details
    }
}
```

### Incomplete Cleanup

```csharp
// ❌ DON'T do this - incomplete cleanup
[TestCleanup]
public void TestCleanup()
{
    // Only cleaning up some data
    _domainFacade.DeleteCustomer(customerId);
    // Missing other test data cleanup
}
```

### Silent Test Failures

```csharp
// ❌ DON'T do this - no error context
Assert.IsTrue(result.IsSuccess);
Assert.AreEqual(expected, actual);
```

---

## Template Usage

- **Test Template**: Use `docs/templates/create/DomainFacadeTests.Base.hbs` for all new test files
- **Test Double Templates**: Create standardized templates for common test double patterns
- **Cleanup Templates**: Standardize cleanup patterns across all test classes

---

**Remember**: Tests are first-class citizens in this architecture. They should be as well-crafted and maintainable as production code.

