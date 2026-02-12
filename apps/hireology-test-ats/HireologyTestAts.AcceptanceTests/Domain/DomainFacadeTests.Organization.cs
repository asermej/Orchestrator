using HireologyTestAts.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HireologyTestAts.AcceptanceTests.Domain;
using HireologyTestAts.AcceptanceTests.TestUtilities;

namespace HireologyTestAts.AcceptanceTests.Domain;

/// <summary>
/// Tests for Organization operations using real DomainFacade.
/// All test organization names MUST start with "TestOrg_" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsOrganization
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

    private async Task<Group> CreateTestGroupAsync(string suffix = "")
    {
        var group = new Group
        {
            Name = $"TestGroup_{suffix}{Guid.NewGuid():N}"[..50]
        };
        return await _domainFacade.CreateGroup(group);
    }

    private async Task<Organization> CreateTestOrganizationAsync(string suffix = "")
    {
        var group = await CreateTestGroupAsync(suffix);
        var org = new Organization
        {
            GroupId = group.Id,
            Name = $"TestOrg_{suffix}{Guid.NewGuid():N}"[..50],
            City = "Chicago",
            State = "IL"
        };
        var result = await _domainFacade.CreateOrganization(org);
        Assert.IsNotNull(result, "Failed to create test Organization");
        return result;
    }

    [TestMethod]
    public async Task CreateOrganization_ValidData_ReturnsCreatedOrganization()
    {
        var group = await CreateTestGroupAsync();
        var org = new Organization
        {
            GroupId = group.Id,
            Name = $"TestOrg_{Guid.NewGuid():N}"[..50],
            City = "Chicago",
            State = "IL"
        };

        var result = await _domainFacade.CreateOrganization(org);

        Assert.IsNotNull(result, "Create should return an Organization");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Organization should have a valid ID");
        Assert.AreEqual(org.Name, result.Name);
        Assert.AreEqual(org.City, result.City);
    }

    [TestMethod]
    public async Task CreateOrganization_EmptyName_ThrowsValidationException()
    {
        var group = await CreateTestGroupAsync();
        var org = new Organization
        {
            GroupId = group.Id,
            Name = ""
        };

        await Assert.ThrowsExceptionAsync<OrganizationValidationException>(() =>
            _domainFacade.CreateOrganization(org), "Should throw validation exception for empty name");
    }

    [TestMethod]
    public async Task CreateOrganization_EmptyGroupId_ThrowsValidationException()
    {
        var org = new Organization
        {
            GroupId = Guid.Empty,
            Name = $"TestOrg_{Guid.NewGuid():N}"[..50]
        };

        await Assert.ThrowsExceptionAsync<OrganizationValidationException>(() =>
            _domainFacade.CreateOrganization(org), "Should throw validation exception for empty GroupId");
    }

    [TestMethod]
    public async Task GetOrganizationById_ExistingId_ReturnsOrganization()
    {
        var created = await CreateTestOrganizationAsync();

        var result = await _domainFacade.GetOrganizationById(created.Id);

        Assert.IsNotNull(result, $"Should return Organization with ID: {created.Id}");
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Name, result.Name);
    }

    [TestMethod]
    public async Task GetOrganizationById_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<OrganizationNotFoundException>(() =>
            _domainFacade.GetOrganizationById(nonExistingId));
    }

    [TestMethod]
    public async Task GetOrganizations_ReturnsListOfOrganizations()
    {
        await CreateTestOrganizationAsync("A");
        await CreateTestOrganizationAsync("B");

        var result = await _domainFacade.GetOrganizations();

        Assert.IsNotNull(result, "Should return a list of organizations");
        Assert.IsTrue(result.Count >= 2, $"Should find at least 2 organizations, found {result.Count}");
    }

    [TestMethod]
    public async Task UpdateOrganization_ValidData_UpdatesSuccessfully()
    {
        var created = await CreateTestOrganizationAsync();
        var updates = new Organization
        {
            Name = $"TestOrg_Updated_{Guid.NewGuid():N}"[..50],
            City = "New York",
            State = "NY"
        };

        var result = await _domainFacade.UpdateOrganization(created.Id, updates);

        Assert.IsNotNull(result, "Update should return the updated Organization");
        Assert.AreEqual(updates.Name, result.Name);
        Assert.AreEqual("New York", result.City);
    }

    [TestMethod]
    public async Task DeleteOrganization_ExistingId_DeletesSuccessfully()
    {
        var created = await CreateTestOrganizationAsync();

        var result = await _domainFacade.DeleteOrganization(created.Id);

        Assert.IsTrue(result, "Should return true when deleting existing Organization");
    }

    [TestMethod]
    public async Task DeleteOrganization_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<OrganizationNotFoundException>(() =>
            _domainFacade.DeleteOrganization(nonExistingId));
    }
}
