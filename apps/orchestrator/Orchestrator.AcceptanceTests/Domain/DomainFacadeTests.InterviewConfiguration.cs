using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for InterviewConfiguration operations using real DomainFacade.
/// Depends on Group and Agent.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsInterviewConfiguration
{
    private DomainFacade _domainFacade = null!;
    private Guid _testGroupId;
    private Guid _testAgentId;
    private Guid _testInterviewGuideId;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = Truncate($"TestOrg_IC_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testGroupId = group.Id;

        var agent = await _domainFacade.CreateAgent(new Agent
        {
            GroupId = _testGroupId,
            DisplayName = Truncate($"TestAgent_IC_{Guid.NewGuid():N}", 80),
            ProfileImageUrl = null
        });
        _testAgentId = agent.Id;

        var guide = await _domainFacade.CreateInterviewGuide(new InterviewGuide
        {
            GroupId = _testGroupId,
            Name = Truncate($"TestGuide_IC_{Guid.NewGuid():N}", 80),
            IsActive = true
        });
        _testInterviewGuideId = guide.Id;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            TestDataCleanup.CleanupAllTestData();
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

    private async Task<InterviewConfiguration> CreateTestInterviewConfigurationAsync(string suffix = "")
    {
        var config = new InterviewConfiguration
        {
            GroupId = _testGroupId,
            InterviewGuideId = _testInterviewGuideId,
            AgentId = _testAgentId,
            Name = Truncate($"TestConfig{suffix}_{Guid.NewGuid():N}", 80),
            Description = null,
            ScoringRubric = null,
            IsActive = true
        };
        var result = await _domainFacade.CreateInterviewConfiguration(config);
        Assert.IsNotNull(result, "Failed to create test InterviewConfiguration");
        return result;
    }

    [TestMethod]
    public async Task CreateInterviewConfiguration_ValidData_ReturnsCreated()
    {
        var config = new InterviewConfiguration
        {
            GroupId = _testGroupId,
            InterviewGuideId = _testInterviewGuideId,
            AgentId = _testAgentId,
            Name = Truncate($"Config_{Guid.NewGuid():N}", 80),
            IsActive = true
        };

        var result = await _domainFacade.CreateInterviewConfiguration(config);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(config.Name, result.Name);
        Assert.AreEqual(config.AgentId, result.AgentId);
    }

    [TestMethod]
    public async Task CreateInterviewConfiguration_InvalidData_ThrowsValidationException()
    {
        var config = new InterviewConfiguration
        {
            GroupId = _testGroupId,
            InterviewGuideId = _testInterviewGuideId,
            AgentId = _testAgentId,
            Name = "", // Required
            IsActive = true
        };

        await Assert.ThrowsExceptionAsync<InterviewConfigurationValidationException>(() =>
            _domainFacade.CreateInterviewConfiguration(config), "Should throw validation exception");
    }

    [TestMethod]
    public async Task GetInterviewConfigurationById_ExistingId_ReturnsConfiguration()
    {
        var created = await CreateTestInterviewConfigurationAsync();

        var result = await _domainFacade.GetInterviewConfigurationById(created.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Name, result.Name);
    }

    [TestMethod]
    public async Task GetInterviewConfigurationById_NonExistingId_ReturnsNull()
    {
        var result = await _domainFacade.GetInterviewConfigurationById(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetInterviewConfigurationByIdWithQuestions_WithQuestions_ReturnsConfigurationWithQuestions()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var question = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "Test question?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            FollowUpsEnabled = true,
            MaxFollowUps = 2,
            CreatedAt = DateTime.UtcNow
        };
        var added = await _domainFacade.AddInterviewConfigurationQuestion(question);

        var result = await _domainFacade.GetInterviewConfigurationByIdWithQuestions(config.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(config.Id, result.Id);
        Assert.IsTrue(result.Questions != null && result.Questions.Count >= 1, "Should have at least one question");
    }

    [TestMethod]
    public async Task SearchInterviewConfigurations_WithResults_ReturnsPaginatedList()
    {
        var c1 = await CreateTestInterviewConfigurationAsync("1");
        var c2 = await CreateTestInterviewConfigurationAsync("2");

        var result = await _domainFacade.SearchInterviewConfigurations(_testGroupId, _testAgentId, null, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 configs, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchInterviewConfigurations_NoResults_ReturnsEmptyList()
    {
        var result = await _domainFacade.SearchInterviewConfigurations(_testGroupId, _testAgentId, "NonExistentNameXYZ123", null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.IsFalse(result.Items.Any());
    }

    [TestMethod]
    public async Task UpdateInterviewConfiguration_ValidData_UpdatesSuccessfully()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        config.Name = Truncate($"Updated_{Guid.NewGuid():N}", 80);
        config.Description = "Updated description";

        var result = await _domainFacade.UpdateInterviewConfiguration(config);

        Assert.IsNotNull(result);
        Assert.AreEqual(config.Name, result.Name);
        Assert.AreEqual(config.Description, result.Description);
    }

    [TestMethod]
    public async Task UpdateInterviewConfiguration_InvalidData_ThrowsValidationException()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        config.Name = "";

        await Assert.ThrowsExceptionAsync<InterviewConfigurationValidationException>(() =>
            _domainFacade.UpdateInterviewConfiguration(config), "Should throw validation exception");
    }

    [TestMethod]
    public async Task AddInterviewConfigurationQuestion_ValidQuestion_ReturnsQuestion()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var question = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "What is your experience?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _domainFacade.AddInterviewConfigurationQuestion(question);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(question.Question, result.Question);
    }

    [TestMethod]
    public async Task GetInterviewConfigurationQuestions_ReturnsQuestions()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var q = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "Follow up?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            CreatedAt = DateTime.UtcNow
        };
        var added = await _domainFacade.AddInterviewConfigurationQuestion(q);

        var questions = await _domainFacade.GetInterviewConfigurationQuestions(config.Id);

        Assert.IsNotNull(questions);
        Assert.IsTrue(questions.Count >= 1);
    }

    [TestMethod]
    public async Task UpdateInterviewConfigurationQuestion_ValidData_UpdatesSuccessfully()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var question = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "Original?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            CreatedAt = DateTime.UtcNow
        };
        var added = await _domainFacade.AddInterviewConfigurationQuestion(question);

        added.Question = "Updated question?";
        var result = await _domainFacade.UpdateInterviewConfigurationQuestion(added);

        Assert.IsNotNull(result);
        Assert.AreEqual("Updated question?", result.Question);
    }

    [TestMethod]
    public async Task GetInterviewConfigurationQuestionById_Existing_ReturnsQuestion()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var question = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "Get by id?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            CreatedAt = DateTime.UtcNow
        };
        var added = await _domainFacade.AddInterviewConfigurationQuestion(question);

        var result = await _domainFacade.GetInterviewConfigurationQuestionById(added.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(added.Id, result.Id);
    }

    [TestMethod]
    public async Task DeleteInterviewConfigurationQuestion_Existing_DeletesSuccessfully()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var question = new InterviewConfigurationQuestion
        {
            InterviewConfigurationId = config.Id,
            Question = "To delete?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            CreatedAt = DateTime.UtcNow
        };
        var added = await _domainFacade.AddInterviewConfigurationQuestion(question);

        var result = await _domainFacade.DeleteInterviewConfigurationQuestion(added.Id);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetInterviewConfigurationQuestionById(added.Id));
    }

    [TestMethod]
    public async Task DeleteInterviewConfiguration_ExistingId_DeletesSuccessfully()
    {
        var config = await CreateTestInterviewConfigurationAsync();
        var id = config.Id;

        var result = await _domainFacade.DeleteInterviewConfiguration(id, null);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetInterviewConfigurationById(id));
    }

    [TestMethod]
    public async Task InterviewConfigurationLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var config = await CreateTestInterviewConfigurationAsync("Lifecycle");
        var createdId = config.Id;

        var retrieved = await _domainFacade.GetInterviewConfigurationById(createdId);
        Assert.IsNotNull(retrieved);

        retrieved.Name = Truncate($"UpdatedLifecycle_{Guid.NewGuid():N}", 80);
        var updated = await _domainFacade.UpdateInterviewConfiguration(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchInterviewConfigurations(_testGroupId, _testAgentId, updated.Name, null, null, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0);

        Assert.IsTrue(await _domainFacade.DeleteInterviewConfiguration(createdId, null));
        Assert.IsNull(await _domainFacade.GetInterviewConfigurationById(createdId));
    }
}
