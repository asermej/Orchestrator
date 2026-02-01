using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Category domain objects and CategoryResource API models.
/// </summary>
public static class CategoryMapper
{
    /// <summary>
    /// Maps a Category domain object to a CategoryResource for API responses.
    /// </summary>
    /// <param name="category">The domain Category object to map</param>
    /// <returns>A CategoryResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null</exception>
    public static CategoryResource ToResource(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryResource
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CategoryType = category.CategoryType,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Category domain objects to CategoryResource objects.
    /// </summary>
    /// <param name="categories">The collection of domain Category objects to map</param>
    /// <returns>A collection of CategoryResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when categories is null</exception>
    public static IEnumerable<CategoryResource> ToResource(IEnumerable<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        return categories.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateCategoryResource to a Category domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateCategoryResource from API request</param>
    /// <returns>A Category domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Category ToDomain(CreateCategoryResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Category
        {
            Name = createResource.Name,
            Description = createResource.Description,
            CategoryType = createResource.CategoryType,
            DisplayOrder = createResource.DisplayOrder,
            IsActive = createResource.IsActive
        };
    }

    /// <summary>
    /// Maps an UpdateCategoryResource to a Category domain object for updates.
    /// </summary>
    /// <param name="updateResource">The UpdateCategoryResource from API request</param>
    /// <param name="existingCategory">The existing Category domain object to update</param>
    /// <returns>A Category domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingCategory is null</exception>
    public static Category ToDomain(UpdateCategoryResource updateResource, Category existingCategory)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingCategory);

        return new Category
        {
            Id = existingCategory.Id,
            Name = updateResource.Name ?? existingCategory.Name,
            Description = updateResource.Description ?? existingCategory.Description,
            CategoryType = updateResource.CategoryType ?? existingCategory.CategoryType,
            DisplayOrder = updateResource.DisplayOrder ?? existingCategory.DisplayOrder,
            IsActive = updateResource.IsActive ?? existingCategory.IsActive,
            CreatedBy = existingCategory.CreatedBy,
            CreatedAt = existingCategory.CreatedAt,
            UpdatedAt = existingCategory.UpdatedAt,
            UpdatedBy = existingCategory.UpdatedBy,
            IsDeleted = existingCategory.IsDeleted,
            DeletedAt = existingCategory.DeletedAt,
            DeletedBy = existingCategory.DeletedBy
        };
    }
}

