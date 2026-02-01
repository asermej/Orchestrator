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
/// Tests for Category operations using real DomainFacade and real DataFacade with data cleanup
/// 
/// TEST APPROACH:
/// - Uses real DomainFacade and DataFacade instances for acceptance tests
/// - Tests the actual integration between layers
/// - No external mocking frameworks used
/// - ServiceLocatorForAcceptanceTesting provides real implementations
/// - Tests clean up their own data to ensure complete independence
/// 
/// ENHANCED CLEANUP APPROACH:
/// - Database-level cleanup before AND after each test for complete isolation
/// - Identifies test data by patterns (name patterns, specific test categories)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsCategory
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsCategory()
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
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
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
    public async Task CreateCategory_ValidData_ReturnsValidId()
    {
        // Arrange
        var category = new Category
        {
            Name = "TestCategory",
            Description = "Test Description",
            CategoryType = "Standard",
            DisplayOrder = 10,
            IsActive = true
        };

        // Act
        var result = await _domainFacade.CreateCategory(category);

        // Assert
        Assert.IsNotNull(result, "Expected category to be created, but got null");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Expected a valid Guid for category ID");
        Assert.AreEqual("TestCategory", result.Name, $"Expected name 'TestCategory', but got '{result.Name}'");
        Assert.AreEqual("Standard", result.CategoryType, $"Expected category type 'Standard', but got '{result.CategoryType}'");
        Assert.AreEqual(10, result.DisplayOrder, $"Expected display order 10, but got {result.DisplayOrder}");
        Assert.IsTrue(result.IsActive, "Expected category to be active");
        Console.WriteLine($"Category created successfully with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreateCategory_DuplicateName_ThrowsException()
    {
        // Arrange
        var category1 = new Category
        {
            Name = "DuplicateTest",
            Description = "First",
            CategoryType = "Standard",
            DisplayOrder = 1,
            IsActive = true
        };
        await _domainFacade.CreateCategory(category1);

        var category2 = new Category
        {
            Name = "DuplicateTest",
            Description = "Second",
            CategoryType = "Standard",
            DisplayOrder = 2,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<CategoryDuplicateNameException>(
            async () => await _domainFacade.CreateCategory(category2),
            "Expected CategoryDuplicateNameException for duplicate name");
        Console.WriteLine("Duplicate name validation working correctly");
    }

    [TestMethod]
    public async Task CreateCategory_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var category = new Category
        {
            Name = "",
            Description = "Test",
            CategoryType = "Standard",
            DisplayOrder = 1,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<CategoryValidationException>(
            async () => await _domainFacade.CreateCategory(category),
            "Expected CategoryValidationException for empty name");
        Console.WriteLine("Empty name validation working correctly");
    }

    [TestMethod]
    public async Task CreateCategory_InvalidCategoryType_ThrowsValidationException()
    {
        // Arrange
        var category = new Category
        {
            Name = "TestInvalidType",
            Description = "Test",
            CategoryType = "InvalidType",
            DisplayOrder = 1,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<CategoryValidationException>(
            async () => await _domainFacade.CreateCategory(category),
            "Expected CategoryValidationException for invalid category type");
        Console.WriteLine("Invalid category type validation working correctly");
    }

    [TestMethod]
    public async Task GetCategoryById_ExistingId_ReturnsCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "TestGetById",
            Description = "Get By ID Test",
            CategoryType = "Standard",
            DisplayOrder = 5,
            IsActive = true
        };
        var created = await _domainFacade.CreateCategory(category);

        // Act
        var result = await _domainFacade.GetCategoryById(created.Id);

        // Assert
        Assert.IsNotNull(result, $"Expected to find category with ID {created.Id}, but got null");
        Assert.AreEqual(created.Id, result.Id, "Category ID mismatch");
        Assert.AreEqual("TestGetById", result.Name, $"Expected name 'TestGetById', but got '{result.Name}'");
        Console.WriteLine($"Retrieved category successfully: {result.Name}");
    }

    [TestMethod]
    public async Task GetCategoryById_NonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetCategoryById(nonExistentId);

        // Assert
        Assert.IsNull(result, $"Expected null for non-existent ID {nonExistentId}, but got a category");
        Console.WriteLine("Non-existent ID handled correctly");
    }

    [TestMethod]
    public async Task SearchCategories_ByName_ReturnsMatchingCategories()
    {
        // Arrange
        await _domainFacade.CreateCategory(new Category { Name = "SearchTest1", CategoryType = "Standard", DisplayOrder = 1, IsActive = true });
        await _domainFacade.CreateCategory(new Category { Name = "SearchTest2", CategoryType = "Standard", DisplayOrder = 2, IsActive = true });
        await _domainFacade.CreateCategory(new Category { Name = "OtherCategory", CategoryType = "Standard", DisplayOrder = 3, IsActive = true });

        // Act
        var result = await _domainFacade.SearchCategories("SearchTest", null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Expected search results, but got null");
        Assert.IsTrue(result.Items.Count() >= 2, $"Expected at least 2 categories, but got {result.Items.Count()}");
        Assert.IsTrue(result.Items.All(c => c.Name.Contains("SearchTest")), "All results should contain 'SearchTest'");
        Console.WriteLine($"Found {result.Items.Count()} matching categories");
    }

    [TestMethod]
    public async Task SearchCategories_ByCategoryType_ReturnsMatchingCategories()
    {
        // Arrange
        await _domainFacade.CreateCategory(new Category { Name = "TestStandard", CategoryType = "Standard", DisplayOrder = 1, IsActive = true });
        await _domainFacade.CreateCategory(new Category { Name = "TestDailyBlog", CategoryType = "DailyBlog", DisplayOrder = 2, IsActive = true });

        // Act
        var result = await _domainFacade.SearchCategories(null, "DailyBlog", null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Expected search results, but got null");
        Assert.IsTrue(result.Items.Any(c => c.CategoryType == "DailyBlog"), "Expected DailyBlog categories");
        Console.WriteLine($"Found {result.Items.Count()} DailyBlog categories");
    }

    [TestMethod]
    public async Task SearchCategories_ByIsActive_ReturnsMatchingCategories()
    {
        // Arrange
        await _domainFacade.CreateCategory(new Category { Name = "TestActive", CategoryType = "Standard", DisplayOrder = 1, IsActive = true });
        await _domainFacade.CreateCategory(new Category { Name = "TestInactive", CategoryType = "Standard", DisplayOrder = 2, IsActive = false });

        // Act
        var result = await _domainFacade.SearchCategories(null, null, true, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Expected search results, but got null");
        Assert.IsTrue(result.Items.All(c => c.IsActive), "All results should be active");
        Console.WriteLine($"Found {result.Items.Count()} active categories");
    }

    [TestMethod]
    public async Task SearchCategories_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await _domainFacade.CreateCategory(new Category
            {
                Name = $"TestPagination{i}",
                CategoryType = "Standard",
                DisplayOrder = i,
                IsActive = true
            });
        }

        // Act
        var page1 = await _domainFacade.SearchCategories("TestPagination", null, null, 1, 10);
        var page2 = await _domainFacade.SearchCategories("TestPagination", null, null, 2, 10);

        // Assert
        Assert.AreEqual(10, page1.Items.Count(), $"Expected 10 items on page 1, but got {page1.Items.Count()}");
        Assert.AreEqual(5, page2.Items.Count(), $"Expected 5 items on page 2, but got {page2.Items.Count()}");
        Assert.AreEqual(15, page1.TotalCount, $"Expected total count of 15, but got {page1.TotalCount}");
        Console.WriteLine($"Pagination working correctly: Page 1 has {page1.Items.Count()} items, Page 2 has {page2.Items.Count()} items");
    }

    [TestMethod]
    public async Task UpdateCategory_ValidData_ReturnsUpdatedCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "TestUpdate",
            Description = "Original",
            CategoryType = "Standard",
            DisplayOrder = 1,
            IsActive = true
        };
        var created = await _domainFacade.CreateCategory(category);

        // Modify
        created.Name = "TestUpdateModified";
        created.Description = "Modified";
        created.DisplayOrder = 5;
        created.IsActive = false;

        // Act
        var result = await _domainFacade.UpdateCategory(created);

        // Assert
        Assert.IsNotNull(result, "Expected updated category, but got null");
        Assert.AreEqual("TestUpdateModified", result.Name, $"Expected name 'TestUpdateModified', but got '{result.Name}'");
        Assert.AreEqual("Modified", result.Description, $"Expected description 'Modified', but got '{result.Description}'");
        Assert.AreEqual(5, result.DisplayOrder, $"Expected display order 5, but got {result.DisplayOrder}");
        Assert.IsFalse(result.IsActive, "Expected category to be inactive");
        Console.WriteLine($"Category updated successfully: {result.Name}");
    }

    [TestMethod]
    public async Task UpdateCategory_DuplicateName_ThrowsException()
    {
        // Arrange
        var category1 = new Category { Name = "TestDuplicate1", CategoryType = "Standard", DisplayOrder = 1, IsActive = true };
        var category2 = new Category { Name = "TestDuplicate2", CategoryType = "Standard", DisplayOrder = 2, IsActive = true };
        
        await _domainFacade.CreateCategory(category1);
        var created2 = await _domainFacade.CreateCategory(category2);

        // Try to update category2 to have the same name as category1
        created2.Name = "TestDuplicate1";

        // Act & Assert
        await Assert.ThrowsExceptionAsync<CategoryDuplicateNameException>(
            async () => await _domainFacade.UpdateCategory(created2),
            "Expected CategoryDuplicateNameException when updating to duplicate name");
        Console.WriteLine("Duplicate name validation on update working correctly");
    }

    [TestMethod]
    public async Task DeleteCategory_ExistingId_ReturnsTrue()
    {
        // Arrange
        var category = new Category
        {
            Name = "TestDelete",
            Description = "To be deleted",
            CategoryType = "Standard",
            DisplayOrder = 1,
            IsActive = true
        };
        var created = await _domainFacade.CreateCategory(category);

        // Act
        var result = await _domainFacade.DeleteCategory(created.Id);

        // Assert
        Assert.IsTrue(result, "Expected delete to return true");

        // Verify it's gone
        var retrieved = await _domainFacade.GetCategoryById(created.Id);
        Assert.IsNull(retrieved, "Expected category to be deleted (soft delete), but it was still found");
        Console.WriteLine($"Category deleted successfully: {created.Id}");
    }

    [TestMethod]
    public async Task DeleteCategory_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteCategory(nonExistentId);

        // Assert
        Assert.IsFalse(result, $"Expected delete to return false for non-existent ID {nonExistentId}");
        Console.WriteLine("Non-existent ID delete handled correctly");
    }

    [TestMethod]
    public async Task CategoryLifecycle_CreateReadUpdateDelete_WorksCorrectly()
    {
        // Create
        var category = new Category
        {
            Name = "LifecycleTest",
            Description = "Full lifecycle test",
            CategoryType = "Standard",
            DisplayOrder = 10,
            IsActive = true
        };
        var created = await _domainFacade.CreateCategory(category);
        Assert.IsNotNull(created, "Create failed");
        Console.WriteLine($"1. Created category: {created.Id}");

        // Read
        var retrieved = await _domainFacade.GetCategoryById(created.Id);
        Assert.IsNotNull(retrieved, "Read failed");
        Assert.AreEqual(created.Name, retrieved.Name, "Name mismatch after read");
        Console.WriteLine($"2. Retrieved category: {retrieved.Name}");

        // Update
        retrieved.Description = "Updated description";
        retrieved.DisplayOrder = 20;
        var updated = await _domainFacade.UpdateCategory(retrieved);
        Assert.AreEqual("Updated description", updated.Description, "Update failed");
        Assert.AreEqual(20, updated.DisplayOrder, "Display order update failed");
        Console.WriteLine($"3. Updated category: {updated.Description}");

        // Delete
        var deleted = await _domainFacade.DeleteCategory(updated.Id);
        Assert.IsTrue(deleted, "Delete failed");
        var afterDelete = await _domainFacade.GetCategoryById(updated.Id);
        Assert.IsNull(afterDelete, "Category still exists after delete");
        Console.WriteLine($"4. Deleted category: {updated.Id}");

        Console.WriteLine("Full lifecycle test completed successfully");
    }
}

