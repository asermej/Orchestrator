using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for user-created Question Package Library CRUD operations.
/// Validates create, update, delete, org-scoping, and system-template guards.
/// All test group names MUST start with "TestOrg_" for cleanup to find them.
/// </summary>
[TestClass]
public class DomainFacadeTestsQuestionPackageLibraryCrud
{
    private DomainFacade _domainFacade = null!;
    private Guid _testGroupId;

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());

        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = Truncate($"TestOrg_QPL_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testGroupId = group.Id;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try { TestDataCleanup.CleanupAllTestData(); }
        catch (Exception ex) { Console.WriteLine($"Warning: {ex.Message}"); }
        finally { _domainFacade?.Dispose(); }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    // --- Role Template CRUD ---

    [TestMethod]
    public async Task CreateRoleTemplate_ValidCustom_CreatesSuccessfully()
    {
        var roleTemplate = new RoleTemplate
        {
            RoleName = "Test Custom Role",
            Industry = "technology",
            GroupId = _testGroupId
        };

        var created = await _domainFacade.CreateRoleTemplateAsync(roleTemplate);

        Assert.IsNotNull(created);
        Assert.AreNotEqual(Guid.Empty, created.Id);
        Assert.AreEqual("custom", created.Source);
        Assert.AreEqual(_testGroupId, created.GroupId);
        Assert.AreEqual("test_custom_role", created.RoleKey);
        Assert.AreEqual("Test Custom Role", created.RoleName);
        Assert.AreEqual("technology", created.Industry);
    }

    [TestMethod]
    public async Task CreateRoleTemplate_MissingName_ThrowsValidation()
    {
        var roleTemplate = new RoleTemplate
        {
            RoleName = "",
            Industry = "technology",
            GroupId = _testGroupId
        };

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.CreateRoleTemplateAsync(roleTemplate));
    }

    [TestMethod]
    public async Task CreateRoleTemplate_MissingGroupId_ThrowsValidation()
    {
        var roleTemplate = new RoleTemplate
        {
            RoleName = "Test Role No Group",
            Industry = "healthcare"
        };

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.CreateRoleTemplateAsync(roleTemplate));
    }

    [TestMethod]
    public async Task UpdateRoleTemplate_CustomTemplate_UpdatesSuccessfully()
    {
        var created = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Original Role",
            Industry = "healthcare",
            GroupId = _testGroupId
        });

        created.RoleName = "Updated Role";
        created.Industry = "technology";
        var updated = await _domainFacade.UpdateRoleTemplateAsync(created);

        Assert.AreEqual("Updated Role", updated.RoleName);
        Assert.AreEqual("technology", updated.Industry);
        Assert.AreEqual("custom", updated.Source);
    }

    [TestMethod]
    public async Task UpdateRoleTemplate_SystemTemplate_ThrowsValidation()
    {
        var systemTemplates = await _domainFacade.GetRoleTemplatesByFilterAsync(source: "system");
        Assert.IsTrue(systemTemplates.Count > 0, "Expected at least one system template");

        var systemTemplate = systemTemplates.First();
        systemTemplate.RoleName = "Attempted Modification";

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.UpdateRoleTemplateAsync(systemTemplate));
    }

    [TestMethod]
    public async Task DeleteRoleTemplate_CustomTemplate_DeletesWithChildren()
    {
        var role = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Deletable Role",
            Industry = "healthcare",
            GroupId = _testGroupId
        });

        var competency = await _domainFacade.CreateCompetencyAsync(new Competency
        {
            RoleTemplateId = role.Id,
            Name = "Test Competency",
            CanonicalExample = "Tell me about a time when you demonstrated this.",
            DefaultWeight = 50,
            DisplayOrder = 1
        });

        var deleted = await _domainFacade.DeleteRoleTemplateAsync(role.Id);
        Assert.IsTrue(deleted);

        var retrieved = await _domainFacade.GetRoleTemplateByIdAsync(role.Id);
        Assert.IsNull(retrieved, "Deleted role template should not be retrievable");
    }

    [TestMethod]
    public async Task DeleteRoleTemplate_SystemTemplate_ThrowsValidation()
    {
        var systemTemplates = await _domainFacade.GetRoleTemplatesByFilterAsync(source: "system");
        Assert.IsTrue(systemTemplates.Count > 0);

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.DeleteRoleTemplateAsync(systemTemplates.First().Id));
    }

    // --- Org-scoping ---

    [TestMethod]
    public async Task GetRoleTemplatesByFilter_SystemOnly_ReturnsOnlySystem()
    {
        await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Should Not Appear",
            Industry = "tech",
            GroupId = _testGroupId
        });

        var systemOnly = await _domainFacade.GetRoleTemplatesByFilterAsync(source: "system");

        Assert.IsTrue(systemOnly.All(r => r.Source == "system"),
            "Filter source=system should only return system templates");
    }

    [TestMethod]
    public async Task GetRoleTemplatesByFilter_ByGroupId_ReturnsSystemPlusCustom()
    {
        var custom = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Group Scoped Role",
            Industry = "tech",
            GroupId = _testGroupId
        });

        var results = await _domainFacade.GetRoleTemplatesByFilterAsync(groupId: _testGroupId);

        Assert.IsTrue(results.Any(r => r.Source == "system"), "Should include system templates");
        Assert.IsTrue(results.Any(r => r.Id == custom.Id), "Should include the custom template for this group");
    }

    // --- Competency CRUD ---

    [TestMethod]
    public async Task CreateCompetency_ValidCustomRole_CreatesSuccessfully()
    {
        var role = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Role With Comp",
            Industry = "healthcare",
            GroupId = _testGroupId
        });

        var competency = await _domainFacade.CreateCompetencyAsync(new Competency
        {
            RoleTemplateId = role.Id,
            Name = "Problem Solving",
            CanonicalExample = "Tell me about a time when you solved a difficult problem.",
            DefaultWeight = 40,
            IsRequired = true,
            DisplayOrder = 1
        });

        Assert.IsNotNull(competency);
        Assert.AreEqual("problem_solving", competency.CompetencyKey);
        Assert.AreEqual(40, competency.DefaultWeight);
    }

    [TestMethod]
    public async Task CreateCompetency_SystemRole_ThrowsValidation()
    {
        var systemTemplates = await _domainFacade.GetRoleTemplatesByFilterAsync(source: "system");
        var systemRole = systemTemplates.First();

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.CreateCompetencyAsync(new Competency
            {
                RoleTemplateId = systemRole.Id,
                Name = "Cannot Add",
                CanonicalExample = "Example",
                DefaultWeight = 50,
                DisplayOrder = 99
            }));
    }

    [TestMethod]
    public async Task CreateCompetency_InvalidWeight_ThrowsValidation()
    {
        var role = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Weight Test Role",
            Industry = "tech",
            GroupId = _testGroupId
        });

        await Assert.ThrowsExceptionAsync<QuestionPackageLibraryValidationException>(
            () => _domainFacade.CreateCompetencyAsync(new Competency
            {
                RoleTemplateId = role.Id,
                Name = "Bad Weight",
                CanonicalExample = "Example",
                DefaultWeight = 150,
                DisplayOrder = 1
            }));
    }

    // --- Full Hierarchy CRUD ---

    [TestMethod]
    public async Task CreateFullHierarchy_CustomRole_PersistsAllLevels()
    {
        var role = await _domainFacade.CreateRoleTemplateAsync(new RoleTemplate
        {
            RoleName = "Full Hierarchy Role",
            Industry = "retail",
            GroupId = _testGroupId
        });

        var competency = await _domainFacade.CreateCompetencyAsync(new Competency
        {
            RoleTemplateId = role.Id,
            Name = "Customer Service",
            CanonicalExample = "Tell me about a time you went above and beyond for a customer.",
            DefaultWeight = 100,
            IsRequired = true,
            DisplayOrder = 1
        });

        // Verify full hierarchy loads (competency has canonical example)
        var loaded = await _domainFacade.GetRoleTemplateWithFullDetailsAsync(role.RoleKey);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.Competencies.Count);
        Assert.IsFalse(string.IsNullOrEmpty(loaded.Competencies[0].CanonicalExample));
    }
}
