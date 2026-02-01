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
/// Tests for Tag operations using real DomainFacade with data cleanup
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
/// - Identifies test data by patterns (name patterns, specific test tags)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsTag
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsTag()
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

    [TestMethod]
    public async Task CreateTag_ValidData_CreatesTagWithNormalizedName()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag" }; // Mixed case

        // Act
        var result = await _domainFacade.CreateTag(tag);

        // Assert
        Assert.IsNotNull(result, "Expected tag to be created, but got null");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Expected a valid Guid for tag ID");
        Assert.AreEqual("testtag", result.Name, "Expected tag name to be normalized to lowercase");
        Console.WriteLine($"Tag created successfully with ID: {result.Id}, Name: {result.Name}");
    }

    [TestMethod]
    public async Task CreateTag_DuplicateName_ThrowsDuplicateException()
    {
        // Arrange
        var tag1 = new Tag { Name = "duplicatetag" };
        await _domainFacade.CreateTag(tag1);

        var tag2 = new Tag { Name = "DuplicateTag" }; // Different case but should be treated as duplicate

        // Act & Assert
        try
        {
            await _domainFacade.CreateTag(tag2);
            Assert.Fail("Expected TagDuplicateException to be thrown");
        }
        catch (TagDuplicateException ex)
        {
            Console.WriteLine($"Expected exception thrown: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains("duplicatetag"), $"Expected error message to contain 'duplicatetag', but got: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task CreateTag_CaseInsensitive_TreatsCelticsAndCelticsAsSame()
    {
        // Arrange
        var tag1 = new Tag { Name = "Celtics" };
        await _domainFacade.CreateTag(tag1);

        var tag2 = new Tag { Name = "celtics" };

        // Act & Assert
        try
        {
            await _domainFacade.CreateTag(tag2);
            Assert.Fail("Expected TagDuplicateException to be thrown for case-insensitive duplicate");
        }
        catch (TagDuplicateException ex)
        {
            Console.WriteLine($"Expected exception thrown: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains("celtics"), $"Expected error message to contain 'celtics', but got: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task GetTagById_ExistingTag_ReturnsTag()
    {
        // Arrange
        var tag = new Tag { Name = "searchtest" };
        var created = await _domainFacade.CreateTag(tag);

        // Act
        var result = await _domainFacade.GetTagById(created.Id);

        // Assert
        Assert.IsNotNull(result, "Expected tag to be found, but got null");
        Assert.AreEqual(created.Id, result.Id, $"Expected tag ID {created.Id}, but got {result.Id}");
        Assert.AreEqual("searchtest", result.Name, $"Expected tag name 'searchtest', but got {result.Name}");
        Console.WriteLine($"Tag retrieved successfully: ID={result.Id}, Name={result.Name}");
    }

    [TestMethod]
    public async Task GetTagById_NonExistingTag_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetTagById(nonExistingId);

        // Assert
        Assert.IsNull(result, $"Expected null for non-existing tag, but got a tag with ID {result?.Id}");
        Console.WriteLine("Correctly returned null for non-existing tag");
    }

    [TestMethod]
    public async Task GetTagByName_ExistingTag_ReturnsTag()
    {
        // Arrange
        var tag = new Tag { Name = "searchbyname" };
        await _domainFacade.CreateTag(tag);

        // Act
        var result = await _domainFacade.GetTagByName("searchbyname");

        // Assert
        Assert.IsNotNull(result, "Expected tag to be found by name, but got null");
        Assert.AreEqual("searchbyname", result.Name, $"Expected tag name 'searchbyname', but got {result.Name}");
        Console.WriteLine($"Tag retrieved by name successfully: {result.Name}");
    }

    [TestMethod]
    public async Task GetTagByName_CaseInsensitive_FindsTag()
    {
        // Arrange
        var tag = new Tag { Name = "Basketball" };
        await _domainFacade.CreateTag(tag);

        // Act
        var result = await _domainFacade.GetTagByName("BASKETBALL");

        // Assert
        Assert.IsNotNull(result, "Expected tag to be found with case-insensitive search, but got null");
        Assert.AreEqual("basketball", result.Name, $"Expected tag name 'basketball', but got {result.Name}");
        Console.WriteLine($"Tag retrieved case-insensitively: {result.Name}");
    }

    [TestMethod]
    public async Task SearchTags_WithSearchTerm_ReturnsMatchingTags()
    {
        // Arrange - Create tags and link them to topics so they appear in search
        var tag1 = await _domainFacade.CreateTag(new Tag { Name = "searchtest1" });
        var tag2 = await _domainFacade.CreateTag(new Tag { Name = "searchtest2" });
        var tag3 = await _domainFacade.CreateTag(new Tag { Name = "differenttag" });
        
        // Create a persona and topics to link tags to (tags only appear in search if linked to topics)
        var persona = await _domainFacade.CreatePersona(new Persona { DisplayName = $"TestPersona{DateTime.Now.Ticks}" });
        var generalCategoryId = GetGeneralCategoryId();
        
        var topic1 = await _domainFacade.CreateTopic(new Topic 
        { 
            Name = $"TestTopic1{DateTime.Now.Ticks}", 
            CategoryId = generalCategoryId,
            PersonaId = persona.Id,
            ContentUrl = "https://example.com/test1"
        });
        var topic2 = await _domainFacade.CreateTopic(new Topic 
        { 
            Name = $"TestTopic2{DateTime.Now.Ticks}", 
            CategoryId = generalCategoryId,
            PersonaId = persona.Id,
            ContentUrl = "https://example.com/test2"
        });
        
        // Link tags to topics
        await _domainFacade.AddTagToTopic(topic1.Id, tag1.Name);
        await _domainFacade.AddTagToTopic(topic2.Id, tag2.Name);
        await _domainFacade.AddTagToTopic(topic1.Id, tag3.Name);

        // Act
        var result = await _domainFacade.SearchTags("searchtest", 1, 10);

        // Assert
        Assert.IsNotNull(result, "Expected search results, but got null");
        Assert.AreEqual(2, result.TotalCount, $"Expected 2 matching tags, but got {result.TotalCount}");
        Assert.IsTrue(result.Items.Any(t => t.Name == "searchtest1"), "Expected to find 'searchtest1'");
        Assert.IsTrue(result.Items.Any(t => t.Name == "searchtest2"), "Expected to find 'searchtest2'");
        Console.WriteLine($"Search returned {result.TotalCount} tags");
    }
    
    private Guid GetGeneralCategoryId()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = "SELECT id FROM categories WHERE name = 'General' LIMIT 1";
        var categoryId = connection.QueryFirstOrDefault<Guid?>(sql);
        if (categoryId == null)
        {
            throw new InvalidOperationException("General category not found in database. Ensure seed data is loaded.");
        }
        return categoryId.Value;
    }

    [TestMethod]
    public async Task SearchTags_NoSearchTerm_ReturnsAllTags()
    {
        // Arrange - Get initial count before creating test tags
        // Note: SearchTags only returns tags that are associated with at least one topic
        var initialResult = await _domainFacade.SearchTags(null, 1, 100);
        var initialCount = initialResult?.TotalCount ?? 0;
        
        // Create a persona and topic first (tags must be associated with a topic to appear in search)
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var persona = await _domainFacade.CreatePersona(new Persona { DisplayName = $"TestPersona{uniqueId}" });
        var categoryId = GetGeneralCategoryId();
        
        var topic = new Topic
        {
            Name = $"TestTopicForTags{uniqueId}",
            Description = "Test topic for tag search",
            CategoryId = categoryId,
            PersonaId = persona.Id,
            ContentUrl = "https://example.com/test"
        };
        var createdTopic = await _domainFacade.CreateTopic(topic);
        
        // Create tags by associating them with the topic (this creates tags if they don't exist)
        var tagNames = new[] { $"searchtesttag{uniqueId}a", $"searchtesttag{uniqueId}b", $"searchtesttag{uniqueId}c" };
        await _domainFacade.UpdateTopicTags(createdTopic.Id, tagNames, null);

        // Act
        var result = await _domainFacade.SearchTags(null, 1, 100);

        // Assert
        Assert.IsNotNull(result, "Expected search results, but got null");
        Assert.IsTrue(result.TotalCount >= initialCount + 3, $"Expected at least {initialCount + 3} tags after creating 3, but got {result.TotalCount}");
        Console.WriteLine($"Search returned {result.TotalCount} total tags (was {initialCount} before test)");
    }

    [TestMethod]
    public async Task DeleteTag_ExistingTag_ReturnsTrue()
    {
        // Arrange
        var tag = new Tag { Name = "tagtoDelete" };
        var created = await _domainFacade.CreateTag(tag);

        // Act
        var result = await _domainFacade.DeleteTag(created.Id);

        // Assert
        Assert.IsTrue(result, "Expected tag to be deleted successfully");
        
        // Verify deletion
        var deletedTag = await _domainFacade.GetTagById(created.Id);
        Assert.IsNull(deletedTag, "Expected tag to be null after deletion, but it still exists");
        Console.WriteLine($"Tag deleted successfully: {created.Id}");
    }

    [TestMethod]
    public async Task DeleteTag_NonExistingTag_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteTag(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Expected false for deleting non-existing tag, but got true");
        Console.WriteLine("Correctly returned false for non-existing tag deletion");
    }

    [TestMethod]
    public async Task GetOrCreateTag_NewTag_CreatesAndReturnsTag()
    {
        // Arrange
        var tagName = "NewTag";

        // Act
        var result = await _domainFacade.GetOrCreateTag(tagName, "testuser");

        // Assert
        Assert.IsNotNull(result, "Expected tag to be created, but got null");
        Assert.AreEqual("newtag", result.Name, $"Expected tag name 'newtag', but got {result.Name}");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Expected a valid Guid for tag ID");
        Console.WriteLine($"Tag created successfully: ID={result.Id}, Name={result.Name}");
    }

    [TestMethod]
    public async Task GetOrCreateTag_ExistingTag_ReturnsExistingTag()
    {
        // Arrange
        var tag = new Tag { Name = "ExistingTag" };
        var created = await _domainFacade.CreateTag(tag);

        // Act
        var result = await _domainFacade.GetOrCreateTag("ExistingTag", "testuser");

        // Assert
        Assert.IsNotNull(result, "Expected to get existing tag, but got null");
        Assert.AreEqual(created.Id, result.Id, $"Expected existing tag ID {created.Id}, but got {result.Id}");
        Assert.AreEqual("existingtag", result.Name, $"Expected tag name 'existingtag', but got {result.Name}");
        Console.WriteLine($"Returned existing tag: ID={result.Id}");
    }
}

