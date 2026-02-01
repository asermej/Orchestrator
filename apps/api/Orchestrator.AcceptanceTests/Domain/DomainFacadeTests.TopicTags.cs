using System;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Topic-Tag relationship operations using real DomainFacade with data cleanup
/// Tests the integration between topics and tags
/// </summary>
[TestClass]
public class DomainFacadeTestsTopicTags
{
    private DomainFacade _domainFacade;
    private string _connectionString;
    private Guid _generalCategoryId;
    private Guid _testPersonaId;
    private const string TestContentUrl = "file://test-content.txt";

    public DomainFacadeTestsTopicTags()
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
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
        
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

    private Guid GetGeneralCategoryId()
    {
        try
        {
            var sql = "SELECT id FROM categories WHERE name = 'General' LIMIT 1";
            using var connection = new NpgsqlConnection(_connectionString);
            var id = connection.QueryFirstOrDefault<Guid>(sql);
            if (id == Guid.Empty)
            {
                throw new InvalidOperationException("General category not found in database. Ensure categories are seeded.");
            }
            return id;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve General category ID: {ex.Message}", ex);
        }
    }


    [TestMethod]
    public async Task AddTagToTopic_ValidData_AddsTagSuccessfully()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest1", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);

        // Act
        var tag = await _domainFacade.AddTagToTopic(createdTopic.Id, "TestTag1", "testuser");

        // Assert
        Assert.IsNotNull(tag, "Expected tag to be created, but got null");
        Assert.AreEqual("testtag1", tag.Name, $"Expected tag name 'testtag1', but got {tag.Name}");

        // Verify tag is associated with topic
        var topicTags = await _domainFacade.GetTopicTags(createdTopic.Id);
        Assert.IsTrue(topicTags.Any(t => t.Id == tag.Id), "Expected tag to be associated with topic");
        Console.WriteLine($"Tag added to topic successfully: TagID={tag.Id}, TopicID={createdTopic.Id}");
    }

    [TestMethod]
    public async Task AddTagToTopic_MultipleTags_AddsAllTagsSuccessfully()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest2", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);

        // Act
        var tag1 = await _domainFacade.AddTagToTopic(createdTopic.Id, "Sports", "testuser");
        var tag2 = await _domainFacade.AddTagToTopic(createdTopic.Id, "Basketball", "testuser");
        var tag3 = await _domainFacade.AddTagToTopic(createdTopic.Id, "Football", "testuser");

        // Assert
        var topicTags = await _domainFacade.GetTopicTags(createdTopic.Id);
        var tagList = topicTags.ToList();
        
        Assert.AreEqual(3, tagList.Count, $"Expected 3 tags, but got {tagList.Count}");
        Assert.IsTrue(tagList.Any(t => t.Name == "sports"), "Expected to find 'sports' tag");
        Assert.IsTrue(tagList.Any(t => t.Name == "basketball"), "Expected to find 'basketball' tag");
        Assert.IsTrue(tagList.Any(t => t.Name == "football"), "Expected to find 'football' tag");
        Console.WriteLine($"Multiple tags added successfully: {tagList.Count} tags");
    }

    [TestMethod]
    public async Task AddTagToTopic_CaseInsensitive_UsesSameTagForDifferentCases()
    {
        // Arrange
        var topic1 = new Topic { Name = "TopicTagTest3A", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var topic2 = new Topic { Name = "TopicTagTest3B", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic1 = await _domainFacade.CreateTopic(topic1);
        var createdTopic2 = await _domainFacade.CreateTopic(topic2);

        // Act
        var tag1 = await _domainFacade.AddTagToTopic(createdTopic1.Id, "TopicTagTest", "testuser");
        var tag2 = await _domainFacade.AddTagToTopic(createdTopic2.Id, "topictagtest", "testuser");

        // Assert
        Assert.AreEqual(tag1.Id, tag2.Id, "Expected same tag ID for case-insensitive tag names");
        Assert.AreEqual("topictagtest", tag1.Name, $"Expected normalized name 'topictagtest', but got {tag1.Name}");
        Console.WriteLine($"Case-insensitive tag reuse verified: TagID={tag1.Id}");
    }

    [TestMethod]
    public async Task GetTopicTags_TopicWithTags_ReturnsAllTags()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest4", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        await _domainFacade.AddTagToTopic(createdTopic.Id, "Tag1", "testuser");
        await _domainFacade.AddTagToTopic(createdTopic.Id, "Tag2", "testuser");

        // Act
        var tags = await _domainFacade.GetTopicTags(createdTopic.Id);
        var tagList = tags.ToList();

        // Assert
        Assert.AreEqual(2, tagList.Count, $"Expected 2 tags, but got {tagList.Count}");
        Console.WriteLine($"Retrieved {tagList.Count} tags for topic");
    }

    [TestMethod]
    public async Task GetTopicTags_TopicWithNoTags_ReturnsEmptyList()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest5", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);

        // Act
        var tags = await _domainFacade.GetTopicTags(createdTopic.Id);
        var tagList = tags.ToList();

        // Assert
        Assert.AreEqual(0, tagList.Count, $"Expected 0 tags, but got {tagList.Count}");
        Console.WriteLine("Correctly returned empty list for topic with no tags");
    }

    [TestMethod]
    public async Task RemoveTagFromTopic_ExistingTag_RemovesSuccessfully()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest6", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        var tag = await _domainFacade.AddTagToTopic(createdTopic.Id, "TagToRemove", "testuser");

        // Act
        var result = await _domainFacade.RemoveTagFromTopic(createdTopic.Id, tag.Id);

        // Assert
        Assert.IsTrue(result, "Expected tag removal to succeed");

        // Verify removal
        var topicTags = await _domainFacade.GetTopicTags(createdTopic.Id);
        Assert.IsFalse(topicTags.Any(t => t.Id == tag.Id), "Expected tag to be removed from topic");
        Console.WriteLine($"Tag removed from topic successfully");
    }

    [TestMethod]
    public async Task RemoveTagFromTopic_NonExistingAssociation_ReturnsFalse()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest7", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        var nonExistingTagId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.RemoveTagFromTopic(createdTopic.Id, nonExistingTagId);

        // Assert
        Assert.IsFalse(result, "Expected false for removing non-existing tag association");
        Console.WriteLine("Correctly returned false for non-existing tag removal");
    }

    [TestMethod]
    public async Task UpdateTopicTags_ReplacesAllTags_Successfully()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest8", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        await _domainFacade.AddTagToTopic(createdTopic.Id, "OldTag1", "testuser");
        await _domainFacade.AddTagToTopic(createdTopic.Id, "OldTag2", "testuser");

        var newTags = new[] { "UpdatedTag1", "UpdatedTag2", "UpdatedTag3" };

        // Act
        var result = await _domainFacade.UpdateTopicTags(createdTopic.Id, newTags, "testuser");
        var resultList = result.ToList();

        // Assert
        Assert.AreEqual(3, resultList.Count, $"Expected 3 tags after update, but got {resultList.Count}");

        // Verify all new tags are present
        var topicTags = await _domainFacade.GetTopicTags(createdTopic.Id);
        var topicTagList = topicTags.ToList();
        Assert.AreEqual(3, topicTagList.Count, $"Expected 3 tags from GetTopicTags, but got {topicTagList.Count}");
        Assert.IsTrue(topicTagList.Any(t => t.Name == "updatedtag1"), "Expected to find 'updatedtag1'");
        Assert.IsTrue(topicTagList.Any(t => t.Name == "updatedtag2"), "Expected to find 'updatedtag2'");
        Assert.IsTrue(topicTagList.Any(t => t.Name == "updatedtag3"), "Expected to find 'updatedtag3'");

        // Verify old tags are removed
        Assert.IsFalse(topicTagList.Any(t => t.Name == "oldtag1"), "Expected 'oldtag1' to be removed");
        Assert.IsFalse(topicTagList.Any(t => t.Name == "oldtag2"), "Expected 'oldtag2' to be removed");
        Console.WriteLine($"Topic tags updated successfully: {topicTagList.Count} tags");
    }

    [TestMethod]
    public async Task UpdateTopicTags_EmptyList_RemovesAllTags()
    {
        // Arrange
        var topic = new Topic { Name = "TopicTagTest9", CategoryId = _generalCategoryId, PersonaId = _testPersonaId, ContentUrl = TestContentUrl };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        await _domainFacade.AddTagToTopic(createdTopic.Id, "TagToRemove1", "testuser");
        await _domainFacade.AddTagToTopic(createdTopic.Id, "TagToRemove2", "testuser");

        // Act
        var result = await _domainFacade.UpdateTopicTags(createdTopic.Id, Array.Empty<string>(), "testuser");

        // Assert
        var topicTags = await _domainFacade.GetTopicTags(createdTopic.Id);
        Assert.AreEqual(0, topicTags.Count(), "Expected all tags to be removed");
        Console.WriteLine("All tags removed successfully with empty list");
    }

    [TestMethod]
    public async Task AddTagToTopic_NonExistingTopic_ThrowsTopicNotFoundException()
    {
        // Arrange
        var nonExistingTopicId = Guid.NewGuid();

        // Act & Assert
        try
        {
            await _domainFacade.AddTagToTopic(nonExistingTopicId, "TestTag", "testuser");
            Assert.Fail("Expected TopicNotFoundException to be thrown");
        }
        catch (TopicNotFoundException ex)
        {
            Console.WriteLine($"Expected exception thrown: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains(nonExistingTopicId.ToString()), 
                $"Expected error message to contain topic ID, but got: {ex.Message}");
        }
    }
}

