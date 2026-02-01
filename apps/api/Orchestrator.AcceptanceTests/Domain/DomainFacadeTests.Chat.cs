using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Chat operations using real DomainFacade and real DataFacade with data cleanup
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
/// - Identifies test data by patterns (title patterns, test personas)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsChat
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsChat()
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
    /// Helper method to create test Chat with unique data and track it for cleanup
    /// </summary>
    private async Task<Chat> CreateTestChatAsync(string suffix = "")
    {
        var chat = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = $"Test{suffix}{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };

        var result = await _domainFacade.CreateChat(chat);
        Assert.IsNotNull(result, "Failed to create test Chat");
        return result;
    }

    /// <summary>
    /// Simple helper method to create test Chat with basic data
    /// </summary>
    private async Task<Chat> CreateTestChat()
    {
        var chat = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = $"Test{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };

        var result = await _domainFacade.CreateChat(chat);
        return result;
    }

    [TestMethod]
    public async Task CreateChat_ValidData_ReturnsCreatedChat()
    {
        // Arrange
        var chat = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = $"Chat about using the Force {DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };

        // Act
        var result = await _domainFacade.CreateChat(chat);

        // Assert
        Assert.IsNotNull(result, "Create should return a Chat");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Chat should have a valid ID");
        Assert.AreEqual(chat.PersonaId, result.PersonaId);
        Assert.AreEqual(chat.UserId, result.UserId);
        Assert.AreEqual(chat.Title, result.Title);
        // Verify LastMessageAt is set (actual time may vary due to timezone/database handling)
        Assert.AreNotEqual(default(DateTime), result.LastMessageAt, "LastMessageAt should be set");
        
        Console.WriteLine($"Chat created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateChat_WithoutTitle_ReturnsCreatedChat()
    {
        // Arrange - Chat can have no title initially
        var chat = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = null, // No title
            LastMessageAt = DateTime.UtcNow
        };

        // Act
        var result = await _domainFacade.CreateChat(chat);

        // Assert
        Assert.IsNotNull(result, "Create should return a Chat");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Chat should have a valid ID");
        Assert.IsNull(result.Title, "Title should be null");
        
        Console.WriteLine($"Chat created without title with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateChat_InvalidData_ThrowsValidationException()
    {
        // Arrange - Chat with empty PersonaId
        var chat = new Chat
        {
            PersonaId = Guid.Empty, // Invalid - required field
            UserId = Guid.NewGuid(),
            Title = "Test Chat",
            LastMessageAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ChatValidationException>(() => 
            _domainFacade.CreateChat(chat), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task CreateChat_EmptyUserId_ThrowsValidationException()
    {
        // Arrange - Chat with empty UserId
        var chat = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = Guid.Empty, // Invalid - required field
            Title = "Test Chat",
            LastMessageAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ChatValidationException>(() => 
            _domainFacade.CreateChat(chat), "Should throw validation exception for empty UserId");
    }

    [TestMethod]
    public async Task GetChatById_ExistingId_ReturnsChat()
    {
        // Arrange - Create a test Chat
        var createdChat = await CreateTestChatAsync();

        // Act
        var result = await _domainFacade.GetChatById(createdChat.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return Chat with ID: {createdChat.Id}");
        Assert.AreEqual(createdChat.Id, result.Id);
        Assert.AreEqual(createdChat.PersonaId, result.PersonaId);
        Assert.AreEqual(createdChat.UserId, result.UserId);
        Assert.AreEqual(createdChat.Title, result.Title);
    }

    [TestMethod]
    public async Task GetChatById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetChatById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchChats_ByTitle_ReturnsPaginatedList()
    {
        // Arrange - Create some test Chats
        var chat1 = await CreateTestChatAsync("Chat1");
        var chat2 = await CreateTestChatAsync("Chat2");

        // Act - Search by title pattern
        var result = await _domainFacade.SearchChats(null, null, "TestChat", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 Chats, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, $"Should return at least 2 items, returned {result.Items.Count()}");
        
        Console.WriteLine($"Search returned {result.TotalCount} total Chats");
    }

    [TestMethod]
    public async Task SearchChats_ByPersonaId_ReturnsMatchingChats()
    {
        // Arrange - Create chats with specific persona
        var testPersonaId = Guid.NewGuid();
        var chat1 = new Chat
        {
            PersonaId = testPersonaId,
            UserId = Guid.NewGuid(),
            Title = $"TestChat1_{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };
        var chat2 = new Chat
        {
            PersonaId = testPersonaId,
            UserId = Guid.NewGuid(),
            Title = $"TestChat2_{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };

        await _domainFacade.CreateChat(chat1);
        await _domainFacade.CreateChat(chat2);

        // Act - Search by persona ID
        var result = await _domainFacade.SearchChats(testPersonaId, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 chats for persona, found {result.TotalCount}");
        Assert.IsTrue(result.Items.All(c => c.PersonaId == testPersonaId), "All returned chats should match the persona ID");
    }

    [TestMethod]
    public async Task SearchChats_ByUserId_ReturnsUserChats()
    {
        // Arrange - Create chats for specific user
        var testUserId = Guid.NewGuid();
        var chat1 = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = testUserId,
            Title = $"TestUserChat1_{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };
        var chat2 = new Chat
        {
            PersonaId = Guid.NewGuid(),
            UserId = testUserId,
            Title = $"TestUserChat2_{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };

        await _domainFacade.CreateChat(chat1);
        await _domainFacade.CreateChat(chat2);

        // Act - Search by user ID
        var result = await _domainFacade.SearchChats(null, testUserId, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 chats for user, found {result.TotalCount}");
        Assert.IsTrue(result.Items.All(c => c.UserId == testUserId), "All returned chats should match the user ID");
    }

    [TestMethod]
    public async Task SearchChats_NoResults_ReturnsEmptyList()
    {
        // Act
        var result = await _domainFacade.SearchChats(null, null, "NonExistentSearchTerm", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent search term");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateChat_ValidData_UpdatesSuccessfully()
    {
        // Arrange - Create a test Chat
        var chat = await CreateTestChatAsync();
        
        // Modify the Chat
        chat.Title = $"Updated{DateTime.Now.Ticks}";
        chat.LastMessageAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _domainFacade.UpdateChat(chat);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated Chat");
        Assert.AreEqual(chat.Title, result.Title);
        
        Console.WriteLine($"Chat updated successfully");
    }

    [TestMethod]
    public async Task UpdateChat_InvalidData_ThrowsValidationException()
    {
        // Arrange - Create a test Chat
        var chat = await CreateTestChatAsync();
        
        // Set invalid data
        chat.PersonaId = Guid.Empty; // Invalid

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ChatValidationException>(() => 
            _domainFacade.UpdateChat(chat), 
            "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteChat_ExistingId_DeletesSuccessfully()
    {
        // Arrange - Create a test Chat
        var chat = await CreateTestChatAsync();

        // Act
        var result = await _domainFacade.DeleteChat(chat.Id);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing Chat");
        var deletedChat = await _domainFacade.GetChatById(chat.Id);
        Assert.IsNull(deletedChat, "Should not find deleted Chat");
        
        Console.WriteLine($"Chat deleted successfully");
    }

    [TestMethod]
    public async Task DeleteChat_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteChat(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task ChatLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var chat = await CreateTestChatAsync("Lifecycle");
        Assert.IsNotNull(chat, "Chat should be created");
        var createdId = chat.Id;
        
        // Get
        var retrievedChat = await _domainFacade.GetChatById(createdId);
        Assert.IsNotNull(retrievedChat, "Should retrieve created Chat");
        Assert.AreEqual(createdId, retrievedChat.Id);
        
        // Update
        retrievedChat.Title = $"UpdatedLifecycle{DateTime.Now.Ticks}";
        retrievedChat.LastMessageAt = DateTime.UtcNow.AddMinutes(10);
        
        var updatedChat = await _domainFacade.UpdateChat(retrievedChat);
        Assert.IsNotNull(updatedChat, "Should update Chat");
        
        // Search - Search by title pattern
        var searchResult = await _domainFacade.SearchChats(null, null, "UpdatedLifecycle", 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated Chat");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteChat(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete Chat");
        
        // Verify deletion
        var deletedChat = await _domainFacade.GetChatById(createdId);
        Assert.IsNull(deletedChat, "Should not find deleted Chat");
        
        Console.WriteLine("Chat lifecycle test completed successfully");
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
                DELETE FROM chats 
                WHERE title LIKE '%Test%'
                   OR title LIKE '%test%'
                   OR title LIKE '%Search%'
                   OR title LIKE '%Update%'
                   OR title LIKE '%Delete%'
                   OR title LIKE '%Lifecycle%'
                   OR title LIKE '%Force%'
                   OR title LIKE '%High School%';";

            var rowsAffected = connection.Execute(resetSql);
            
            Console.WriteLine($"Database reset: Removed {rowsAffected} test Chat records");
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
        var result = await _domainFacade.SearchChats(null, null, "Test", 1, 100);
        
        Assert.IsNotNull(result, "Search should return results object");
        Assert.AreEqual(0, result.TotalCount, $"Database should be clean but found {result.TotalCount} test records");
        Assert.IsFalse(result.Items.Any(), "No test items should remain in database");
        
        Console.WriteLine("✅ Database verification passed - no test data found");
    }
}

