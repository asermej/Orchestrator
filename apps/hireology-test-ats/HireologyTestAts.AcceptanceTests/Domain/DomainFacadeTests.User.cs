using HireologyTestAts.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HireologyTestAts.AcceptanceTests.Domain;
using HireologyTestAts.AcceptanceTests.TestUtilities;

namespace HireologyTestAts.AcceptanceTests.Domain;

/// <summary>
/// Tests for User operations using real DomainFacade.
/// All test user emails MUST use "@test-example.com" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsUser
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

    private string TestEmail() => $"test-{Guid.NewGuid():N}@test-example.com";
    private string TestAuth0Sub() => $"auth0|test-{Guid.NewGuid():N}";

    [TestMethod]
    public async Task GetOrCreateUser_NewUser_CreatesAndReturnsUser()
    {
        var auth0Sub = TestAuth0Sub();
        var email = TestEmail();
        var name = "Test User";

        var result = await _domainFacade.GetOrCreateUser(auth0Sub, email, name);

        Assert.IsNotNull(result, "Should return created user");
        Assert.AreNotEqual(Guid.Empty, result.Id, "User should have a valid ID");
        Assert.AreEqual(auth0Sub, result.Auth0Sub);
        Assert.AreEqual(email, result.Email);
    }

    [TestMethod]
    public async Task GetOrCreateUser_ExistingUser_ReturnsExistingUser()
    {
        var auth0Sub = TestAuth0Sub();
        var email = TestEmail();

        var first = await _domainFacade.GetOrCreateUser(auth0Sub, email, "First");
        var second = await _domainFacade.GetOrCreateUser(auth0Sub, email, "First");

        Assert.AreEqual(first.Id, second.Id, "Should return same user on second call");
    }

    [TestMethod]
    public async Task GetUserById_ExistingId_ReturnsUser()
    {
        var created = await _domainFacade.GetOrCreateUser(TestAuth0Sub(), TestEmail(), "Test");

        var result = await _domainFacade.GetUserById(created.Id);

        Assert.IsNotNull(result, $"Should return User with ID: {created.Id}");
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetUserById_NonExistingId_ThrowsNotFoundException()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<UserNotFoundException>(() =>
            _domainFacade.GetUserById(nonExistingId));
    }

    [TestMethod]
    public async Task UpdateUser_ValidData_UpdatesSuccessfully()
    {
        var created = await _domainFacade.GetOrCreateUser(TestAuth0Sub(), TestEmail(), "Original");
        var updates = new User { Email = TestEmail(), Name = "Updated Name" };

        var result = await _domainFacade.UpdateUser(created.Id, updates);

        Assert.IsNotNull(result, "Update should return the updated User");
        Assert.AreEqual("Updated Name", result.Name);
    }

    [TestMethod]
    public async Task GetUsers_ReturnsListOfUsers()
    {
        await _domainFacade.GetOrCreateUser(TestAuth0Sub(), TestEmail(), "User1");
        await _domainFacade.GetOrCreateUser(TestAuth0Sub(), TestEmail(), "User2");

        var result = await _domainFacade.GetUsers(1, 20);

        Assert.IsNotNull(result, "Should return a list of users");
        Assert.IsTrue(result.Count >= 2, $"Should find at least 2 users, found {result.Count}");
    }

    [TestMethod]
    public async Task SetUserAccess_GroupAndOrganization_GrantsAccess()
    {
        var user = await _domainFacade.GetOrCreateUser(TestAuth0Sub(), TestEmail(), "Access Test");

        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = $"TestGroup_{Guid.NewGuid():N}"[..50]
        });
        var org = await _domainFacade.CreateOrganization(new Organization
        {
            GroupId = group.Id,
            Name = $"TestOrg_{Guid.NewGuid():N}"[..50]
        });

        await _domainFacade.SetUserAccess(user.Id, new[] { group.Id }, Array.Empty<Guid>());

        var allowedOrgs = await _domainFacade.GetAllowedOrganizationIds(user.Id);
        Assert.IsTrue(allowedOrgs.Contains(org.Id), "User should have access to org via group membership");
    }
}
