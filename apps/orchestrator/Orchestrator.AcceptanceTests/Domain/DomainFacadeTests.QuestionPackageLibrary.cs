using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Question Package Library operations using real DomainFacade.
/// These tests verify the read-only API against Liquibase-seeded data.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsQuestionPackageLibrary
{
    private DomainFacade _domainFacade = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try { TestDataCleanup.CleanupAllTestData(); }
        catch (Exception ex) { Console.WriteLine($"Warning: {ex.Message}"); }
        finally { _domainFacade?.Dispose(); }
    }

    [TestMethod]
    public async Task GetAllRoleTemplates_ReturnsSeededRoles()
    {
        var result = await _domainFacade.GetAllRoleTemplatesAsync();

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count >= 3, $"Expected at least 3 role templates, got {result.Count}");

        var roleKeys = result.Select(r => r.RoleKey).ToList();
        CollectionAssert.Contains(roleKeys, "caregiver");
        CollectionAssert.Contains(roleKeys, "cna");
        CollectionAssert.Contains(roleKeys, "housekeeping");
    }

    [TestMethod]
    public async Task GetAllRoleTemplates_HasCorrectDefaults()
    {
        var result = await _domainFacade.GetAllRoleTemplatesAsync();

        foreach (var role in result)
        {
            Assert.AreEqual(2, role.MaxFollowUpsPerQuestion, $"Role {role.RoleKey}: MaxFollowUps should be 2");
            Assert.AreEqual(1, role.ScoringScaleMin, $"Role {role.RoleKey}: ScoringScaleMin should be 1");
            Assert.AreEqual(5, role.ScoringScaleMax, $"Role {role.RoleKey}: ScoringScaleMax should be 5");
            Assert.AreEqual(2, role.FlagThreshold, $"Role {role.RoleKey}: FlagThreshold should be 2");
            Assert.AreEqual("healthcare", role.Industry, $"Role {role.RoleKey}: Industry should be healthcare");
        }
    }

    [TestMethod]
    public async Task GetRoleTemplateByKey_Caregiver_ReturnsCorrectRole()
    {
        var result = await _domainFacade.GetRoleTemplateByKeyAsync("caregiver");

        Assert.IsNotNull(result, "Caregiver role template should exist");
        Assert.AreEqual("caregiver", result.RoleKey);
        Assert.AreEqual("Caregiver", result.RoleName);
    }

    [TestMethod]
    public async Task GetRoleTemplateByKey_CNA_ReturnsCorrectRole()
    {
        var result = await _domainFacade.GetRoleTemplateByKeyAsync("cna");

        Assert.IsNotNull(result, "CNA role template should exist");
        Assert.AreEqual("cna", result.RoleKey);
        Assert.AreEqual("Certified Nursing Assistant", result.RoleName);
    }

    [TestMethod]
    public async Task GetRoleTemplateByKey_NonExistent_ReturnsNull()
    {
        var result = await _domainFacade.GetRoleTemplateByKeyAsync("non_existent_role");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetRoleTemplateWithFullDetails_Caregiver_LoadsHierarchy()
    {
        var result = await _domainFacade.GetRoleTemplateWithFullDetailsAsync("caregiver");

        Assert.IsNotNull(result, "Caregiver should load with full details");
        Assert.AreEqual(3, result.Competencies.Count, "Caregiver should have 3 competencies");

        foreach (var comp in result.Competencies)
        {
            Assert.IsFalse(string.IsNullOrEmpty(comp.CanonicalExample), $"Competency '{comp.Name}' should have a canonical example (backfilled from seed)");
        }
    }

    [TestMethod]
    public async Task GetRoleTemplateWithFullDetails_CNA_Has4Competencies()
    {
        var result = await _domainFacade.GetRoleTemplateWithFullDetailsAsync("cna");

        Assert.IsNotNull(result, "CNA should load with full details");
        Assert.AreEqual(4, result.Competencies.Count, "CNA should have 4 competencies");

        var competencyKeys = result.Competencies.Select(c => c.CompetencyKey).ToList();
        CollectionAssert.Contains(competencyKeys, "patient_care_mindset");
        CollectionAssert.Contains(competencyKeys, "reliability_accountability");
        CollectionAssert.Contains(competencyKeys, "teamwork_communication");
        CollectionAssert.Contains(competencyKeys, "handling_difficult_situations");
    }

    [TestMethod]
    public async Task GetRoleTemplateWithFullDetails_CNA_PatientCareHasCanonicalExample()
    {
        var result = await _domainFacade.GetRoleTemplateWithFullDetailsAsync("cna");
        Assert.IsNotNull(result);

        var patientCare = result.Competencies.First(c => c.CompetencyKey == "patient_care_mindset");
        Assert.IsFalse(string.IsNullOrEmpty(patientCare.CanonicalExample), "CNA Patient Care should have a canonical example");
    }

    [TestMethod]
    public async Task GetRoleTemplateWithFullDetails_EachCompetencyHasCanonicalExample()
    {
        var result = await _domainFacade.GetRoleTemplateWithFullDetailsAsync("cna");
        Assert.IsNotNull(result);

        foreach (var comp in result.Competencies)
        {
            Assert.IsFalse(string.IsNullOrEmpty(comp.CanonicalExample),
                $"Competency '{comp.Name}' should have a canonical example");
        }
    }

    [TestMethod]
    public async Task GetRoleTemplateWithFullDetails_Weights_SumTo100PerRole()
    {
        foreach (var roleKey in new[] { "caregiver", "cna", "housekeeping" })
        {
            var result = await _domainFacade.GetRoleTemplateWithFullDetailsAsync(roleKey);
            Assert.IsNotNull(result);

            var totalWeight = result.Competencies.Sum(c => c.DefaultWeight);
            Assert.AreEqual(100, totalWeight,
                $"Role '{roleKey}' competency weights should sum to 100, got {totalWeight}");
        }
    }

    [TestMethod]
    public async Task GetRoleTemplateById_ValidId_ReturnsRole()
    {
        var roles = await _domainFacade.GetAllRoleTemplatesAsync();
        var firstRole = roles.First();

        var result = await _domainFacade.GetRoleTemplateByIdAsync(firstRole.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(firstRole.Id, result.Id);
        Assert.AreEqual(firstRole.RoleKey, result.RoleKey);
    }

    [TestMethod]
    public async Task GetRoleTemplateById_NonExistentId_ReturnsNull()
    {
        var result = await _domainFacade.GetRoleTemplateByIdAsync(Guid.NewGuid());
        Assert.IsNull(result);
    }
}
