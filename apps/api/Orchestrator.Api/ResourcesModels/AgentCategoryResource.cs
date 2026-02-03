using System;
using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Request model for adding a category to an agent
/// </summary>
public class AddAgentCategoryResource
{
    /// <summary>
    /// The ID of the category to add (required)
    /// </summary>
    [Required(ErrorMessage = "CategoryId is required")]
    public Guid CategoryId { get; set; }
}

