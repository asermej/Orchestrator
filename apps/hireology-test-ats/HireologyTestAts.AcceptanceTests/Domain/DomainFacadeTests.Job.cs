using HireologyTestAts.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HireologyTestAts.AcceptanceTests.Domain;
using HireologyTestAts.AcceptanceTests.TestUtilities;

namespace HireologyTestAts.AcceptanceTests.Domain;

/// <summary>
/// Tests for Job operations using real DomainFacade.
/// Jobs are scoped to organizations. Test orgs MUST start with "TestOrg_".
/// </summary>
[TestClass]
public class DomainFacadeTestsJob
{
    private DomainFacade _domainFacade = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
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

    private async Task<(Group Group, Organization Org)> CreateTestOrgAsync()
    {
        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = $"TestGroup_{Guid.NewGuid():N}"[..50]
        });
        var org = await _domainFacade.CreateOrganization(new Organization
        {
            GroupId = group.Id,
            Name = $"TestOrg_{Guid.NewGuid():N}"[..50]
        });
        return (group, org);
    }

    [TestMethod]
    public async Task CreateJob_ValidData_ReturnsCreatedJob()
    {
        var (_, org) = await CreateTestOrgAsync();
        var job = new Job
        {
            ExternalJobId = $"ext-{Guid.NewGuid():N}"[..30],
            Title = "Software Engineer",
            Description = "Build things",
            Location = "Remote",
            OrganizationId = org.Id
        };

        var result = await _domainFacade.CreateJob(job);

        Assert.IsNotNull(result, "Create should return a Job");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Job should have a valid ID");
        Assert.AreEqual(job.Title, result.Title);
        Assert.AreEqual(org.Id, result.OrganizationId);
    }

    [TestMethod]
    public async Task CreateJob_EmptyTitle_ThrowsValidationException()
    {
        var job = new Job
        {
            ExternalJobId = $"ext-{Guid.NewGuid():N}"[..30],
            Title = ""
        };

        await Assert.ThrowsExceptionAsync<JobValidationException>(() =>
            _domainFacade.CreateJob(job), "Should throw validation exception for empty title");
    }

    [TestMethod]
    public async Task CreateJob_EmptyExternalJobId_ThrowsValidationException()
    {
        var job = new Job
        {
            ExternalJobId = "",
            Title = "Software Engineer"
        };

        await Assert.ThrowsExceptionAsync<JobValidationException>(() =>
            _domainFacade.CreateJob(job), "Should throw validation exception for empty externalJobId");
    }

    [TestMethod]
    public async Task CreateJob_DuplicateExternalJobId_ThrowsValidationException()
    {
        var (_, org) = await CreateTestOrgAsync();
        var externalId = $"ext-{Guid.NewGuid():N}"[..30];

        await _domainFacade.CreateJob(new Job
        {
            ExternalJobId = externalId,
            Title = "First Job",
            OrganizationId = org.Id
        });

        await Assert.ThrowsExceptionAsync<JobValidationException>(() =>
            _domainFacade.CreateJob(new Job
            {
                ExternalJobId = externalId,
                Title = "Second Job",
                OrganizationId = org.Id
            }), "Should throw validation exception for duplicate externalJobId");
    }

    [TestMethod]
    public async Task GetJobById_ExistingId_ReturnsJob()
    {
        var (_, org) = await CreateTestOrgAsync();
        var created = await _domainFacade.CreateJob(new Job
        {
            ExternalJobId = $"ext-{Guid.NewGuid():N}"[..30],
            Title = "Test Job",
            OrganizationId = org.Id
        });

        var result = await _domainFacade.GetJobById(created.Id);

        Assert.IsNotNull(result, $"Should return Job with ID: {created.Id}");
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Title, result.Title);
    }

    [TestMethod]
    public async Task GetJobById_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<JobNotFoundException>(() =>
            _domainFacade.GetJobById(nonExistingId));
    }

    [TestMethod]
    public async Task UpdateJob_ValidData_UpdatesSuccessfully()
    {
        var (_, org) = await CreateTestOrgAsync();
        var created = await _domainFacade.CreateJob(new Job
        {
            ExternalJobId = $"ext-{Guid.NewGuid():N}"[..30],
            Title = "Original Title",
            OrganizationId = org.Id
        });
        var updates = new Job { Title = "Updated Title" };

        var result = await _domainFacade.UpdateJob(created.Id, updates);

        Assert.IsNotNull(result, "Update should return the updated Job");
        Assert.AreEqual("Updated Title", result.Title);
    }

    [TestMethod]
    public async Task DeleteJob_ExistingId_DeletesSuccessfully()
    {
        var (_, org) = await CreateTestOrgAsync();
        var created = await _domainFacade.CreateJob(new Job
        {
            ExternalJobId = $"ext-{Guid.NewGuid():N}"[..30],
            Title = "To Delete",
            OrganizationId = org.Id
        });

        var result = await _domainFacade.DeleteJob(created.Id);

        Assert.IsTrue(result, "Should return true when deleting existing Job");
    }

    [TestMethod]
    public async Task DeleteJob_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<JobNotFoundException>(() =>
            _domainFacade.DeleteJob(nonExistingId));
    }
}
