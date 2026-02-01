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
/// Tests for Topic operations using real DomainFacade and real DataFacade with data cleanup
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
/// - Identifies test data by patterns (name patterns, specific test topics)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsTopic
{
    private DomainFacade _domainFacade;
    private string _connectionString;
    private Guid _generalCategoryId;
    private Guid _testPersonaId;
    private const string TestContentUrl = "file://test-content.txt";

    public DomainFacadeTestsTopic()
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
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
        
        // Get the General category ID for use in tests
        _generalCategoryId = GetGeneralCategoryId();
        
        // Create a test persona for testing topics
        _testPersonaId = CreateTestPersona();
    }

    /// <summary>
    /// Creates a test persona for use in topic tests
    /// </summary>
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

    /// <summary>
    /// Gets the General category ID from the database for use in tests
    /// </summary>
    private Guid GetGeneralCategoryId()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = "SELECT id FROM categories WHERE name = 'General' LIMIT 1";
            var categoryId = connection.QueryFirstOrDefault<Guid>(sql);
            
            if (categoryId == Guid.Empty)
            {
                throw new InvalidOperationException("General category not found in database. Ensure categories are seeded.");
            }
            
            return categoryId;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve General category ID: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// Helper method to create test Topic with unique data
    /// </summary>
    private async Task<Topic> CreateTestTopicAsync(string suffix = "")
    {
        var topic = new Topic
        {
            Name = $"Test{suffix}{DateTime.Now.Ticks}",
            Description = $"Test description {suffix}",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };

        var result = await _domainFacade.CreateTopic(topic);
        Assert.IsNotNull(result, "Failed to create test Topic");
        return result;
    }

    [TestMethod]
    public async Task CreateTopic_ValidData_ReturnsCreatedTopic()
    {
        // Arrange
        var topic = new Topic
        {
            Name = $"name{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };

        // Act
        var result = await _domainFacade.CreateTopic(topic);

        // Assert
        Assert.IsNotNull(result, "Create should return a Topic");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Topic should have a valid ID");
        Assert.AreEqual(topic.Name, result.Name);
        Assert.AreEqual(topic.Description, result.Description);
        Assert.AreEqual(topic.PersonaId, result.PersonaId);
        Assert.AreEqual(topic.ContentUrl, result.ContentUrl);
        Assert.AreEqual(topic.CategoryId, result.CategoryId);
        
        Console.WriteLine($"Topic created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateTopic_InvalidData_ThrowsValidationException()
    {
        // Arrange - Topic with empty required name field
        var topic = new Topic
        {
            Name = "", // Required field empty
            Description = "Test description",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TopicValidationException>(() => 
            _domainFacade.CreateTopic(topic), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task CreateTopic_WithoutContentUrl_SucceedsForNewTopic()
    {
        // Arrange - New topic with empty ContentUrl should be allowed
        // The ContentUrl will be populated after creation when training content is saved
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            ContentUrl = "", // Empty is OK for new topics
            CategoryId = _generalCategoryId
        };

        // Act
        var createdTopic = await _domainFacade.CreateTopic(topic);

        // Assert
        Assert.IsNotNull(createdTopic, "Topic should be created successfully");
        Assert.AreNotEqual(Guid.Empty, createdTopic.Id, "Created topic should have a valid ID");
        Assert.AreEqual(topic.Name, createdTopic.Name);
        
        // Cleanup
        await _domainFacade.DeleteTopic(createdTopic.Id);
    }

    [TestMethod]
    public async Task UpdateTopic_WithEmptyContentUrl_ThrowsValidationException()
    {
        // Arrange - Create a topic with content URL first
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };
        var createdTopic = await _domainFacade.CreateTopic(topic);

        try
        {
            // Now try to update it with empty ContentUrl
            createdTopic.ContentUrl = "";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TopicValidationException>(() => 
                _domainFacade.UpdateTopic(createdTopic), 
                "Should throw validation exception when updating topic with empty ContentUrl");
        }
        finally
        {
            // Cleanup
            await _domainFacade.DeleteTopic(createdTopic.Id);
        }
    }

    [TestMethod]
    public async Task CreateTopic_WithValidContent_CreatesSuccessfully()
    {
        // Arrange - Topic with valid training content
        var topic = new Topic
        {
            Name = $"ValidContentTopic{DateTime.Now.Ticks}",
            Description = "Topic with valid training content",
            PersonaId = _testPersonaId,
            ContentUrl = "file://valid-training-content.txt",
            CategoryId = _generalCategoryId
        };

        // Act
        var result = await _domainFacade.CreateTopic(topic);

        // Assert
        Assert.IsNotNull(result, "Topic with valid content should be created");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Topic should have a valid ID");
        Assert.AreEqual(topic.ContentUrl, result.ContentUrl, "ContentUrl should match");
        
        Console.WriteLine($"Topic created successfully with content URL: {result.ContentUrl}");
    }

    [TestMethod]
    public async Task CreateTopic_MissingCategoryId_ThrowsValidationException()
    {
        // Arrange - Topic with missing CategoryId
        var topic = new Topic
        {
            Name = $"TestTopic{DateTime.Now.Ticks}",
            Description = "Test description",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = Guid.Empty // Invalid empty CategoryId
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TopicValidationException>(() => 
            _domainFacade.CreateTopic(topic), "Should throw validation exception for missing CategoryId");
    }

    [TestMethod]
    public async Task GetTopicById_ExistingId_ReturnsTopic()
    {
        // Arrange - Create a test Topic
        var createdTopic = await CreateTestTopicAsync();

        // Act
        var result = await _domainFacade.GetTopicById(createdTopic.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return Topic with ID: {createdTopic.Id}");
        Assert.AreEqual(createdTopic.Id, result.Id);
        Assert.AreEqual(createdTopic.Name, result.Name);
        Assert.AreEqual(createdTopic.Description, result.Description);
    }

    [TestMethod]
    public async Task GetTopicById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetTopicById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchTopics_ByName_ReturnsPaginatedList()
    {
        // Arrange - Create some test Topics
        var topic1 = await CreateTestTopicAsync("Search1");
        var topic2 = await CreateTestTopicAsync("Search2");

        // Act - Search by name
        var result = await _domainFacade.SearchTopics("Search", null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 Topics, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, $"Should return at least 2 items, returned {result.Items.Count()}");
        
        Console.WriteLine($"Search returned {result.TotalCount} total Topics");
    }

    [TestMethod]
    public async Task SearchTopics_ByPersonaId_ReturnsFilteredList()
    {
        // Arrange - Create topics for two different personas
        var persona2 = new Persona
        {
            DisplayName = $"TestPersona2{DateTime.Now.Ticks}"
        };
        var createdPersona2 = await _domainFacade.CreatePersona(persona2);
        
        var topic1 = new Topic
        {
            Name = $"TestPersona1Topic{DateTime.Now.Ticks}",
            Description = "Topic for persona 1",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };
        await _domainFacade.CreateTopic(topic1);

        var topic2 = new Topic
        {
            Name = $"TestPersona2Topic{DateTime.Now.Ticks}",
            Description = "Topic for persona 2",
            PersonaId = createdPersona2.Id,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };
        await _domainFacade.CreateTopic(topic2);

        // Act - Search for topics by persona 1 only
        var result = await _domainFacade.SearchTopics(null, _testPersonaId, 1, 100);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 1, "Should find at least one topic for persona 1");
        Assert.IsTrue(result.Items.All(t => t.PersonaId == _testPersonaId), "All returned topics should belong to persona 1");
    }

    [TestMethod]
    public async Task SearchTopics_NoResults_ReturnsEmptyList()
    {
        // Act
        var result = await _domainFacade.SearchTopics("NonExistentSearchTerm", null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent search term");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateTopic_ValidData_UpdatesSuccessfully()
    {
        // Arrange - Create a test Topic
        var topic = await CreateTestTopicAsync();
        
        // Modify the Topic
        topic.Name = $"Updated{DateTime.Now.Ticks}";
        topic.Description = "Updated description";
        topic.ContributionNotes = "Updated contribution notes";

        // Act
        var result = await _domainFacade.UpdateTopic(topic);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated Topic");
        Assert.AreEqual(topic.Name, result.Name);
        Assert.AreEqual(topic.Description, result.Description);
        Assert.AreEqual(topic.ContributionNotes, result.ContributionNotes);
        
        Console.WriteLine($"Topic updated successfully");
    }

    [TestMethod]
    public async Task UpdateTopic_InvalidData_ThrowsValidationException()
    {
        // Arrange - Create a test Topic
        var topic = await CreateTestTopicAsync();
        
        // Set invalid data
        topic.Name = ""; // Invalid empty value

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TopicValidationException>(() => 
            _domainFacade.UpdateTopic(topic), 
            "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteTopic_ExistingId_DeletesSuccessfully()
    {
        // Arrange - Create a test Topic
        var topic = await CreateTestTopicAsync();

        // Act
        var result = await _domainFacade.DeleteTopic(topic.Id);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing Topic");
        var deletedTopic = await _domainFacade.GetTopicById(topic.Id);
        Assert.IsNull(deletedTopic, "Should not find deleted Topic");
        
        Console.WriteLine($"Topic deleted successfully");
    }

    [TestMethod]
    public async Task DeleteTopic_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteTopic(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task SearchTopicsWithTags_ReturnsTopicsAndTags()
    {
        // Arrange - Create test topics with tags
        var topic1 = await CreateTestTopicAsync("WithTags1");
        var topic2 = await CreateTestTopicAsync("WithTags2");
        
        // Add tags to topics
        var tag1 = await _domainFacade.AddTagToTopic(topic1.Id, "testtag1");
        var tag2 = await _domainFacade.AddTagToTopic(topic1.Id, "testtag2");
        var tag3 = await _domainFacade.AddTagToTopic(topic2.Id, "testtag3");

        // Act - Search topics with tags using optimized method
        var (topicsResult, tagsByTopicId) = await _domainFacade.SearchTopicsWithTags("WithTags", null, 1, 10);

        // Assert
        Assert.IsNotNull(topicsResult, "Search should return topics result");
        Assert.IsNotNull(tagsByTopicId, "Search should return tags dictionary");
        Assert.IsTrue(topicsResult.TotalCount >= 2, $"Should find at least 2 topics, found {topicsResult.TotalCount}");
        
        // Verify topic1 has 2 tags
        Assert.IsTrue(tagsByTopicId.ContainsKey(topic1.Id), "Tags dictionary should contain topic1");
        Assert.AreEqual(2, tagsByTopicId[topic1.Id].Count, $"Topic1 should have 2 tags, but has {tagsByTopicId[topic1.Id].Count}");
        Assert.IsTrue(tagsByTopicId[topic1.Id].Any(t => t.Name == "testtag1"), "Topic1 should have testtag1");
        Assert.IsTrue(tagsByTopicId[topic1.Id].Any(t => t.Name == "testtag2"), "Topic1 should have testtag2");
        
        // Verify topic2 has 1 tag
        Assert.IsTrue(tagsByTopicId.ContainsKey(topic2.Id), "Tags dictionary should contain topic2");
        Assert.AreEqual(1, tagsByTopicId[topic2.Id].Count, $"Topic2 should have 1 tag, but has {tagsByTopicId[topic2.Id].Count}");
        Assert.IsTrue(tagsByTopicId[topic2.Id].Any(t => t.Name == "testtag3"), "Topic2 should have testtag3");
        
        Console.WriteLine($"SearchTopicsWithTags returned {topicsResult.TotalCount} topics with tags");
    }

    [TestMethod]
    public async Task SearchTopicsWithTags_TopicsWithoutTags_ReturnsEmptyTagsList()
    {
        // Arrange - Create test topics WITHOUT tags
        var topic1 = await CreateTestTopicAsync("NoTags1");
        var topic2 = await CreateTestTopicAsync("NoTags2");

        // Act - Search topics with tags
        var (topicsResult, tagsByTopicId) = await _domainFacade.SearchTopicsWithTags("NoTags", null, 1, 10);

        // Assert
        Assert.IsNotNull(topicsResult, "Search should return topics result");
        Assert.IsNotNull(tagsByTopicId, "Search should return tags dictionary");
        Assert.IsTrue(topicsResult.TotalCount >= 2, $"Should find at least 2 topics, found {topicsResult.TotalCount}");
        
        // Verify topics have no tags (not in dictionary or empty list)
        if (tagsByTopicId.ContainsKey(topic1.Id))
        {
            Assert.AreEqual(0, tagsByTopicId[topic1.Id].Count, "Topic1 should have no tags");
        }
        
        if (tagsByTopicId.ContainsKey(topic2.Id))
        {
            Assert.AreEqual(0, tagsByTopicId[topic2.Id].Count, "Topic2 should have no tags");
        }
        
        Console.WriteLine($"SearchTopicsWithTags correctly handled topics without tags");
    }

    [TestMethod]
    public async Task SearchTopicsWithTags_ByPersonaId_ReturnsFilteredTopicsWithTags()
    {
        // Arrange - Create topics for two different personas
        var persona2 = new Persona
        {
            DisplayName = $"TestPersona2{DateTime.Now.Ticks}"
        };
        var createdPersona2 = await _domainFacade.CreatePersona(persona2);
        
        // Create topics for persona 1 with tags
        var topic1 = new Topic
        {
            Name = $"TestFilteredWithTags{DateTime.Now.Ticks}",
            Description = "Topic for persona 1 with tags",
            PersonaId = _testPersonaId,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };
        var createdTopic1 = await _domainFacade.CreateTopic(topic1);
        await _domainFacade.AddTagToTopic(createdTopic1.Id, "persona1tag");

        // Create topics for persona 2 with tags
        var topic2 = new Topic
        {
            Name = $"TestFilteredWithTags2{DateTime.Now.Ticks}",
            Description = "Topic for persona 2 with tags",
            PersonaId = createdPersona2.Id,
            ContentUrl = TestContentUrl,
            CategoryId = _generalCategoryId
        };
        var createdTopic2 = await _domainFacade.CreateTopic(topic2);
        await _domainFacade.AddTagToTopic(createdTopic2.Id, "persona2tag");

        // Act - Search for topics by persona 1 only with tags
        var (topicsResult, tagsByTopicId) = await _domainFacade.SearchTopicsWithTags(null, _testPersonaId, 1, 100);

        // Assert
        Assert.IsNotNull(topicsResult, "Search should return topics result");
        Assert.IsNotNull(tagsByTopicId, "Search should return tags dictionary");
        Assert.IsTrue(topicsResult.TotalCount >= 1, "Should find at least one topic for persona 1");
        Assert.IsTrue(topicsResult.Items.All(t => t.PersonaId == _testPersonaId), "All returned topics should belong to persona 1");
        
        // Verify only persona 1's topic tags are returned
        Assert.IsTrue(tagsByTopicId.ContainsKey(createdTopic1.Id), "Should have tags for persona 1's topic");
        Assert.IsFalse(tagsByTopicId.ContainsKey(createdTopic2.Id), "Should NOT have tags for persona 2's topic");
        
        Console.WriteLine($"SearchTopicsWithTags correctly filtered by personaId with tags");
    }

    [TestMethod]
    public async Task TopicLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var topic = await CreateTestTopicAsync("Lifecycle");
        Assert.IsNotNull(topic, "Topic should be created");
        var createdId = topic.Id;
        
        // Get
        var retrievedTopic = await _domainFacade.GetTopicById(createdId);
        Assert.IsNotNull(retrievedTopic, "Should retrieve created Topic");
        Assert.AreEqual(createdId, retrievedTopic.Id);
        
        // Update
        retrievedTopic.Name = $"UpdatedLifecycle{DateTime.Now.Ticks}";
        retrievedTopic.Description = "Updated lifecycle description";
        
        var updatedTopic = await _domainFacade.UpdateTopic(retrievedTopic);
        Assert.IsNotNull(updatedTopic, "Should update Topic");
        Assert.AreEqual(retrievedTopic.Name, updatedTopic.Name);
        
        // Search
        var searchResult = await _domainFacade.SearchTopics("UpdatedLifecycle", null, 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated Topic");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteTopic(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete Topic");
        
        // Verify deletion
        var deletedTopic = await _domainFacade.GetTopicById(createdId);
        Assert.IsNull(deletedTopic, "Should not find deleted Topic");
        
        Console.WriteLine("Topic lifecycle test completed successfully");
    }
}

