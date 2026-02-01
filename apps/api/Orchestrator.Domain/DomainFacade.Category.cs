using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Category> CreateCategory(Category category)
    {
        return await CategoryManager.CreateCategory(category);
    }

    public async Task<Category?> GetCategoryById(Guid id)
    {
        return await CategoryManager.GetCategoryById(id);
    }

    public async Task<PaginatedResult<Category>> SearchCategories(string? name, string? categoryType, bool? isActive, int pageNumber, int pageSize)
    {
        return await CategoryManager.SearchCategories(name, categoryType, isActive, pageNumber, pageSize);
    }

    public async Task<Category> UpdateCategory(Category category)
    {
        return await CategoryManager.UpdateCategory(category);
    }

    public async Task<bool> DeleteCategory(Guid id)
    {
        return await CategoryManager.DeleteCategory(id);
    }
}

