using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Category entities
/// </summary>
internal sealed class CategoryManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public CategoryManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new Category
    /// </summary>
    /// <param name="category">The Category entity to create</param>
    /// <returns>The created Category</returns>
    public async Task<Category> CreateCategory(Category category)
    {
        CategoryValidator.Validate(category);
        
        // Check for duplicate name
        var existing = await DataFacade.GetCategoryByName(category.Name).ConfigureAwait(false);
        if (existing != null)
        {
            throw new CategoryDuplicateNameException($"A category with the name '{category.Name}' already exists.");
        }
        
        return await DataFacade.AddCategory(category).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Category by ID
    /// </summary>
    /// <param name="id">The ID of the Category to get</param>
    /// <returns>The Category if found, null otherwise</returns>
    public async Task<Category?> GetCategoryById(Guid id)
    {
        return await DataFacade.GetCategoryById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Categories
    /// </summary>
    /// <param name="name">Optional name to search for</param>
    /// <param name="categoryType">Optional category type to filter by</param>
    /// <param name="isActive">Optional active flag to filter by</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of Categories</returns>
    public async Task<PaginatedResult<Category>> SearchCategories(string? name, string? categoryType, bool? isActive, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchCategories(name, categoryType, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a Category
    /// </summary>
    /// <param name="category">The Category entity with updated data</param>
    /// <returns>The updated Category</returns>
    public async Task<Category> UpdateCategory(Category category)
    {
        CategoryValidator.Validate(category);
        
        // Check for duplicate name (excluding current category)
        var existing = await DataFacade.GetCategoryByName(category.Name).ConfigureAwait(false);
        if (existing != null && existing.Id != category.Id)
        {
            throw new CategoryDuplicateNameException($"A category with the name '{category.Name}' already exists.");
        }
        
        return await DataFacade.UpdateCategory(category).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a Category
    /// </summary>
    /// <param name="id">The ID of the Category to delete</param>
    /// <returns>True if the Category was deleted, false if not found</returns>
    public async Task<bool> DeleteCategory(Guid id)
    {
        return await DataFacade.DeleteCategory(id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}

