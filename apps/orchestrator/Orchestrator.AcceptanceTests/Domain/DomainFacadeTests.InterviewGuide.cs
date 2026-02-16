using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for InterviewGuide operations using real DomainFacade.
/// Depends on Group.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// All test group names MUST start with "TestOrg_".
/// </summary>
[TestClass]
public class DomainFacadeTestsInterviewGuide
{
    private DomainFacade _domainFacade = null!;
    private Guid _testGroupId;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = Truncate($"TestOrg_IG_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testGroupId = group.Id;
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

    private async Task<InterviewGuide> CreateTestInterviewGuideAsync(string suffix = "")
    {
        var guide = new InterviewGuide
        {
            GroupId = _testGroupId,
            Name = Truncate($"TestGuide{suffix}_{Guid.NewGuid():N}", 80),
            Description = "Test guide description",
            OpeningTemplate = "Hello {{applicantName}}! I'm {{agentName}}.",
            ClosingTemplate = "Thank you, {{applicantName}}!",
            ScoringRubric = "Score 1-10",
            IsActive = true
        };
        var result = await _domainFacade.CreateInterviewGuide(guide);
        Assert.IsNotNull(result, "Failed to create test InterviewGuide");
        return result;
    }

    [TestMethod]
    public async Task CreateInterviewGuide_ValidData_ReturnsCreated()
    {
        var guide = new InterviewGuide
        {
            GroupId = _testGroupId,
            Name = Truncate($"Guide_{Guid.NewGuid():N}", 80),
            Description = "A test guide",
            OpeningTemplate = "Hello {{applicantName}}!",
            ClosingTemplate = "Goodbye {{applicantName}}!",
            ScoringRubric = "Rubric text",
            IsActive = true
        };

        var result = await _domainFacade.CreateInterviewGuide(guide);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(guide.Name, result.Name);
        Assert.AreEqual(guide.OpeningTemplate, result.OpeningTemplate);
        Assert.AreEqual(guide.ClosingTemplate, result.ClosingTemplate);
        Assert.AreEqual(guide.ScoringRubric, result.ScoringRubric);
    }

    [TestMethod]
    public async Task CreateInterviewGuide_WithQuestions_ReturnsGuideWithQuestions()
    {
        var guide = new InterviewGuide
        {
            GroupId = _testGroupId,
            Name = Truncate($"GuideQ_{Guid.NewGuid():N}", 80),
            IsActive = true,
            Questions = new List<InterviewGuideQuestion>
            {
                new() { Question = "Tell me about yourself", ScoringWeight = 2.0m, ScoringGuidance = "Look for communication" },
                new() { Question = "Why this role?", ScoringWeight = 1.5m }
            }
        };

        var result = await _domainFacade.CreateInterviewGuide(guide);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Questions);
        Assert.AreEqual(2, result.Questions.Count);
        Assert.AreEqual("Tell me about yourself", result.Questions[0].Question);
        Assert.AreEqual(2.0m, result.Questions[0].ScoringWeight);
    }

    [TestMethod]
    public async Task CreateInterviewGuide_InvalidData_ThrowsValidationException()
    {
        var guide = new InterviewGuide
        {
            GroupId = _testGroupId,
            Name = "", // Required
            IsActive = true
        };

        await Assert.ThrowsExceptionAsync<InterviewGuideValidationException>(() =>
            _domainFacade.CreateInterviewGuide(guide), "Should throw validation exception");
    }

    [TestMethod]
    public async Task CreateInterviewGuide_MissingGroupId_ThrowsValidationException()
    {
        var guide = new InterviewGuide
        {
            GroupId = Guid.Empty,
            Name = "Test Guide",
            IsActive = true
        };

        await Assert.ThrowsExceptionAsync<InterviewGuideValidationException>(() =>
            _domainFacade.CreateInterviewGuide(guide), "Should throw validation exception for missing group ID");
    }

    [TestMethod]
    public async Task GetInterviewGuideById_ExistingId_ReturnsGuide()
    {
        var created = await CreateTestInterviewGuideAsync();

        var result = await _domainFacade.GetInterviewGuideById(created.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Name, result.Name);
        Assert.AreEqual(created.OpeningTemplate, result.OpeningTemplate);
    }

    [TestMethod]
    public async Task GetInterviewGuideById_NonExistingId_ReturnsNull()
    {
        var result = await _domainFacade.GetInterviewGuideById(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetInterviewGuideByIdWithQuestions_WithQuestions_ReturnsGuideWithQuestions()
    {
        var guide = await CreateTestInterviewGuideAsync();
        var question = new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "Test question?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m,
            FollowUpsEnabled = true,
            MaxFollowUps = 2
        };
        await _domainFacade.AddInterviewGuideQuestion(question);

        var result = await _domainFacade.GetInterviewGuideByIdWithQuestions(guide.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(guide.Id, result.Id);
        Assert.IsTrue(result.Questions != null && result.Questions.Count >= 1, "Should have at least one question");
    }

    [TestMethod]
    public async Task SearchInterviewGuides_WithResults_ReturnsPaginatedList()
    {
        await CreateTestInterviewGuideAsync("1");
        await CreateTestInterviewGuideAsync("2");

        var result = await _domainFacade.SearchInterviewGuides(_testGroupId, null, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 guides, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchInterviewGuides_NoResults_ReturnsEmptyList()
    {
        var result = await _domainFacade.SearchInterviewGuides(_testGroupId, "NonExistentNameXYZ123", null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.IsFalse(result.Items.Any());
    }

    [TestMethod]
    public async Task UpdateInterviewGuide_ValidData_UpdatesSuccessfully()
    {
        var guide = await CreateTestInterviewGuideAsync();
        guide.Name = Truncate($"Updated_{Guid.NewGuid():N}", 80);
        guide.Description = "Updated description";
        guide.OpeningTemplate = "Updated opening";
        guide.ClosingTemplate = "Updated closing";

        var result = await _domainFacade.UpdateInterviewGuide(guide);

        Assert.IsNotNull(result);
        Assert.AreEqual(guide.Name, result.Name);
        Assert.AreEqual(guide.Description, result.Description);
        Assert.AreEqual("Updated opening", result.OpeningTemplate);
        Assert.AreEqual("Updated closing", result.ClosingTemplate);
    }

    [TestMethod]
    public async Task UpdateInterviewGuide_InvalidData_ThrowsValidationException()
    {
        var guide = await CreateTestInterviewGuideAsync();
        guide.Name = "";

        await Assert.ThrowsExceptionAsync<InterviewGuideValidationException>(() =>
            _domainFacade.UpdateInterviewGuide(guide), "Should throw validation exception");
    }

    [TestMethod]
    public async Task UpdateInterviewGuideWithQuestions_ReplacesQuestions()
    {
        var guide = await CreateTestInterviewGuideAsync();
        await _domainFacade.AddInterviewGuideQuestion(new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "Original question",
            DisplayOrder = 0,
            ScoringWeight = 1.0m
        });

        var newQuestions = new List<InterviewGuideQuestion>
        {
            new() { Question = "Replacement Q1", ScoringWeight = 1.0m },
            new() { Question = "Replacement Q2", ScoringWeight = 2.0m }
        };

        var result = await _domainFacade.UpdateInterviewGuideWithQuestions(guide, newQuestions);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Questions.Count);
        Assert.AreEqual("Replacement Q1", result.Questions[0].Question);
        Assert.AreEqual("Replacement Q2", result.Questions[1].Question);
    }

    [TestMethod]
    public async Task AddInterviewGuideQuestion_ValidQuestion_ReturnsQuestion()
    {
        var guide = await CreateTestInterviewGuideAsync();
        var question = new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "What is your experience?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m
        };

        var result = await _domainFacade.AddInterviewGuideQuestion(question);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(question.Question, result.Question);
    }

    [TestMethod]
    public async Task GetInterviewGuideQuestions_ReturnsQuestions()
    {
        var guide = await CreateTestInterviewGuideAsync();
        await _domainFacade.AddInterviewGuideQuestion(new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "Q1?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m
        });

        var questions = await _domainFacade.GetInterviewGuideQuestions(guide.Id);

        Assert.IsNotNull(questions);
        Assert.IsTrue(questions.Count >= 1);
    }

    [TestMethod]
    public async Task UpdateInterviewGuideQuestion_ValidData_UpdatesSuccessfully()
    {
        var guide = await CreateTestInterviewGuideAsync();
        var question = new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "Original?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m
        };
        var added = await _domainFacade.AddInterviewGuideQuestion(question);

        added.Question = "Updated question?";
        var result = await _domainFacade.UpdateInterviewGuideQuestion(added);

        Assert.IsNotNull(result);
        Assert.AreEqual("Updated question?", result.Question);
    }

    [TestMethod]
    public async Task DeleteInterviewGuideQuestion_Existing_DeletesSuccessfully()
    {
        var guide = await CreateTestInterviewGuideAsync();
        var question = new InterviewGuideQuestion
        {
            InterviewGuideId = guide.Id,
            Question = "To delete?",
            DisplayOrder = 0,
            ScoringWeight = 1.0m
        };
        var added = await _domainFacade.AddInterviewGuideQuestion(question);

        var result = await _domainFacade.DeleteInterviewGuideQuestion(added.Id);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetInterviewGuideQuestionById(added.Id));
    }

    [TestMethod]
    public async Task DeleteInterviewGuide_ExistingId_DeletesSuccessfully()
    {
        var guide = await CreateTestInterviewGuideAsync();
        var id = guide.Id;

        var result = await _domainFacade.DeleteInterviewGuide(id, null);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetInterviewGuideById(id));
    }

    [TestMethod]
    public async Task InterviewGuideLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var guide = await CreateTestInterviewGuideAsync("Lifecycle");
        var createdId = guide.Id;

        // Get
        var retrieved = await _domainFacade.GetInterviewGuideById(createdId);
        Assert.IsNotNull(retrieved);

        // Update
        retrieved.Name = Truncate($"UpdatedLifecycle_{Guid.NewGuid():N}", 80);
        var updated = await _domainFacade.UpdateInterviewGuide(retrieved);
        Assert.IsNotNull(updated);

        // Search
        var searchResult = await _domainFacade.SearchInterviewGuides(_testGroupId, updated.Name, null, null, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0);

        // Delete
        Assert.IsTrue(await _domainFacade.DeleteInterviewGuide(createdId, null));
        Assert.IsNull(await _domainFacade.GetInterviewGuideById(createdId));
    }

    [TestMethod]
    public async Task InterviewGuide_UsedByInterviewConfiguration_WorksCorrectly()
    {
        // Create a guide
        var guide = await CreateTestInterviewGuideAsync("ConfigRef");

        // Create an agent
        var agent = await _domainFacade.CreateAgent(new Agent
        {
            GroupId = _testGroupId,
            DisplayName = Truncate($"TestAgent_IG_{Guid.NewGuid():N}", 80)
        });

        // Create a configuration that references the guide
        var config = await _domainFacade.CreateInterviewConfiguration(new InterviewConfiguration
        {
            GroupId = _testGroupId,
            InterviewGuideId = guide.Id,
            AgentId = agent.Id,
            Name = Truncate($"ConfigWithGuide_{Guid.NewGuid():N}", 80),
            IsActive = true
        });

        Assert.IsNotNull(config);
        Assert.AreEqual(guide.Id, config.InterviewGuideId);
    }
}
