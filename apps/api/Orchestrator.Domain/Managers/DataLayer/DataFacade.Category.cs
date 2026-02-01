using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private CategoryDataManager CategoryDataManager => new(_dbConnectionString);

    public Task<Category> AddCategory(Category category)
    {
        return CategoryDataManager.Add(category);
    }

    public async Task<Category?> GetCategoryById(Guid id)
    {
        return await CategoryDataManager.GetById(id);
    }
    
    public Task<Category> UpdateCategory(Category category)
    {
        return CategoryDataManager.Update(category);
    }

    public Task<bool> DeleteCategory(Guid id)
    {
        return CategoryDataManager.Delete(id);
    }

    public Task<PaginatedResult<Category>> SearchCategories(string? name, string? categoryType, bool? isActive, int pageNumber, int pageSize)
    {
        return CategoryDataManager.Search(name, categoryType, isActive, pageNumber, pageSize);
    }

    public async Task<Category?> GetCategoryByName(string name)
    {
        return await CategoryDataManager.GetByName(name);
    }
}

