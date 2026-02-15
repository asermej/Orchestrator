using HireologyTestAts.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HireologyTestAts.AcceptanceTests.Domain;
using HireologyTestAts.AcceptanceTests.TestUtilities;

namespace HireologyTestAts.AcceptanceTests.Domain;

/// <summary>
/// Tests for Group operations using real DomainFacade.
/// All test group names MUST start with "TestGroup_" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsGroup
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
            Name = $"TestGroup_{suffix}{Guid.NewGuid():N}"
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
            Name = $"TestGroup_{Guid.NewGuid():N}"
        };

        var result = await _domainFacade.CreateGroup(group);

        Assert.IsNotNull(result, "Create should return a Group");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Group should have a valid ID");
        Assert.AreEqual(group.Name, result.Name);
    }

    [TestMethod]
    public async Task CreateGroup_EmptyName_ThrowsValidationException()
    {
        var group = new Group { Name = "" };

        await Assert.ThrowsExceptionAsync<GroupValidationException>(() =>
            _domainFacade.CreateGroup(group), "Should throw validation exception for empty name");
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
    public async Task GetGroupById_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<GroupNotFoundException>(() =>
            _domainFacade.GetGroupById(nonExistingId));
    }

    [TestMethod]
    public async Task GetGroups_ReturnsListOfGroups()
    {
        await CreateTestGroupAsync("A");
        await CreateTestGroupAsync("B");

        var result = await _domainFacade.GetGroups();

        Assert.IsNotNull(result, "Should return a list of groups");
        Assert.IsTrue(result.Count >= 2, $"Should find at least 2 groups, found {result.Count}");
    }

    [TestMethod]
    public async Task UpdateGroup_ValidData_UpdatesSuccessfully()
    {
        var created = await CreateTestGroupAsync();
        var updates = new Group { Name = $"TestGroup_Updated_{Guid.NewGuid():N}" };

        var result = await _domainFacade.UpdateGroup(created.Id, updates);

        Assert.IsNotNull(result, "Update should return the updated Group");
        Assert.AreEqual(updates.Name, result.Name);
    }

    [TestMethod]
    public async Task DeleteGroup_ExistingId_DeletesSuccessfully()
    {
        var created = await CreateTestGroupAsync();

        var result = await _domainFacade.DeleteGroup(created.Id);

        Assert.IsTrue(result, "Should return true when deleting existing Group");
    }

    [TestMethod]
    public async Task DeleteGroup_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<GroupNotFoundException>(() =>
            _domainFacade.DeleteGroup(nonExistingId));
    }
}
