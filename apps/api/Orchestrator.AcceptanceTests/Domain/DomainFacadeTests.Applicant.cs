using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Applicant operations using real DomainFacade.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsApplicant
{
    private DomainFacade _domainFacade = null!;
    private Guid _testOrganizationId;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        var org = await _domainFacade.CreateOrganization(new Organization
        {
            Name = Truncate($"TestOrg_Applicant_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testOrganizationId = org.Id;
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

    private async Task<Applicant> CreateTestApplicantAsync(string suffix = "")
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var applicant = new Applicant
        {
            OrganizationId = _testOrganizationId,
            ExternalApplicantId = $"ext_{unique}",
            FirstName = $"First{suffix}",
            LastName = $"Last{suffix}",
            Email = $"applicant_{unique}@example.com",
            Phone = $"+1555{unique}"
        };
        var result = await _domainFacade.CreateApplicant(applicant);
        Assert.IsNotNull(result, "Failed to create test Applicant");
        return result;
    }

    [TestMethod]
    public async Task CreateApplicant_ValidData_ReturnsCreatedApplicant()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var applicant = new Applicant
        {
            OrganizationId = _testOrganizationId,
            ExternalApplicantId = $"ext_{unique}",
            FirstName = "Jane",
            LastName = "Doe",
            Email = $"jane_{unique}@example.com",
            Phone = "+15551234567"
        };

        var result = await _domainFacade.CreateApplicant(applicant);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(applicant.ExternalApplicantId, result.ExternalApplicantId);
        Assert.AreEqual(applicant.FirstName, result.FirstName);
        Assert.AreEqual(applicant.Email, result.Email);
    }

    [TestMethod]
    public async Task CreateApplicant_InvalidData_ThrowsValidationException()
    {
        var applicant = new Applicant
        {
            OrganizationId = _testOrganizationId,
            ExternalApplicantId = "", // Required
            FirstName = "X",
            LastName = "Y"
        };

        await Assert.ThrowsExceptionAsync<ApplicantValidationException>(() =>
            _domainFacade.CreateApplicant(applicant), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task GetApplicantById_ExistingId_ReturnsApplicant()
    {
        var created = await CreateTestApplicantAsync();

        var result = await _domainFacade.GetApplicantById(created.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Email, result.Email);
    }

    [TestMethod]
    public async Task GetApplicantById_NonExistingId_ReturnsNull()
    {
        var result = await _domainFacade.GetApplicantById(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetApplicantByExternalId_Existing_ReturnsApplicant()
    {
        var created = await CreateTestApplicantAsync();

        var result = await _domainFacade.GetApplicantByExternalId(_testOrganizationId, created.ExternalApplicantId);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetApplicantByExternalId_NonExisting_ReturnsNull()
    {
        var result = await _domainFacade.GetApplicantByExternalId(_testOrganizationId, "nonexistent_ext_12345");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetOrCreateApplicant_New_CreatesAndReturnsApplicant()
    {
        var externalId = Truncate($"getorcreate_{Guid.NewGuid():N}", 50);
        var firstName = "New";
        var lastName = "Applicant";

        var result = await _domainFacade.GetOrCreateApplicant(_testOrganizationId, externalId, firstName, lastName, "new@example.com", null);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(externalId, result.ExternalApplicantId);
        Assert.AreEqual(firstName, result.FirstName);
    }

    [TestMethod]
    public async Task GetOrCreateApplicant_Existing_ReturnsExistingApplicant()
    {
        var created = await CreateTestApplicantAsync();

        var result = await _domainFacade.GetOrCreateApplicant(_testOrganizationId, created.ExternalApplicantId, "Other", "Name", "other@example.com", null);

        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id, "Should return existing applicant, not create duplicate");
    }

    [TestMethod]
    public async Task SearchApplicants_WithResults_ReturnsPaginatedList()
    {
        var applicant1 = await CreateTestApplicantAsync("1");
        var applicant2 = await CreateTestApplicantAsync("2");

        var result = await _domainFacade.SearchApplicants(_testOrganizationId, null, null, 1, 10);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 applicants, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2);
    }

    [TestMethod]
    public async Task SearchApplicants_NoResults_ReturnsEmptyList()
    {
        var result = await _domainFacade.SearchApplicants(_testOrganizationId, "nonexistent.email.xyz@example.com", null, 1, 10);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.IsFalse(result.Items.Any());
    }

    [TestMethod]
    public async Task UpdateApplicant_ValidData_UpdatesSuccessfully()
    {
        var applicant = await CreateTestApplicantAsync();
        applicant.FirstName = "UpdatedFirst";
        applicant.LastName = "UpdatedLast";
        applicant.Email = Truncate($"updated_{Guid.NewGuid():N}", 20) + "@example.com";

        var result = await _domainFacade.UpdateApplicant(applicant);

        Assert.IsNotNull(result);
        Assert.AreEqual(applicant.FirstName, result.FirstName);
        Assert.AreEqual(applicant.LastName, result.LastName);
    }

    [TestMethod]
    public async Task UpdateApplicant_InvalidData_ThrowsValidationException()
    {
        var applicant = await CreateTestApplicantAsync();
        applicant.ExternalApplicantId = "";

        await Assert.ThrowsExceptionAsync<ApplicantValidationException>(() =>
            _domainFacade.UpdateApplicant(applicant), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task DeleteApplicant_ExistingId_DeletesSuccessfully()
    {
        var applicant = await CreateTestApplicantAsync();
        var id = applicant.Id;

        var result = await _domainFacade.DeleteApplicant(id);

        Assert.IsTrue(result);
        Assert.IsNull(await _domainFacade.GetApplicantById(id));
    }

    [TestMethod]
    public async Task DeleteApplicant_NonExistingId_ReturnsFalse()
    {
        var result = await _domainFacade.DeleteApplicant(Guid.NewGuid());
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ApplicantLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        var applicant = await CreateTestApplicantAsync("Lifecycle");
        var createdId = applicant.Id;

        var retrieved = await _domainFacade.GetApplicantById(createdId);
        Assert.IsNotNull(retrieved);

        retrieved.FirstName = "UpdatedLifecycle";
        retrieved.LastName = "Test";
        var updated = await _domainFacade.UpdateApplicant(retrieved);
        Assert.IsNotNull(updated);

        var searchResult = await _domainFacade.SearchApplicants(_testOrganizationId, null, "UpdatedLifecycle", 1, 10);
        Assert.IsNotNull(searchResult);
        Assert.IsTrue(searchResult.TotalCount > 0);

        Assert.IsTrue(await _domainFacade.DeleteApplicant(createdId));
        Assert.IsNull(await _domainFacade.GetApplicantById(createdId));
    }
}
