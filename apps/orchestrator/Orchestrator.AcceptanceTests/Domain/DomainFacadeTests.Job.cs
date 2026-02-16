using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Job operations using real DomainFacade.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsJob
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
            Name = Truncate($"TestOrg_Job_{Guid.NewGuid():N}", 50),
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

    private async Task<Job> CreateTestJobAsync(string suffix = "")
    {
        var job = new Job
        {
            GroupId = _testGroupId,
            ExternalJobId = Truncate($"ext_{Guid.NewGuid():N}", 50),
            Title = Truncate($"TestJob{suffix}_{Guid.NewGuid():N}", 80),
            Status = "active",
            Description = null,
            Location = null
        };
        var result = await _domainFacade.CreateJob(job);
        Assert.IsNotNull(result, "Failed to create test Job");
        return result;
    }

    [TestMethod]
    public async Task CreateJob_ValidData_ReturnsCreatedJob()
    {
        var job = new Job
        {
            GroupId = _testGroupId,
            ExternalJobId = Truncate($"ext_{Guid.NewGuid():N}", 50),
            Title = Truncate($"Job_{Guid.NewGuid():N}", 80),
            Status = "active"
        };

        var result = await _domainFacade.CreateJob(job);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(job.GroupId, result.GroupId);
        Assert.AreEqual(job.ExternalJobId, result.ExternalJobId);
        Assert.AreEqual(job.Title, result.Title);
    }

    [TestMethod]
    public async Task CreateJob_InvalidData_ThrowsValidationException()
    {
        var job = new Job
        {
            GroupId = _testGroupId,
            ExternalJobId = "",
            Title = "",
            Status = "active"
        };

        await Assert.ThrowsExceptionAsync<JobValidationException>(() =>
            _domainFacade.CreateJob(job), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task GetJobById_ExistingId_ReturnsJob()
    {
        var created = await CreateTestJobAsync();

        var result = await _domainFacade.GetJobById(created.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Title, result.Title);
    }

    [TestMethod]
    public async Task GetJobById_NonExistingId_ReturnsNull()
    {
        var result = await _domainFacade.GetJobById(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetJobByExternalId_Existing_ReturnsJob()
    {
        var created = await CreateTestJobAsync();

        var result = await _domainFacade.GetJobByExternalId(_testGroupId, created.ExternalJobId);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetJobByExternalId_NonExisting_ReturnsNull()
    {
        var result = await _domainFacade.GetJobByExternalId(_testGroupId, "nonexistent_ext_id_12345");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetOrCreateJob_New_CreatesAndReturnsJob()
    {
        var externalId = Truncate($"getorcreate_{Guid.NewGuid():N}", 50);
        var title = Truncate($"GetOrCreate_{Guid.NewGuid():N}", 80);

        var result = await _domainFacade.GetOrCreateJob(_testGroupId, externalId, title, null, null);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(externalId, result.ExternalJobId);
        Assert.AreEqual(title, result.Title);
    }

    [TestMethod]
    public async Task GetOrCreateJob_Existing_ReturnsExistingJob()
    {
        var created = await CreateTestJobAsync();
        var externalId = created.ExternalJobId;
        var title = created.Title;

        var result = await _domainFacade.GetOrCreateJob(_testGroupId, externalId, "Different Title", null, null);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id, "Should return existing job, not create duplicate");
        Assert.AreEqual(title, result.Title);
    }

    [TestMethod]
    public async Task SearchJobs_WithResults_ReturnsPaginatedList()
    {
        var job1 = await CreateTestJobAsync("1");
        var job2 = await CreateTestJobAsync("2");

        var result = await _domainFacade.SearchJobs(_testGroupId, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 jobs, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchJobs_NoResults_ReturnsEmptyList()
    {
        var result = await _domainFacade.SearchJobs(_testGroupId, "NonExistentTitleXYZ123", null, 1, 10);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.IsFalse(result.Items.Any());
    }

    [TestMethod]
    public async Task UpdateJob_ValidData_UpdatesSuccessfully()
    {
        var job = await CreateTestJobAsync();
        job.Title = Truncate($"Updated_{Guid.NewGuid():N}", 80);
        job.Status = "closed";

        var result = await _domainFacade.UpdateJob(job);

        Assert.IsNotNull(result);
        Assert.AreEqual(job.Title, result.Title);
        Assert.AreEqual("closed", result.Status);
    }

    [TestMethod]
    public async Task UpdateJob_InvalidData_ThrowsValidationException()
    {
        var job = await CreateTestJobAsync();
        job.Title = "";

        await Assert.ThrowsExceptionAsync<JobValidationException>(() =>
            _domainFacade.UpdateJob(job), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteJob_ExistingId_DeletesSuccessfully()
    {
        var job = await CreateTestJobAsync();
        var id = job.Id;

        var result = await _domainFacade.DeleteJob(id);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetJobById(id));
    }

    [TestMethod]
    public async Task DeleteJob_NonExistingId_ReturnsFalse()
    {
        var result = await _domainFacade.DeleteJob(Guid.NewGuid());
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task JobLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var job = await CreateTestJobAsync("Lifecycle");
        var createdId = job.Id;

        var retrieved = await _domainFacade.GetJobById(createdId);
        Assert.IsNotNull(retrieved);

        retrieved.Title = Truncate($"UpdatedLifecycle_{Guid.NewGuid():N}", 80);
        var updated = await _domainFacade.UpdateJob(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchJobs(_testGroupId, updated.Title, null, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0);

        Assert.IsTrue(await _domainFacade.DeleteJob(createdId));
        Assert.IsNull(await _domainFacade.GetJobById(createdId));
    }
}
