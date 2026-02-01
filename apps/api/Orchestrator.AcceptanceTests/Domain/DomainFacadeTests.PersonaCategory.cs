using System;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for PersonaCategory operations using real DomainFacade
/// </summary>
[TestClass]
public class DomainFacadeTestsPersonaCategory
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsPersonaCategory()
    {
        _domainFacade = null!;
        _connectionString = null!;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        TestDataCleanup.CleanupAllTestData(_connectionString);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            TestDataCleanup.CleanupAllTestData(_connectionString);
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

    [TestMethod]
    public async Task AddCategoryToPersona_ValidData_ReturnsValidId()
    {
        // Arrange - Create test persona and category
        var persona = new Persona { DisplayName = "TestPersonaAdd" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        
        var category = new Category { Name = "TestCatAdd", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var createdCategory = await _domainFacade.CreateCategory(category);

        // Act
        var result = await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCategory.Id);

        // Assert
        Assert.IsNotNull(result, "Expected PersonaCategory to be created");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Expected valid ID");
        Assert.AreEqual(createdPersona.Id, result.PersonaId);
        Assert.AreEqual(createdCategory.Id, result.CategoryId);
        Console.WriteLine($"Successfully added category {createdCategory.Name} to persona {createdPersona.DisplayName}");
    }

    [TestMethod]
    public async Task AddCategoryToPersona_DuplicatePersonaCategory_ThrowsException()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaDup" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        
        var category = new Category { Name = "TestCatDup", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var createdCategory = await _domainFacade.CreateCategory(category);

        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCategory.Id);

        // Try to add same combination again
        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaCategoryDuplicateException>(
            async () => await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCategory.Id),
            "Expected PersonaCategoryDuplicateException for duplicate");
        Console.WriteLine("Duplicate validation working correctly");
    }

    [TestMethod]
    public async Task AddCategoryToPersona_InvalidPersonaId_ThrowsValidationException()
    {
        // Arrange
        var category = new Category { Name = "TestCatInvalidPersona", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var createdCategory = await _domainFacade.CreateCategory(category);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaCategoryValidationException>(
            async () => await _domainFacade.AddCategoryToPersona(Guid.NewGuid(), createdCategory.Id), // Non-existent persona
            "Expected PersonaCategoryValidationException for invalid persona");
        Console.WriteLine("Invalid persona validation working correctly");
    }

    [TestMethod]
    public async Task AddCategoryToPersona_InvalidCategoryId_ThrowsValidationException()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaInvalidCat" };
        var createdPersona = await _domainFacade.CreatePersona(persona);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaCategoryValidationException>(
            async () => await _domainFacade.AddCategoryToPersona(createdPersona.Id, Guid.NewGuid()), // Non-existent category
            "Expected PersonaCategoryValidationException for invalid category");
        Console.WriteLine("Invalid category validation working correctly");
    }

    [TestMethod]
    public async Task GetCategoriesByPersonaId_PersonaWithCategories_ReturnsCategories()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaGet" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        
        var cat1 = new Category { Name = "TestCatGet1", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var cat2 = new Category { Name = "TestCatGet2", CategoryType = "Standard", DisplayOrder = 2, IsActive = true };
        var createdCat1 = await _domainFacade.CreateCategory(cat1);
        var createdCat2 = await _domainFacade.CreateCategory(cat2);

        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCat1.Id);
        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCat2.Id);

        // Act
        var result = await _domainFacade.GetPersonaCategories(createdPersona.Id);

        // Assert
        var categories = result.ToList();
        Assert.IsTrue(categories.Count >= 2, $"Expected at least 2 categories, got {categories.Count}");
        Assert.IsTrue(categories.Any(c => c.Name == "TestCatGet1"), "Expected TestCatGet1");
        Assert.IsTrue(categories.Any(c => c.Name == "TestCatGet2"), "Expected TestCatGet2");
        Console.WriteLine($"Found {categories.Count} categories for persona");
    }

    [TestMethod]
    public async Task GetCategoriesByPersonaId_PersonaWithNoCategories_ReturnsEmpty()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaEmpty" };
        var createdPersona = await _domainFacade.CreatePersona(persona);

        // Act
        var result = await _domainFacade.GetPersonaCategories(createdPersona.Id);

        // Assert
        Assert.IsFalse(result.Any(), "Expected no categories for persona with no categories");
        Console.WriteLine("Empty result handled correctly");
    }

    [TestMethod]
    public async Task RemoveCategoryFromPersona_ExistingPersonaCategory_ReturnsTrue()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaRemove" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        
        var category = new Category { Name = "TestCatRemove", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var createdCategory = await _domainFacade.CreateCategory(category);

        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCategory.Id);

        // Act
        var result = await _domainFacade.RemoveCategoryFromPersona(createdPersona.Id, createdCategory.Id);

        // Assert
        Assert.IsTrue(result, "Expected RemoveCategoryFromPersona to return true");

        // Verify it's removed
        var categories = await _domainFacade.GetPersonaCategories(createdPersona.Id);
        Assert.IsFalse(categories.Any(c => c.Id == createdCategory.Id), "Category should be removed");
        Console.WriteLine("Category removed successfully");
    }

    [TestMethod]
    public async Task RemoveCategoryFromPersona_NonExistentPersonaCategory_ReturnsFalse()
    {
        // Arrange
        var persona = new Persona { DisplayName = "TestPersonaRemoveNonExistent" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        
        var category = new Category { Name = "TestCatRemoveNonExistent", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var createdCategory = await _domainFacade.CreateCategory(category);

        // Act (without adding it first)
        var result = await _domainFacade.RemoveCategoryFromPersona(createdPersona.Id, createdCategory.Id);

        // Assert
        Assert.IsFalse(result, "Expected RemoveCategoryFromPersona to return false for non-existent");
        Console.WriteLine("Non-existent removal handled correctly");
    }

    [TestMethod]
    public async Task PersonaCategoryLifecycle_AddGetRemove_WorksCorrectly()
    {
        // Create persona and categories
        var persona = new Persona { DisplayName = "TestPersonaLifecycle" };
        var createdPersona = await _domainFacade.CreatePersona(persona);
        Console.WriteLine($"1. Created persona: {createdPersona.DisplayName}");
        
        var cat1 = new Category { Name = "TestCatLifecycle1", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var cat2 = new Category { Name = "TestCatLifecycle2", CategoryType = "Standard", DisplayOrder = 2, IsActive = true };
        var createdCat1 = await _domainFacade.CreateCategory(cat1);
        var createdCat2 = await _domainFacade.CreateCategory(cat2);
        Console.WriteLine($"2. Created categories: {createdCat1.Name}, {createdCat2.Name}");

        // Add categories to persona
        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCat1.Id);
        await _domainFacade.AddCategoryToPersona(createdPersona.Id, createdCat2.Id);
        Console.WriteLine("3. Added both categories to persona");

        // Get categories
        var categories = (await _domainFacade.GetPersonaCategories(createdPersona.Id)).ToList();
        Assert.IsTrue(categories.Count >= 2, "Expected at least 2 categories");
        Console.WriteLine($"4. Retrieved {categories.Count} categories");

        // Remove one category
        var removed = await _domainFacade.RemoveCategoryFromPersona(createdPersona.Id, createdCat1.Id);
        Assert.IsTrue(removed, "Expected removal to succeed");
        Console.WriteLine("5. Removed one category");

        // Verify only one remains
        var remaining = (await _domainFacade.GetPersonaCategories(createdPersona.Id)).ToList();
        Assert.IsTrue(remaining.Any(c => c.Id == createdCat2.Id), "Expected cat2 to remain");
        Assert.IsFalse(remaining.Any(c => c.Id == createdCat1.Id), "Expected cat1 to be removed");
        Console.WriteLine("6. Verified only one category remains");

        Console.WriteLine("Full lifecycle test completed successfully");
    }
}

