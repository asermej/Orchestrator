using System;
using System.IO;
using System.Threading.Tasks;
using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Topic Training operations using real DomainFacade
/// Tests the integration between topics and training data storage
/// </summary>
[TestClass]
public class DomainFacadeTestsTopicTraining
{
    private DomainFacade _domainFacade;
    private string _connectionString;
    private Guid _generalCategoryId;
    private Guid _testPersonaId;

    public DomainFacadeTestsTopicTraining()
    {
        _domainFacade = null!;
        _connectionString = null!;
        _generalCategoryId = Guid.Empty;
        _testPersonaId = Guid.Empty;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        // Get General category ID
        _generalCategoryId = GetGeneralCategoryId();
        
        // Clean up ALL test data before each test
        TestDataCleanup.CleanupAllTestData(_connectionString);
        TestDataCleanup.CleanupTestTrainingFiles();
        
        // Create a test persona for testing topics
        _testPersonaId = CreateTestPersona();
    }

    private Guid CreateTestPersona()
    {
        var persona = new Persona
        {
            DisplayName = $"TestPersona{DateTime.Now.Ticks}"
        };
        var result = _domainFacade.CreatePersona(persona).GetAwaiter().GetResult();
        return result.Id;
    }

    private Guid GetGeneralCategoryId()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QuerySingle<Guid>("SELECT id FROM categories WHERE name = 'General' LIMIT 1");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
            TestDataCleanup.CleanupAllTestData(_connectionString);
            TestDataCleanup.CleanupTestTrainingFiles();
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

    [TestMethod]
    public async Task SaveTopicTrainingContent_ValidContent_SavesAndRetrievesCorrectly()
    {
        // Arrange - Create topic with initial content (now required)
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        var trainingContent = "This is test training content. The persona should speak in riddles and metaphors.";

        // Act
        var fileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, trainingContent);

        // Assert
        Assert.IsNotNull(fileUrl, "SaveTopicTrainingContent should return a file URL");
        Assert.IsTrue(fileUrl.StartsWith("file:///"), "File URL should start with file:/// (exactly 3 slashes)");
        
        // Verify exactly 3 slashes after file:
        var slashCount = fileUrl.Substring(0, Math.Min(10, fileUrl.Length)).Count(c => c == '/');
        Assert.AreEqual(3, slashCount, "File URL should have exactly 3 slashes after 'file:' (file:///)");

        // Verify content can be retrieved
        var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
        Assert.AreEqual(trainingContent, retrievedContent, "Retrieved content should match saved content");
        
        // Verify the topic's ContentUrl was updated
        var updatedTopic = await _domainFacade.GetTopicById(topic.Id);
        Assert.IsNotNull(updatedTopic, "Topic should exist after saving training content");
        Assert.AreEqual(fileUrl, updatedTopic.ContentUrl, "Topic's ContentUrl should be updated with the file URL");
    }

    [TestMethod]
    public async Task SaveTopicTrainingContent_UpdateExistingContent_ReplacesOldContent()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        var initialContent = "Initial training content";
        var updatedContent = "Updated training content with different information";

        // Act - Save initial content
        var initialFileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, initialContent);
        
        // Act - Update with new content
        var updatedFileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, updatedContent);

        // Assert
        Assert.IsNotNull(updatedFileUrl, "SaveTopicTrainingContent should return a file URL");
        Assert.AreEqual(initialFileUrl, updatedFileUrl, "File URL should be the same when updating content");

        // Verify the new content replaced the old content
        var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
        Assert.AreEqual(updatedContent, retrievedContent, "Retrieved content should match the updated content");
        Assert.AreNotEqual(initialContent, retrievedContent, "Old content should be replaced");
    }

    [TestMethod]
    public async Task SaveTopicTrainingContent_EmptyContent_ThrowsValidationException()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        // Add initial training
        await _domainFacade.SaveTopicTrainingContent(topic.Id, "Initial training");

        // Act & Assert - Attempting to save empty content should throw validation exception
        // Training content is now required and cannot be cleared
        await Assert.ThrowsExceptionAsync<TopicValidationException>(
            () => _domainFacade.SaveTopicTrainingContent(topic.Id, ""),
            "Should throw validation exception when trying to save empty training content"
        );
    }

    [TestMethod]
    public async Task SaveTopicTrainingContent_LargeContent_SavesAndRetrievesCorrectly()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        // Create large content (multiple paragraphs)
        var largeContent = string.Join("\n\n", Enumerable.Range(1, 50).Select(i =>
            $"Paragraph {i}: This is a test paragraph with various information about the topic. " +
            $"It contains multiple sentences and should be preserved exactly as written. " +
            $"The content may include special characters like @#$%^&*() and numbers like 12345."));

        // Act
        var fileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, largeContent);

        // Assert
        Assert.IsNotNull(fileUrl, "SaveTopicTrainingContent should return a file URL");
        Assert.IsTrue(fileUrl.StartsWith("file:///"), "File URL should start with file:///");

        // Verify content can be retrieved exactly as saved
        var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
        Assert.AreEqual(largeContent, retrievedContent, "Retrieved large content should match saved content exactly");
        Assert.AreEqual(largeContent.Length, retrievedContent.Length, "Content length should match");
    }

    [TestMethod]
    public async Task SaveTopicTrainingContent_SpecialCharacters_PreservesContent()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        var contentWithSpecialChars = "Content with special characters:\n" +
            "- Quotes: \"double\" and 'single'\n" +
            "- Symbols: @#$%^&*()\n" +
            "- Unicode: 你好世界 🚀 ñ ü\n" +
            "- Backslashes: C:\\path\\to\\file\n" +
            "- Newlines and\ttabs\n" +
            "- JSON-like: {\"key\": \"value\"}";

        // Act
        var fileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, contentWithSpecialChars);

        // Assert
        Assert.IsNotNull(fileUrl, "SaveTopicTrainingContent should return a file URL");

        // Verify content with special characters is preserved
        var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
        Assert.AreEqual(contentWithSpecialChars, retrievedContent, 
            "Retrieved content should preserve all special characters exactly");
    }

    [TestMethod]
    public async Task GetTopicTrainingContent_NonExistentTopic_ThrowsException()
    {
        // Arrange
        var nonExistentTopicId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TopicNotFoundException>(
            async () => await _domainFacade.GetTopicTrainingContent(nonExistentTopicId),
            "Should throw TopicNotFoundException for non-existent topic");
    }

    [TestMethod]
    public async Task GetTopicTrainingContent_NoTrainingContent_ReturnsEmptyString()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        // Act - Get training content without ever saving any
        var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);

        // Assert
        Assert.AreEqual(string.Empty, retrievedContent, 
            "Should return empty string when no training content has been saved");
    }

    [TestMethod]
    public async Task SaveTopicTrainingContent_MultipleSavesInSequence_MaintainsDataIntegrity()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = "file://initial-content.txt"
        };
        topic = await _domainFacade.CreateTopic(topic);

        var contents = new[]
        {
            "First version of training content",
            "Second version with more details",
            "Third version with even more information",
            "Final version of the training content"
        };

        // Act & Assert - Save multiple times and verify each time
        foreach (var content in contents)
        {
            var fileUrl = await _domainFacade.SaveTopicTrainingContent(topic.Id, content);
            Assert.IsNotNull(fileUrl, "Each save should return a file URL");
            
            var retrievedContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
            Assert.AreEqual(content, retrievedContent, 
                $"Retrieved content should match the current saved content: {content}");
        }

        // Verify final state
        var finalContent = await _domainFacade.GetTopicTrainingContent(topic.Id);
        Assert.AreEqual(contents.Last(), finalContent, 
            "Final retrieved content should match the last saved version");
    }
}

