using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Interview operations using real DomainFacade.
/// Depends on Group, Agent, Job, Applicant.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsInterview
{
    private DomainFacade _domainFacade = null!;
    private Guid _testGroupId;
    private Guid _testAgentId;
    private Guid _testJobId;
    private Guid _testApplicantId;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = Truncate($"TestOrg_Interview_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testGroupId = group.Id;

        var agent = await _domainFacade.CreateAgent(new Agent
        {
            GroupId = _testGroupId,
            DisplayName = Truncate($"TestAgent_Interview_{Guid.NewGuid():N}", 80),
            ProfileImageUrl = null
        });
        _testAgentId = agent.Id;

        var job = new Job
        {
            GroupId = _testGroupId,
            ExternalJobId = Truncate($"ext_job_{Guid.NewGuid():N}", 50),
            Title = Truncate($"TestJob_Interview_{Guid.NewGuid():N}", 80),
            Status = "active"
        };
        var createdJob = await _domainFacade.CreateJob(job);
        _testJobId = createdJob.Id;

        var applicant = new Applicant
        {
            GroupId = _testGroupId,
            ExternalApplicantId = Truncate($"ext_app_{Guid.NewGuid():N}", 50),
            FirstName = "Interview",
            LastName = "Test",
            Email = Truncate($"interview_test_{Guid.NewGuid():N}", 20) + "@example.com"
        };
        var createdApplicant = await _domainFacade.CreateApplicant(applicant);
        _testApplicantId = createdApplicant.Id;
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

    private async Task<Interview> CreateTestInterviewAsync(string suffix = "")
    {
        var interview = new Interview
        {
            JobId = _testJobId,
            ApplicantId = _testApplicantId,
            AgentId = _testAgentId,
            Token = Truncate($"token_{Guid.NewGuid():N}", 50),
            Status = InterviewStatus.Pending,
            InterviewType = InterviewType.Voice
        };
        var result = await _domainFacade.CreateInterview(interview);
        Assert.IsNotNull(result, "Failed to create test Interview");
        return result;
    }

    [TestMethod]
    public async Task CreateInterview_ValidData_ReturnsCreatedInterview()
    {
        var interview = new Interview
        {
            JobId = _testJobId,
            ApplicantId = _testApplicantId,
            AgentId = _testAgentId,
            Token = Truncate($"token_{Guid.NewGuid():N}", 50),
            Status = InterviewStatus.Pending,
            InterviewType = InterviewType.Voice
        };

        var result = await _domainFacade.CreateInterview(interview);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(interview.Token, result.Token);
        Assert.AreEqual(InterviewStatus.Pending, result.Status);
    }

    [TestMethod]
    public async Task GetInterviewById_ExistingId_ReturnsInterview()
    {
        var created = await CreateTestInterviewAsync();

        var result = await _domainFacade.GetInterviewById(created.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Token, result.Token);
    }

    [TestMethod]
    public async Task GetInterviewById_NonExistingId_ReturnsNull()
    {
        var result = await _domainFacade.GetInterviewById(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetInterviewByToken_ExistingToken_ReturnsInterview()
    {
        var created = await CreateTestInterviewAsync();

        var result = await _domainFacade.GetInterviewByToken(created.Token);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetInterviewByToken_NonExistingToken_ReturnsNull()
    {
        var result = await _domainFacade.GetInterviewByToken("nonexistent_token_12345");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SearchInterviews_WithResults_ReturnsPaginatedList()
    {
        var i1 = await CreateTestInterviewAsync("1");
        var i2 = await CreateTestInterviewAsync("2");

        var result = await _domainFacade.SearchInterviews(null, _testJobId, null, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 interviews, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchInterviews_NoResults_ReturnsEmptyList()
    {
        var result = await _domainFacade.SearchInterviews(null, Guid.NewGuid(), null, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.IsFalse(result.Items.Any());
    }

    [TestMethod]
    public async Task UpdateInterview_ValidData_UpdatesSuccessfully()
    {
        var interview = await CreateTestInterviewAsync();
        interview.Status = InterviewStatus.InProgress;

        var result = await _domainFacade.UpdateInterview(interview);

        Assert.IsNotNull(result);
        Assert.AreEqual(InterviewStatus.InProgress, result.Status);
    }

    [TestMethod]
    public async Task StartInterview_ExistingId_UpdatesStatus()
    {
        var interview = await CreateTestInterviewAsync();

        var result = await _domainFacade.StartInterview(interview.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(InterviewStatus.InProgress, result.Status);
    }

    [TestMethod]
    public async Task DeleteInterview_ExistingId_DeletesSuccessfully()
    {
        var interview = await CreateTestInterviewAsync();
        var id = interview.Id;

        var result = await _domainFacade.DeleteInterview(id);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetInterviewById(id));
    }

    [TestMethod]
    public async Task DeleteInterview_NonExistingId_ReturnsFalse()
    {
        var result = await _domainFacade.DeleteInterview(Guid.NewGuid());
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task InterviewLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var interview = await CreateTestInterviewAsync("Lifecycle");
        var createdId = interview.Id;

        var retrieved = await _domainFacade.GetInterviewById(createdId);
        Assert.IsNotNull(retrieved);

        retrieved.Status = InterviewStatus.InProgress;
        var updated = await _domainFacade.UpdateInterview(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchInterviews(null, _testJobId, _testApplicantId, _testAgentId, InterviewStatus.InProgress, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0);

        Assert.IsTrue(await _domainFacade.DeleteInterview(createdId));
        Assert.IsNull(await _domainFacade.GetInterviewById(createdId));
    }
}
