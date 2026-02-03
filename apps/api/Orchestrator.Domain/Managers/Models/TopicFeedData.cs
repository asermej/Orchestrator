using System;

namespace Orchestrator.Domain;

/// <summary>
/// Represents enriched topic data for feed display, including author information and engagement metrics
/// </summary>
public class TopicFeedData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AgentId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    
    // Author information
    public Guid AuthorId { get; set; }
    public string? AuthorFirstName { get; set; }
    public string? AuthorLastName { get; set; }
    public string? AuthorProfileImageUrl { get; set; }
    
    // Engagement metrics
    public int ChatCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

