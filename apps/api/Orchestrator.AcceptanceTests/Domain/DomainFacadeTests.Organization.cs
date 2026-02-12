using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Organization operations using real DomainFacade.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// All test organization names MUST start with "TestOrg_" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsOrganization
{
    private DomainFacade _domainFacade = null!;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

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

    private async Task<Organization> CreateTestOrganizationAsync(string suffix = "")
    {
        var org = new Organization
        {
            Name = Truncate($"TestOrg_{suffix}{Guid.NewGuid():N}", 50),
            ApiKey = "", // Manager generates if empty
            IsActive = true
        };
        var result = await _domainFacade.CreateOrganization(org);
        Assert.IsNotNull(result, "Failed to create test Organization");
        return result;
    }

    [TestMethod]
    public async Task CreateOrganization_ValidData_ReturnsCreatedOrganization()
    {
        var org = new Organization
        {
            Name = Truncate($"TestOrg_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        };

        var result = await _domainFacade.CreateOrganization(org);

        Assert.IsNotNull(result, "Create should return an Organization");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Organization should have a valid ID");
        Assert.IsFalse(string.IsNullOrEmpty(result.ApiKey), "ApiKey should be generated");
        Assert.AreEqual(org.Name, result.Name);
        Assert.AreEqual(org.IsActive, result.IsActive);
    }

    [TestMethod]
    public async Task CreateOrganization_InvalidData_ThrowsValidationException()
    {
        var org = new Organization
        {
            Name = "", // Required
            IsActive = true
        };

        await Assert.ThrowsExceptionAsync<OrganizationValidationException>(() =>
            _domainFacade.CreateOrganization(org), "Should throw validation exception for invalid data");
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
    public async Task GetOrganizationById_NonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();

        var result = await _domainFacade.GetOrganizationById(nonExistingId);

        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task GetOrganizationByApiKey_ExistingKey_ReturnsOrganization()
    {
        var created = await CreateTestOrganizationAsync();
        Assert.IsFalse(string.IsNullOrEmpty(created.ApiKey), "Test org should have ApiKey");

        var result = await _domainFacade.GetOrganizationByApiKey(created.ApiKey);

        Assert.IsNotNull(result, "Should return Organization by ApiKey");
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetOrganizationByApiKey_NonExistingKey_ReturnsNull()
    {
        var result = await _domainFacade.GetOrganizationByApiKey("nonexistent_key_12345");

        Assert.IsNull(result, "Should return null for non-existing ApiKey");
    }

    [TestMethod]
    public async Task SearchOrganizations_WithResults_ReturnsPaginatedList()
    {
        var prefix = Truncate($"TestOrg_Search_{Guid.NewGuid():N}", 30);
        var org1 = await CreateTestOrganizationAsync("1");
        var org2 = await CreateTestOrganizationAsync("2");
        // Update names so search finds them (SearchOrganizations filters by name)
        org1.Name = prefix + "_A";
        org2.Name = prefix + "_B";
        await _domainFacade.UpdateOrganization(org1);
        await _domainFacade.UpdateOrganization(org2);

        var result = await _domainFacade.SearchOrganizations(prefix, null, 1, 10);

        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 organizations, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, "Should return at least 2 items");
    }

    [TestMethod]
    public async Task SearchOrganizations_NoResults_ReturnsEmptyList()
    {
        var uniqueName = Truncate($"NonExistent{Guid.NewGuid():N}", 30);

        var result = await _domainFacade.SearchOrganizations(uniqueName, null, 1, 10);

        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateOrganization_ValidData_UpdatesSuccessfully()
    {
        var org = await CreateTestOrganizationAsync();
        org.Name = Truncate($"TestOrg_Updated_{Guid.NewGuid():N}", 50);
        org.WebhookUrl = "https://example.com/webhook";

        var result = await _domainFacade.UpdateOrganization(org);

        Assert.IsNotNull(result, "Update should return the updated Organization");
        Assert.AreEqual(org.Name, result.Name);
        Assert.AreEqual(org.WebhookUrl, result.WebhookUrl);
    }

    [TestMethod]
    public async Task UpdateOrganization_InvalidData_ThrowsValidationException()
    {
        var org = await CreateTestOrganizationAsync();
        org.Name = "";

        await Assert.ThrowsExceptionAsync<OrganizationValidationException>(() =>
            _domainFacade.UpdateOrganization(org), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteOrganization_ExistingId_DeletesSuccessfully()
    {
        var org = await CreateTestOrganizationAsync();
        var id = org.Id;

        var result = await _domainFacade.DeleteOrganization(id);

        Assert.IsTrue(result, "Should return true when deleting existing Organization");
        var deleted = await _domainFacade.GetOrganizationById(id);
        Assert.IsNull(deleted, "Should not find deleted Organization");
    }

    [TestMethod]
    public async Task DeleteOrganization_NonExistingId_ReturnsFalse()
    {
        var nonExistingId = Guid.NewGuid();

        var result = await _domainFacade.DeleteOrganization(nonExistingId);

        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task OrganizationLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var org = await CreateTestOrganizationAsync("Lifecycle");
        Assert.IsNotNull(org);
        var createdId = org.Id;

        var retrieved = await _domainFacade.GetOrganizationById(createdId);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(createdId, retrieved.Id);

        retrieved.Name = Truncate($"TestOrg_UpdatedLifecycle_{Guid.NewGuid():N}", 50);
        var updated = await _domainFacade.UpdateOrganization(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchOrganizations(updated.Name, null, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated organization");

        var deleteResult = await _domainFacade.DeleteOrganization(createdId);
        Assert.IsTrue(deleteResult);
        var afterDelete = await _domainFacade.GetOrganizationById(createdId);
        Assert.IsNull(afterDelete, "Should not find deleted Organization");
    }
}
