using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for User operations using real DomainFacade and real DataFacade with data cleanup
/// 
/// TEST APPROACH:
/// - Uses real DomainFacade and DataFacade instances for acceptance tests
/// - Tests the actual integration between layers
/// - No external mocking frameworks used
/// - ServiceLocatorForAcceptanceTesting provides real implementations
/// - Tests clean up their own data to ensure complete independence
/// 
/// ENHANCED CLEANUP APPROACH:
/// - Database-level cleanup before AND after each test for complete isolation
/// - Identifies test data by patterns (email domains, specific names)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsUser
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsUser()
    {
        _domainFacade = null!;
        _connectionString = null!;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
            TestDataCleanup.CleanupAllTestData(_connectionString);
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

    /// <summary>
    /// Helper method to create test User with unique data and track it for cleanup
    /// </summary>
    private async Task<User> CreateTestUserAsync(string suffix = "")
    {
        var user = new User
        {
            FirstName = $"Test{suffix}{DateTime.Now.Ticks}",
            LastName = $"User{suffix}{DateTime.Now.Ticks}",
            Email = $"test{suffix}{DateTime.Now.Ticks}@example.com",
            Phone = $"+1555{DateTime.Now.Ticks.ToString().Substring(0, 7)}"
        };

        var result = await _domainFacade.CreateUser(user);
        Assert.IsNotNull(result, "Failed to create test User");
        return result;
    }

    /// <summary>
    /// Simple helper method to create test User with basic data
    /// </summary>
    private async Task<User> CreateTestUser()
    {
        var user = new User
        {
            FirstName = $"Test{DateTime.Now.Ticks}",
            LastName = $"User{DateTime.Now.Ticks}",
            Email = $"test{DateTime.Now.Ticks}@example.com",
            Phone = "+15551234567"
        };

        var result = await _domainFacade.CreateUser(user);
        return result;
    }

    [TestMethod]
    public async Task CreateUser_ValidData_ReturnsCreatedUser()
    {
        // Arrange
        var user = new User
        {
            FirstName = $"firstname{DateTime.Now.Ticks}",
            LastName = $"lastname{DateTime.Now.Ticks}",
            Email = $"email{DateTime.Now.Ticks}@example.com",
            Phone = $"+1555{DateTime.Now.Ticks.ToString().Substring(0, 7)}"
        };

        // Act
        var result = await _domainFacade.CreateUser(user);

        // Assert
        Assert.IsNotNull(result, "Create should return a User");
        Assert.AreNotEqual(Guid.Empty, result.Id, "User should have a valid ID");
        Assert.AreEqual(user.FirstName, result.FirstName);
        Assert.AreEqual(user.LastName, result.LastName);
        Assert.AreEqual(user.Email, result.Email);
        Assert.AreEqual(user.Phone, result.Phone);
        
        Console.WriteLine($"User created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateUser_InvalidData_ThrowsValidationException()
    {
        // Arrange - User with empty required fields
        var user = new User
        {
            FirstName = "", // Required field empty
            LastName = "", // Required field empty
            Email = "", // Required field empty
            Phone = "" // Optional field
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserValidationException>(() => 
            _domainFacade.CreateUser(user), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task CreateUser_DuplicateEmail_ThrowsDuplicateException()
    {
        // Arrange - Create first user
        var firstUser = await CreateTestUserAsync("First");
        
        // Create second user with same email
        var secondUser = new User
        {
            FirstName = "Different",
            LastName = "User",
            Email = firstUser.Email, // Same email
            Phone = "+15559876543"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserDuplicateEmailException>(() => 
            _domainFacade.CreateUser(secondUser), "Should throw duplicate email exception");
    }

    [TestMethod]
    public async Task GetUserById_ExistingId_ReturnsUser()
    {
        // Arrange - Create a test User
        var createdUser = await CreateTestUserAsync();

        // Act
        var result = await _domainFacade.GetUserById(createdUser.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return User with ID: {createdUser.Id}");
        Assert.AreEqual(createdUser.Id, result.Id);
        Assert.AreEqual(createdUser.FirstName, result.FirstName);
        Assert.AreEqual(createdUser.LastName, result.LastName);
        Assert.AreEqual(createdUser.Email, result.Email);
        Assert.AreEqual(createdUser.Phone, result.Phone);
    }

    [TestMethod]
    public async Task GetUserById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetUserById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchUsers_WithResults_ReturnsPaginatedList()
    {
        // Arrange - Create some test Users
        var user1 = await CreateTestUserAsync("Test1");
        var user2 = await CreateTestUserAsync("Test2");

        // Act - Search by firstName pattern
        var result = await _domainFacade.SearchUsers(null, null, "Test", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 Users, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, $"Should return at least 2 items, returned {result.Items.Count()}");
        
        Console.WriteLine($"Search returned {result.TotalCount} total Users");
    }

    [TestMethod]
    public async Task SearchUsers_NoResults_ReturnsEmptyList()
    {
        // Act
        var result = await _domainFacade.SearchUsers("NonExistentSearchTerm", "NonExistentSearchTerm", "NonExistentSearchTerm", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent search term");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateUser_ValidData_UpdatesSuccessfully()
    {
        // Arrange - Create a test User
        var user = await CreateTestUserAsync();
        
        // Modify the User
        user.FirstName = $"Updated{DateTime.Now.Ticks}";
        user.LastName = $"UpdatedLast{DateTime.Now.Ticks}";
        user.Email = $"updated{DateTime.Now.Ticks}@example.com";
        user.Phone = "+15559999999";

        // Act
        var result = await _domainFacade.UpdateUser(user);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated User");
        Assert.AreEqual(user.FirstName, result.FirstName);
        Assert.AreEqual(user.LastName, result.LastName);
        Assert.AreEqual(user.Email, result.Email);
        Assert.AreEqual(user.Phone, result.Phone);
        
        Console.WriteLine($"User updated successfully");
    }

    [TestMethod]
    public async Task UpdateUser_InvalidData_ThrowsValidationException()
    {
        // Arrange - Create a test User
        var user = await CreateTestUserAsync();
        
        // Set invalid data
        user.FirstName = ""; // Invalid empty value
        user.LastName = ""; // Invalid empty value
        user.Email = ""; // Invalid empty value

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserValidationException>(() => 
            _domainFacade.UpdateUser(user), 
            "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task UpdateUser_DuplicateEmail_ThrowsDuplicateException()
    {
        // Arrange - Create two test Users
        var user1 = await CreateTestUserAsync("User1");
        var user2 = await CreateTestUserAsync("User2");
        
        // Try to update user2 with user1's email
        user2.Email = user1.Email;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserDuplicateEmailException>(() => 
            _domainFacade.UpdateUser(user2), 
            "Should throw duplicate email exception");
    }

    [TestMethod]
    public async Task DeleteUser_ExistingId_DeletesSuccessfully()
    {
        // Arrange - Create a test User
        var user = await CreateTestUserAsync();

        // Act
        var result = await _domainFacade.DeleteUser(user.Id);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing User");
        var deletedUser = await _domainFacade.GetUserById(user.Id);
        Assert.IsNull(deletedUser, "Should not find deleted User");
        
        Console.WriteLine($"User deleted successfully");
    }

    [TestMethod]
    public async Task DeleteUser_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteUser(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task UserLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var user = await CreateTestUserAsync("Lifecycle");
        Assert.IsNotNull(user, "User should be created");
        var createdId = user.Id;
        
        // Get
        var retrievedUser = await _domainFacade.GetUserById(createdId);
        Assert.IsNotNull(retrievedUser, "Should retrieve created User");
        Assert.AreEqual(createdId, retrievedUser.Id);
        
        // Update
        retrievedUser.FirstName = $"Updated{DateTime.Now.Ticks}";
        retrievedUser.LastName = $"UpdatedLast{DateTime.Now.Ticks}";
        retrievedUser.Email = $"updated{DateTime.Now.Ticks}@example.com";
        retrievedUser.Phone = "+15559999999";
        
        var updatedUser = await _domainFacade.UpdateUser(retrievedUser);
        Assert.IsNotNull(updatedUser, "Should update User");
        
        // Search - Search by lastName pattern
        var searchResult = await _domainFacade.SearchUsers(null, null, "Updated", 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated User");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteUser(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete User");
        
        // Verify deletion
        var deletedUser = await _domainFacade.GetUserById(createdId);
        Assert.IsNull(deletedUser, "Should not find deleted User");
        
        Console.WriteLine("User lifecycle test completed successfully");
    }

    /// <summary>
    /// Optional: Call this method to completely reset the database after all tests
    /// This can be useful for integration test scenarios or when you want a completely clean slate
    /// </summary>
    [TestMethod]
    [TestCategory("DatabaseReset")]
    public void ResetDatabase_RemoveAllTestData()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // More aggressive cleanup that removes ALL potential test data
            var resetSql = @"
                DELETE FROM users 
                WHERE email LIKE '%@example.com' 
                   OR email LIKE '%@test.com'
                   OR email LIKE '%test%'
                   OR first_name LIKE '%Test%'
                   OR first_name LIKE '%test%'
                   OR first_name LIKE '%Search%'
                   OR first_name LIKE '%Update%'
                   OR first_name LIKE '%Delete%'
                   OR first_name LIKE '%Lifecycle%'
                   OR last_name LIKE '%Test%'
                   OR last_name LIKE '%test%';";

            var rowsAffected = connection.Execute(resetSql);
            
            Console.WriteLine($"Database reset: Removed {rowsAffected} test User records");
            Assert.IsTrue(true, $"Successfully reset database - removed {rowsAffected} records");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to reset database: {ex.Message}");
        }
    }

    /// <summary>
    /// Optional: Call this to verify the database is clean after reset
    /// </summary>
    [TestMethod]
    [TestCategory("DatabaseReset")]
    public async Task VerifyDatabaseClean_NoTestDataRemains()
    {
        // Search for any remaining test data
        var result = await _domainFacade.SearchUsers(null, null, "Test", 1, 100);
        
        Assert.IsNotNull(result, "Search should return results object");
        Assert.AreEqual(0, result.TotalCount, $"Database should be clean but found {result.TotalCount} test records");
        Assert.IsFalse(result.Items.Any(), "No test items should remain in database");
        
        Console.WriteLine("✅ Database verification passed - no test data found");
    }
} 