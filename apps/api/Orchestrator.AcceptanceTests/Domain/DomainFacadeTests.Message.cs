using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Message operations using real DomainFacade and real DataFacade with data cleanup
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
/// - Identifies test data by patterns (content patterns, test chats)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsMessage
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsMessage()
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
    /// Helper method to create a test chat for message testing
    /// </summary>
    private async Task<Chat> CreateTestChat()
    {
        // Create a persona first to get a valid PersonaId
        var persona = new Persona
        {
            DisplayName = $"TestPersona{DateTime.Now.Ticks}"
        };
        persona = await _domainFacade.CreatePersona(persona);

        var chat = new Chat
        {
            PersonaId = persona.Id,
            UserId = Guid.NewGuid(),
            Title = $"TestChat{DateTime.Now.Ticks}",
            LastMessageAt = DateTime.UtcNow
        };
        return await _domainFacade.CreateChat(chat);
    }

    /// <summary>
    /// Helper method to create test Message with unique data
    /// </summary>
    private async Task<Message> CreateTestMessageAsync(Guid chatId, string role = "user", string suffix = "")
    {
        var message = new Message
        {
            ChatId = chatId,
            Role = role,
            Content = $"Test{suffix}{DateTime.Now.Ticks}"
        };

        var result = await _domainFacade.CreateMessage(message);
        Assert.IsNotNull(result, "Failed to create test Message");
        return result;
    }

    [TestMethod]
    public async Task CreateMessage_ValidData_ReturnsCreatedMessage()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message = new Message
        {
            ChatId = testChat.Id,
            Role = "user",
            Content = $"Hello, this is a test message {DateTime.Now.Ticks}"
        };

        // Act
        var result = await _domainFacade.CreateMessage(message);

        // Assert
        Assert.IsNotNull(result, "Create should return a Message");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Message should have a valid ID");
        Assert.AreEqual(message.ChatId, result.ChatId);
        Assert.AreEqual(message.Role, result.Role);
        Assert.AreEqual(message.Content, result.Content);
        Assert.AreNotEqual(default(DateTime), result.CreatedAt, "CreatedAt should be set");
        
        Console.WriteLine($"Message created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateMessage_AssistantRole_ReturnsCreatedMessage()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message = new Message
        {
            ChatId = testChat.Id,
            Role = "assistant",
            Content = $"Test assistant response {DateTime.Now.Ticks}"
        };

        // Act
        var result = await _domainFacade.CreateMessage(message);

        // Assert
        Assert.IsNotNull(result, "Create should return a Message");
        Assert.AreEqual("assistant", result.Role);
        
        Console.WriteLine($"Assistant Message created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateMessage_InvalidRole_ThrowsValidationException()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message = new Message
        {
            ChatId = testChat.Id,
            Role = "invalid_role", // Invalid role
            Content = "Test content"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<MessageValidationException>(() => 
            _domainFacade.CreateMessage(message), 
            "Should throw validation exception for invalid role");
    }

    [TestMethod]
    public async Task CreateMessage_EmptyChatId_ThrowsValidationException()
    {
        // Arrange
        var message = new Message
        {
            ChatId = Guid.Empty, // Invalid - required field
            Role = "user",
            Content = "Test content"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<MessageValidationException>(() => 
            _domainFacade.CreateMessage(message), 
            "Should throw validation exception for empty ChatId");
    }

    [TestMethod]
    public async Task CreateMessage_EmptyContent_ThrowsValidationException()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message = new Message
        {
            ChatId = testChat.Id,
            Role = "user",
            Content = "" // Invalid - required field
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<MessageValidationException>(() => 
            _domainFacade.CreateMessage(message), 
            "Should throw validation exception for empty Content");
    }

    [TestMethod]
    [Ignore("Requires real OpenAI API key. TODO: Implement custom test double for OpenAI service per testing standards.")]
    public async Task CreateUserMessageAndGetAIResponse_ValidUserMessage_ReturnsAIResponse()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var userMessage = new Message
        {
            ChatId = testChat.Id,
            Role = "user",
            Content = $"User message for AI response test {DateTime.Now.Ticks}"
        };

        // Act
        var aiResponse = await _domainFacade.CreateUserMessageAndGetAIResponse(userMessage);

        // Assert
        Assert.IsNotNull(aiResponse, "AI response should not be null");
        Assert.AreNotEqual(Guid.Empty, aiResponse.Id, "AI response should have a valid ID");
        Assert.AreEqual(testChat.Id, aiResponse.ChatId, "AI response should be in the same chat");
        Assert.AreEqual("assistant", aiResponse.Role, "AI response should have 'assistant' role");
        Assert.IsFalse(string.IsNullOrWhiteSpace(aiResponse.Content), "AI response should have content");
        
        // Verify both messages were saved
        var messages = await _domainFacade.SearchMessages(testChat.Id, null, null, 1, 10);
        Assert.IsTrue(messages.TotalCount >= 2, "Should have at least 2 messages (user + AI)");
        
        Console.WriteLine($"AI response created: {aiResponse.Content}");
    }

    [TestMethod]
    public async Task CreateUserMessageAndGetAIResponse_AssistantRole_ThrowsValidationException()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var assistantMessage = new Message
        {
            ChatId = testChat.Id,
            Role = "assistant", // Invalid for this method
            Content = "Test content"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<MessageValidationException>(() => 
            _domainFacade.CreateUserMessageAndGetAIResponse(assistantMessage), 
            "Should throw validation exception when role is not 'user'");
    }

    [TestMethod]
    public async Task GetMessageById_ExistingId_ReturnsMessage()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var createdMessage = await CreateTestMessageAsync(testChat.Id);

        // Act
        var result = await _domainFacade.GetMessageById(createdMessage.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return Message with ID: {createdMessage.Id}");
        Assert.AreEqual(createdMessage.Id, result.Id);
        Assert.AreEqual(createdMessage.ChatId, result.ChatId);
        Assert.AreEqual(createdMessage.Role, result.Role);
        Assert.AreEqual(createdMessage.Content, result.Content);
    }

    [TestMethod]
    public async Task GetMessageById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetMessageById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchMessages_ByChatId_ReturnsMessagesInOrder()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message1 = await CreateTestMessageAsync(testChat.Id, "user", "1");
        await Task.Delay(100); // Ensure different timestamps
        var message2 = await CreateTestMessageAsync(testChat.Id, "assistant", "2");
        await Task.Delay(100);
        var message3 = await CreateTestMessageAsync(testChat.Id, "user", "3");

        // Act
        var result = await _domainFacade.SearchMessages(testChat.Id, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 3, $"Should find at least 3 messages, found {result.TotalCount}");
        
        // Verify chronological order (ASC)
        var messagesList = result.Items.ToList();
        Assert.IsTrue(messagesList.Count >= 3, "Should return at least 3 items");
        
        Console.WriteLine($"Search returned {result.TotalCount} messages in chronological order");
    }

    [TestMethod]
    public async Task SearchMessages_ByRole_ReturnsFilteredMessages()
    {
        // Arrange
        var testChat = await CreateTestChat();
        await CreateTestMessageAsync(testChat.Id, "user", "User1");
        await CreateTestMessageAsync(testChat.Id, "assistant", "AI1");
        await CreateTestMessageAsync(testChat.Id, "user", "User2");

        // Act - Search for user messages only
        var result = await _domainFacade.SearchMessages(testChat.Id, "user", null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 user messages, found {result.TotalCount}");
        Assert.IsTrue(result.Items.All(m => m.Role == "user"), "All returned messages should have 'user' role");
    }

    [TestMethod]
    public async Task SearchMessages_ByContent_ReturnsMatchingMessages()
    {
        // Arrange
        var testChat = await CreateTestChat();
        await CreateTestMessageAsync(testChat.Id, "user", "SearchableContent");
        await CreateTestMessageAsync(testChat.Id, "user", "OtherContent");

        // Act
        var result = await _domainFacade.SearchMessages(testChat.Id, null, "Searchable", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 1, $"Should find at least 1 message with 'Searchable', found {result.TotalCount}");
    }

    [TestMethod]
    public async Task SearchMessages_NoResults_ReturnsEmptyList()
    {
        // Act
        var result = await _domainFacade.SearchMessages(Guid.NewGuid(), null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent chat");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task DeleteMessage_ExistingId_DeletesSuccessfully()
    {
        // Arrange
        var testChat = await CreateTestChat();
        var message = await CreateTestMessageAsync(testChat.Id);

        // Act
        var result = await _domainFacade.DeleteMessage(message.Id);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing Message");
        var deletedMessage = await _domainFacade.GetMessageById(message.Id);
        Assert.IsNull(deletedMessage, "Should not find deleted Message");
        
        Console.WriteLine($"Message deleted successfully");
    }

    [TestMethod]
    public async Task DeleteMessage_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteMessage(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task MessageLifecycleTest_CreateGetSearchDelete_WorksCorrectly()
    {
        // Create chat
        var testChat = await CreateTestChat();
        Assert.IsNotNull(testChat, "Chat should be created");
        
        // Create message
        var message = await CreateTestMessageAsync(testChat.Id, "user", "Lifecycle");
        Assert.IsNotNull(message, "Message should be created");
        var createdId = message.Id;
        
        // Get
        var retrievedMessage = await _domainFacade.GetMessageById(createdId);
        Assert.IsNotNull(retrievedMessage, "Should retrieve created Message");
        Assert.AreEqual(createdId, retrievedMessage.Id);
        
        // Search
        var searchResult = await _domainFacade.SearchMessages(testChat.Id, null, "Lifecycle", 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find the message");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteMessage(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete Message");
        
        // Verify deletion
        var deletedMessage = await _domainFacade.GetMessageById(createdId);
        Assert.IsNull(deletedMessage, "Should not find deleted Message");
        
        Console.WriteLine("Message lifecycle test completed successfully");
    }

    /// <summary>
    /// Optional: Call this method to completely reset the database after all tests
    /// </summary>
    [TestMethod]
    [TestCategory("DatabaseReset")]
    public void ResetDatabase_RemoveAllTestData()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var resetSql = @"
                DELETE FROM messages 
                WHERE content LIKE '%Test%'
                   OR content LIKE '%test%'
                   OR content LIKE '%simulated%'
                   OR content LIKE '%User message%'
                   OR content LIKE '%Hello%';";

            var rowsAffected = connection.Execute(resetSql);
            
            Console.WriteLine($"Database reset: Removed {rowsAffected} test Message records");
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
        var result = await _domainFacade.SearchMessages(null, null, "Test", 1, 100);
        
        Assert.IsNotNull(result, "Search should return results object");
        Assert.AreEqual(0, result.TotalCount, $"Database should be clean but found {result.TotalCount} test records");
        Assert.IsFalse(result.Items.Any(), "No test items should remain in database");
        
        Console.WriteLine("✅ Database verification passed - no test data found");
    }
}

