using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Group operations using real DomainFacade.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// All test group names MUST start with "TestOrg_" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsGroup
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

    private async Task<Group> CreateTestGroupAsync(string suffix = "")
    {
        var group = new Group
        {
            Name = Truncate($"TestOrg_{suffix}{Guid.NewGuid():N}", 50),
            ApiKey = "", // Manager generates if empty
            IsActive = true
        };
        var result = await _domainFacade.CreateGroup(group);
        Assert.IsNotNull(result, "Failed to create test Group");
        return result;
    }

    [TestMethod]
    public async Task CreateGroup_ValidData_ReturnsCreatedGroup()
    {
        var group = new Group
        {
            Name = Truncate($"TestOrg_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        };

        var result = await _domainFacade.CreateGroup(group);

        Assert.IsNotNull(result, "Create should return a Group");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Group should have a valid ID");
        Assert.IsFalse(string.IsNullOrEmpty(result.ApiKey), "ApiKey should be generated");
        Assert.AreEqual(group.Name, result.Name);
        Assert.AreEqual(group.IsActive, result.IsActive);
    }

    [TestMethod]
    public async Task CreateGroup_InvalidData_ThrowsValidationException()
    {
        var group = new Group
        {
            Name = "", // Required
            IsActive = true
        };

        await Assert.ThrowsExceptionAsync<GroupValidationException>(() =>
            _domainFacade.CreateGroup(group), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task GetGroupById_ExistingId_ReturnsGroup()
    {
        var created = await CreateTestGroupAsync();

        var result = await _domainFacade.GetGroupById(created.Id);

        Assert.IsNotNull(result, $"Should return Group with ID: {created.Id}");
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Name, result.Name);
    }

    [TestMethod]
    public async Task GetGroupById_NonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();

        var result = await _domainFacade.GetGroupById(nonExistingId);

        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task GetGroupByApiKey_ExistingKey_ReturnsGroup()
    {
        var created = await CreateTestGroupAsync();
        Assert.IsFalse(string.IsNullOrEmpty(created.ApiKey), "Test group should have ApiKey");

        var result = await _domainFacade.GetGroupByApiKey(created.ApiKey);

        Assert.IsNotNull(result, "Should return Group by ApiKey");
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetGroupByApiKey_NonExistingKey_ReturnsNull()
    {
        var result = await _domainFacade.GetGroupByApiKey("nonexistent_key_12345");

        Assert.IsNull(result, "Should return null for non-existing ApiKey");
    }

    [TestMethod]
    public async Task SearchGroups_WithResults_ReturnsPaginatedList()
    {
        var prefix = Truncate($"TestOrg_Search_{Guid.NewGuid():N}", 30);
        var group1 = await CreateTestGroupAsync("1");
        var group2 = await CreateTestGroupAsync("2");
        // Update names so search finds them
        group1.Name = prefix + "_A";
        group2.Name = prefix + "_B";
        await _domainFacade.UpdateGroup(group1);
        await _domainFacade.UpdateGroup(group2);

        var result = await _domainFacade.SearchGroups(prefix, null, 1, 10);

        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 groups, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, "Should return at least 2 items");
    }

    [TestMethod]
    public async Task SearchGroups_NoResults_ReturnsEmptyList()
    {
        var uniqueName = Truncate($"NonExistent{Guid.NewGuid():N}", 30);

        var result = await _domainFacade.SearchGroups(uniqueName, null, 1, 10);

        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateGroup_ValidData_UpdatesSuccessfully()
    {
        var group = await CreateTestGroupAsync();
        group.Name = Truncate($"TestOrg_Updated_{Guid.NewGuid():N}", 50);
        group.WebhookUrl = "https://example.com/webhook";

        var result = await _domainFacade.UpdateGroup(group);

        Assert.IsNotNull(result, "Update should return the updated Group");
        Assert.AreEqual(group.Name, result.Name);
        Assert.AreEqual(group.WebhookUrl, result.WebhookUrl);
    }

    [TestMethod]
    public async Task UpdateGroup_InvalidData_ThrowsValidationException()
    {
        var group = await CreateTestGroupAsync();
        group.Name = "";

        await Assert.ThrowsExceptionAsync<GroupValidationException>(() =>
            _domainFacade.UpdateGroup(group), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteGroup_ExistingId_DeletesSuccessfully()
    {
        var group = await CreateTestGroupAsync();
        var id = group.Id;

        var result = await _domainFacade.DeleteGroup(id);

        Assert.IsTrue(result, "Should return true when deleting existing Group");
        var deleted = await _domainFacade.GetGroupById(id);
        Assert.IsNull(deleted, "Should not find deleted Group");
    }

    [TestMethod]
    public async Task DeleteGroup_NonExistingId_ReturnsFalse()
    {
        var nonExistingId = Guid.NewGuid();

        var result = await _domainFacade.DeleteGroup(nonExistingId);

        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task GroupLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var group = await CreateTestGroupAsync("Lifecycle");
        Assert.IsNotNull(group);
        var createdId = group.Id;

        var retrieved = await _domainFacade.GetGroupById(createdId);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(createdId, retrieved.Id);

        retrieved.Name = Truncate($"TestOrg_UpdatedLifecycle_{Guid.NewGuid():N}", 50);
        var updated = await _domainFacade.UpdateGroup(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchGroups(updated.Name, null, 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated group");

        var deleteResult = await _domainFacade.DeleteGroup(createdId);
        Assert.IsTrue(deleteResult);
        var afterDelete = await _domainFacade.GetGroupById(createdId);
        Assert.IsNull(afterDelete, "Should not find deleted Group");
    }
}
