using System;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Category in API responses
/// </summary>
public class CategoryResource
{
    /// <summary>
    /// The unique identifier of the Category
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the category (required)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the category (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of category (Standard or DailyBlog)
    /// </summary>
    public string CategoryType { get; set; } = "Standard";

    /// <summary>
    /// The display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this category is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this Category was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Category was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Category
/// </summary>
public class CreateCategoryResource
{
    /// <summary>
    /// The name of the category (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the category (optional)
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of category (Standard or DailyBlog)
    /// </summary>
    [Required(ErrorMessage = "CategoryType is required")]
    [RegularExpression("^(Standard|DailyBlog)$", ErrorMessage = "CategoryType must be either 'Standard' or 'DailyBlog'")]
    public string CategoryType { get; set; } = "Standard";

    /// <summary>
    /// The display order for UI sorting (default: 0)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this category is active (default: true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating an existing Category
/// </summary>
public class UpdateCategoryResource
{
    /// <summary>
    /// The name of the category
    /// </summary>
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// The description of the category
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of category (Standard or DailyBlog)
    /// </summary>
    [RegularExpression("^(Standard|DailyBlog)$", ErrorMessage = "CategoryType must be either 'Standard' or 'DailyBlog'")]
    public string? CategoryType { get; set; }

    /// <summary>
    /// The display order for UI sorting
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Whether this category is active
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for searching Categories
/// </summary>
public class SearchCategoryRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by Name (partial match, case insensitive)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by category type
    /// </summary>
    public string? CategoryType { get; set; }

    /// <summary>
    /// Filter by active/inactive status
    /// </summary>
    public bool? IsActive { get; set; }
}

