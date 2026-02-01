using System;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Topic Feed operations with search and sorting functionality
/// 
/// TEST APPROACH:
/// - Uses real DomainFacade and DataFacade instances for acceptance tests
/// - Tests the actual integration between layers
/// - No external mocking frameworks used
/// - ServiceLocatorForAcceptanceTesting provides real implementations
/// - Tests clean up their own data to ensure complete independence
/// </summary>
[TestClass]
public class DomainFacadeTestsTopicFeed
{
    private DomainFacade _domainFacade;
    private string _connectionString;
    private Guid _generalCategoryId;
    private Guid _testPersonaId;
    private const string TestContentUrl = "file://test-content.txt";

    public DomainFacadeTestsTopicFeed()
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
        // Try searching for General category (case-insensitive search with broader criteria)
        var result = _domainFacade.SearchCategories(null, "General", null, 1, 100).GetAwaiter().GetResult();
        var generalCategory = result.Items.FirstOrDefault(c => c.Name.Equals("General", StringComparison.OrdinalIgnoreCase));
        
        if (generalCategory != null)
        {
            return generalCategory.Id;
        }
        
        // If General category doesn't exist, create it
        try
        {
            var category = new Category { Name = "General", IsActive = true };
            var created = _domainFacade.CreateCategory(category).GetAwaiter().GetResult();
            return created.Id;
        }
        catch (CategoryDuplicateNameException)
        {
            // If it already exists (race condition), do a more exhaustive search
            result = _domainFacade.SearchCategories(null, null, null, 1, 100).GetAwaiter().GetResult();
            generalCategory = result.Items.FirstOrDefault(c => c.Name.Equals("General", StringComparison.OrdinalIgnoreCase));
            
            if (generalCategory != null)
            {
                return generalCategory.Id;
            }
            
            // Last resort: throw a more descriptive error
            throw new InvalidOperationException("Unable to find or create General category for tests. This may indicate a database seeding issue.");
        }
    }

    [TestMethod]
    public async Task SearchTopicsFeed_WithSearchTerm_ReturnsMatchingTopics()
    {
        // Arrange - Create topics with distinct names
        var topic1 = new Topic
        {
            Name = "SearchTestAI Machine Learning",
            Description = "About artificial intelligence",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var topic2 = new Topic
        {
            Name = "SearchTestCooking Recipes",
            Description = "Delicious food",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var topic3 = new Topic
        {
            Name = "SearchTestAI Development",
            Description = "Building AI systems",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };

        await _domainFacade.CreateTopic(topic1);
        await _domainFacade.CreateTopic(topic2);
        await _domainFacade.CreateTopic(topic3);

        // Act - Search for "AI" in name or description
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: null,
            tagIds: null,
            searchTerm: "AI",
            sortBy: null,
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(2, feedResult.Items.Count(), 
            $"Expected 2 topics with 'AI' in name/description, but got {feedResult.Items.Count()}");
        Assert.IsTrue(feedResult.Items.All(t => t.Name.Contains("AI") || (t.Description != null && t.Description.Contains("AI"))),
            "All returned topics should contain 'AI' in name or description");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_SortByRecent_ReturnsNewestFirst()
    {
        // Arrange - Create topics with slight time delays
        var topic1 = new Topic
        {
            Name = "SortTestOldest",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic1);
        await Task.Delay(100); // Small delay to ensure different timestamps

        var topic2 = new Topic
        {
            Name = "SortTestMiddle",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic2);
        await Task.Delay(100);

        var topic3 = new Topic
        {
            Name = "SortTestNewest",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic3);

        // Act - Sort by recent
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: null,
            tagIds: null,
            searchTerm: "SortTest",
            sortBy: "recent",
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(3, feedResult.Items.Count(), 
            $"Expected 3 topics, but got {feedResult.Items.Count()}");
        var items = feedResult.Items.ToList();
        Assert.AreEqual("SortTestNewest", items[0].Name, 
            $"First topic should be 'SortTestNewest', but got '{items[0].Name}'");
        Assert.AreEqual("SortTestOldest", items[2].Name, 
            $"Last topic should be 'SortTestOldest', but got '{items[2].Name}'");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_SortByPopular_ReturnsMostChatCountFirst()
    {
        // Arrange - Create topics
        var topic1 = new Topic
        {
            Name = "PopularTestLeast",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created1 = await _domainFacade.CreateTopic(topic1);

        var topic2 = new Topic
        {
            Name = "PopularTestMost",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created2 = await _domainFacade.CreateTopic(topic2);

        var topic3 = new Topic
        {
            Name = "PopularTestMiddle",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created3 = await _domainFacade.CreateTopic(topic3);

        // Save training content for topics so they can be used in chats
        await _domainFacade.SaveTopicTrainingContent(created1.Id, "Test training content for PopularTestLeast");
        await _domainFacade.SaveTopicTrainingContent(created2.Id, "Test training content for PopularTestMost");
        await _domainFacade.SaveTopicTrainingContent(created3.Id, "Test training content for PopularTestMiddle");

        // Create chats to establish popularity (chat_count)
        var testUserId = Guid.NewGuid();
        var chat1 = await _domainFacade.CreateChat(new Chat { PersonaId = _testPersonaId, UserId = testUserId, LastMessageAt = DateTime.UtcNow });
        var chat2 = await _domainFacade.CreateChat(new Chat { PersonaId = _testPersonaId, UserId = testUserId, LastMessageAt = DateTime.UtcNow });
        var chat3 = await _domainFacade.CreateChat(new Chat { PersonaId = _testPersonaId, UserId = testUserId, LastMessageAt = DateTime.UtcNow });
        var chat4 = await _domainFacade.CreateChat(new Chat { PersonaId = _testPersonaId, UserId = testUserId, LastMessageAt = DateTime.UtcNow });

        // Associate chats with topics (topic2 = 3 chats, topic3 = 1 chat, topic1 = 0 chats)
        await _domainFacade.AddTopicToChat(chat1.Id, created2.Id);
        await _domainFacade.AddTopicToChat(chat2.Id, created2.Id);
        await _domainFacade.AddTopicToChat(chat3.Id, created2.Id);
        await _domainFacade.AddTopicToChat(chat4.Id, created3.Id);

        // Act - Sort by popular (chat_count)
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: null,
            tagIds: null,
            searchTerm: "PopularTest",
            sortBy: "popular",
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(3, feedResult.Items.Count(), 
            $"Expected 3 topics, but got {feedResult.Items.Count()}");
        var items = feedResult.Items.ToList();
        Assert.AreEqual("PopularTestMost", items[0].Name, 
            $"First topic should be 'PopularTestMost' (3 chats), but got '{items[0].Name}' with {items[0].ChatCount} chats");
        Assert.AreEqual(3, items[0].ChatCount, 
            $"Most popular topic should have 3 chats, but got {items[0].ChatCount}");
        Assert.AreEqual("PopularTestLeast", items[2].Name, 
            $"Last topic should be 'PopularTestLeast' (0 chats), but got '{items[2].Name}'");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        // Arrange - Get or create Technology category
        var searchResult = await _domainFacade.SearchCategories(null, null, null, 1, 100);
        var techCategory = searchResult.Items.FirstOrDefault(c => c.Name.Equals("Technology", StringComparison.OrdinalIgnoreCase));
        
        if (techCategory == null)
        {
            try
            {
                techCategory = await _domainFacade.CreateCategory(new Category { Name = "Technology", IsActive = true });
            }
            catch (CategoryDuplicateNameException)
            {
                // Race condition: category was created between search and create, search again
                searchResult = await _domainFacade.SearchCategories(null, null, null, 1, 100);
                techCategory = searchResult.Items.FirstOrDefault(c => c.Name.Equals("Technology", StringComparison.OrdinalIgnoreCase));
                
                if (techCategory == null)
                {
                    throw new InvalidOperationException("Unable to find or create Technology category for test");
                }
            }
        }
        
        // Create topics in different categories
        var topic1 = new Topic
        {
            Name = "CategoryTestGeneral",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic1);

        var topic2 = new Topic
        {
            Name = "CategoryTestTech",
            PersonaId = _testPersonaId,
            CategoryId = techCategory.Id,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic2);

        // Act - Filter by Technology category
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: techCategory.Id,
            tagIds: null,
            searchTerm: "CategoryTest",
            sortBy: null,
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(1, feedResult.Items.Count(), 
            $"Expected 1 topic in Technology category, but got {feedResult.Items.Count()}");
        Assert.AreEqual("CategoryTestTech", feedResult.Items.First().Name,
            $"Expected topic 'CategoryTestTech', but got '{feedResult.Items.First().Name}'");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_WithTagFilter_ReturnsOnlyTopicsWithTag()
    {
        // Arrange - Create topics and add tags
        var topic1 = new Topic
        {
            Name = "TagTestAI",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created1 = await _domainFacade.CreateTopic(topic1);
        var aiTag = await _domainFacade.AddTagToTopic(created1.Id, "artificial-intelligence", "test-user");

        var topic2 = new Topic
        {
            Name = "TagTestCooking",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created2 = await _domainFacade.CreateTopic(topic2);
        await _domainFacade.AddTagToTopic(created2.Id, "cooking", "test-user");

        var topic3 = new Topic
        {
            Name = "TagTestBoth",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created3 = await _domainFacade.CreateTopic(topic3);
        await _domainFacade.AddTagToTopic(created3.Id, "artificial-intelligence", "test-user");
        await _domainFacade.AddTagToTopic(created3.Id, "cooking", "test-user");

        // Act - Filter by AI tag
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: null,
            tagIds: new[] { aiTag.Id },
            searchTerm: "TagTest",
            sortBy: null,
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(2, feedResult.Items.Count(), 
            $"Expected 2 topics with 'artificial-intelligence' tag, but got {feedResult.Items.Count()}");
        Assert.IsTrue(feedResult.Items.Any(t => t.Name == "TagTestAI"),
            "Expected to find 'TagTestAI' topic");
        Assert.IsTrue(feedResult.Items.Any(t => t.Name == "TagTestBoth"),
            "Expected to find 'TagTestBoth' topic");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_CombinedFilters_ReturnsCorrectResults()
    {
        // Arrange - Create topics with various attributes
        var topic1 = new Topic
        {
            Name = "CombinedTestAI Machine Learning",
            Description = "Advanced AI concepts",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created1 = await _domainFacade.CreateTopic(topic1);
        var mlTag = await _domainFacade.AddTagToTopic(created1.Id, "machine-learning", "test-user");

        var topic2 = new Topic
        {
            Name = "CombinedTestAI Basics",
            Description = "Introduction to AI",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        var created2 = await _domainFacade.CreateTopic(topic2);
        await _domainFacade.AddTagToTopic(created2.Id, "machine-learning", "test-user");

        var topic3 = new Topic
        {
            Name = "CombinedTestCooking",
            Description = "AI-powered cooking",
            PersonaId = _testPersonaId,
            CategoryId = _generalCategoryId,
            ContentUrl = TestContentUrl,
            CreatedBy = "test-user"
        };
        await _domainFacade.CreateTopic(topic3);

        // Act - Combined: search term "AI" + tag filter + category filter
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: _generalCategoryId,
            tagIds: new[] { mlTag.Id },
            searchTerm: "Machine",
            sortBy: "recent",
            pageNumber: 1,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(1, feedResult.Items.Count(), 
            $"Expected 1 topic matching all filters (search='Machine', tag='machine-learning', category=General), but got {feedResult.Items.Count()}");
        Assert.AreEqual("CombinedTestAI Machine Learning", feedResult.Items.First().Name,
            $"Expected 'CombinedTestAI Machine Learning', but got '{feedResult.Items.First().Name}'");
    }

    [TestMethod]
    public async Task SearchTopicsFeed_Pagination_ReturnsCorrectPage()
    {
        // Arrange - Create multiple topics
        for (int i = 1; i <= 15; i++)
        {
            var topic = new Topic
            {
                Name = $"PaginationTest{i:D2}",
                PersonaId = _testPersonaId,
                CategoryId = _generalCategoryId,
                ContentUrl = TestContentUrl,
                CreatedBy = "test-user"
            };
            await _domainFacade.CreateTopic(topic);
            await Task.Delay(10); // Small delay for consistent ordering
        }

        // Act - Get page 2 with 10 items per page
        var (feedResult, _) = await _domainFacade.SearchTopicsFeed(
            categoryId: null,
            tagIds: null,
            searchTerm: "PaginationTest",
            sortBy: "recent",
            pageNumber: 2,
            pageSize: 10
        );

        // Assert
        Assert.AreEqual(5, feedResult.Items.Count(), 
            $"Expected 5 topics on page 2 (15 total, 10 per page), but got {feedResult.Items.Count()}");
        Assert.AreEqual(15, feedResult.TotalCount,
            $"Expected total count of 15, but got {feedResult.TotalCount}");
        Assert.AreEqual(2, feedResult.PageNumber,
            $"Expected page number 2, but got {feedResult.PageNumber}");
    }
}

