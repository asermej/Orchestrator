using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for User operations using real DomainFacade
/// 
/// TEST APPROACH:
/// - Uses real DomainFacade instances for acceptance tests
/// - Tests the actual integration between layers
/// - No external mocking frameworks used
/// - Tests clean up ONLY the data they create (tracked by ID)
/// </summary>
[TestClass]
public class DomainFacadeTestsUser
{
    private DomainFacade _domainFacade = null!;
    
    // Track entities created during test for targeted cleanup
    private readonly List<Guid> _createdUserIds = new();

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        
        // Clear tracking list
        _createdUserIds.Clear();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        try
        {
            // Delete ONLY the users created during this test
            foreach (var userId in _createdUserIds)
            {
                try
                {
                    await _domainFacade.DeleteUser(userId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete user {userId}: {ex.Message}");
                }
            }
        }
        finally
        {
            _domainFacade?.Dispose();
        }
    }

    /// <summary>
    /// Helper method to create test User and track it for cleanup
    /// </summary>
    private async Task<User> CreateTestUserAsync(string suffix = "")
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var randomDigits = DateTime.Now.Ticks.ToString()[..7];
        var user = new User
        {
            FirstName = $"Test{suffix}",
            LastName = $"User{suffix}",
            Email = $"test_{uniqueId}@example.com",
            Phone = $"+1555{randomDigits}"
        };

        var result = await _domainFacade.CreateUser(user);
        Assert.IsNotNull(result, "Failed to create test User");
        
        // Track for cleanup
        _createdUserIds.Add(result.Id);
        
        return result;
    }

    [TestMethod]
    public async Task CreateUser_ValidData_ReturnsCreatedUser()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = $"john_{uniqueId}@example.com",
            Phone = "+15551230001"
        };

        // Act
        var result = await _domainFacade.CreateUser(user);
        _createdUserIds.Add(result.Id); // Track for cleanup

        // Assert
        Assert.IsNotNull(result, "Create should return a User");
        Assert.AreNotEqual(Guid.Empty, result.Id, "User should have a valid ID");
        Assert.AreEqual(user.FirstName, result.FirstName, "FirstName should match");
        Assert.AreEqual(user.LastName, result.LastName, "LastName should match");
        Assert.AreEqual(user.Email, result.Email, "Email should match");
        Assert.AreEqual(user.Phone, result.Phone, "Phone should match");
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
            _domainFacade.CreateUser(user), 
            "Should throw validation exception for empty required fields");
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
            _domainFacade.CreateUser(secondUser), 
            "Should throw duplicate email exception");
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
        Assert.AreEqual(createdUser.Id, result.Id, "ID should match");
        Assert.AreEqual(createdUser.FirstName, result.FirstName, "FirstName should match");
        Assert.AreEqual(createdUser.LastName, result.LastName, "LastName should match");
        Assert.AreEqual(createdUser.Email, result.Email, "Email should match");
        Assert.AreEqual(createdUser.Phone, result.Phone, "Phone should match");
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
        // Arrange - Create some test Users with unique lastName pattern
        var uniqueLastName = $"SearchTest{Guid.NewGuid():N}"[..20];
        var uniqueId1 = Guid.NewGuid().ToString("N")[..8];
        var uniqueId2 = Guid.NewGuid().ToString("N")[..8];
        
        var user1 = new User
        {
            FirstName = "Search1",
            LastName = uniqueLastName,
            Email = $"search1_{uniqueId1}@example.com",
            Phone = "+15551111111"
        };
        var created1 = await _domainFacade.CreateUser(user1);
        _createdUserIds.Add(created1.Id);
        
        var user2 = new User
        {
            FirstName = "Search2",
            LastName = uniqueLastName,
            Email = $"search2_{uniqueId2}@example.com",
            Phone = "+15552222222"
        };
        var created2 = await _domainFacade.CreateUser(user2);
        _createdUserIds.Add(created2.Id);

        // Act - Search by lastName pattern (SearchUsers searches by phone, email, lastName)
        var result = await _domainFacade.SearchUsers(null, null, uniqueLastName, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.AreEqual(2, result.TotalCount, $"Should find exactly 2 Users with lastName '{uniqueLastName}'");
        Assert.AreEqual(2, result.Items.Count(), "Should return 2 items");
    }

    [TestMethod]
    public async Task SearchUsers_NoResults_ReturnsEmptyList()
    {
        // Arrange - Use a unique search term that won't match anything
        var uniqueSearchTerm = $"NonExistent{Guid.NewGuid():N}"[..20];
        
        // Act - Search by lastName (SearchUsers searches by phone, email, lastName)
        var result = await _domainFacade.SearchUsers(null, null, uniqueSearchTerm, 1, 10);

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
        var newUniqueId = Guid.NewGuid().ToString("N")[..8];
        
        // Modify the User
        user.FirstName = "Updated";
        user.LastName = "Name";
        user.Email = $"updated_{newUniqueId}@example.com";
        user.Phone = "+15559999999";

        // Act
        var result = await _domainFacade.UpdateUser(user);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated User");
        Assert.AreEqual(user.FirstName, result.FirstName, "FirstName should be updated");
        Assert.AreEqual(user.LastName, result.LastName, "LastName should be updated");
        Assert.AreEqual(user.Email, result.Email, "Email should be updated");
        Assert.AreEqual(user.Phone, result.Phone, "Phone should be updated");
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
            "Should throw validation exception for empty required fields");
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
        var userId = user.Id;
        
        // Remove from tracking since we're testing delete
        _createdUserIds.Remove(userId);

        // Act
        var result = await _domainFacade.DeleteUser(userId);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing User");
        var deletedUser = await _domainFacade.GetUserById(userId);
        Assert.IsNull(deletedUser, "Should not find deleted User");
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
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var uniqueLastName = $"Lifecycle{uniqueId}"[..15];
        var user = new User
        {
            FirstName = "Lifecycle",
            LastName = uniqueLastName,
            Email = $"lifecycle_{uniqueId}@example.com",
            Phone = "+15551234567"
        };
        var created = await _domainFacade.CreateUser(user);
        Assert.IsNotNull(created, "User should be created");
        var createdId = created.Id;
        // Don't add to tracking - we'll delete it as part of the test
        
        // Get
        var retrievedUser = await _domainFacade.GetUserById(createdId);
        Assert.IsNotNull(retrievedUser, "Should retrieve created User");
        Assert.AreEqual(createdId, retrievedUser.Id, "Retrieved ID should match");
        
        // Update
        var updatedUniqueId = Guid.NewGuid().ToString("N")[..8];
        var updatedLastName = $"Updated{updatedUniqueId}"[..15];
        retrievedUser.FirstName = "Updated";
        retrievedUser.LastName = updatedLastName;
        retrievedUser.Email = $"updated_{updatedUniqueId}@example.com";
        retrievedUser.Phone = "+15559999999";
        
        var updatedUser = await _domainFacade.UpdateUser(retrievedUser);
        Assert.IsNotNull(updatedUser, "Should update User");
        Assert.AreEqual(updatedLastName, updatedUser.LastName, "LastName should be updated");
        
        // Search - Search by updated lastName (SearchUsers searches by phone, email, lastName)
        var searchResult = await _domainFacade.SearchUsers(null, null, updatedLastName, 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.AreEqual(1, searchResult.TotalCount, "Should find exactly 1 updated User");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteUser(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete User");
        
        // Verify deletion
        var deletedUser = await _domainFacade.GetUserById(createdId);
        Assert.IsNull(deletedUser, "Should not find deleted User");
    }
}
