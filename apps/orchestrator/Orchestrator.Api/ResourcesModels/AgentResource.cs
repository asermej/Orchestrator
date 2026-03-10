using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an AI Interviewer Agent in API responses
/// </summary>
public class AgentResource
{
    /// <summary>
    /// The unique identifier of the Agent
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The group this agent belongs to
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// The ATS organization this agent is scoped to (null = group-wide)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The visibility scope of this agent
    /// </summary>
    public string VisibilityScope { get; set; } = "owner_only";

    /// <summary>
    /// The display name of the agent
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The profile image URL for this agent
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    public string? Tone { get; set; }
    public string? Pace { get; set; }
    public string? AcknowledgmentStyle { get; set; }
    public string? AdditionalInstructions { get; set; }

    public string? ElevenlabsVoiceId { get; set; }
    public decimal VoiceStability { get; set; }
    public decimal VoiceSimilarityBoost { get; set; }
    public string? VoiceProvider { get; set; }
    public string? VoiceType { get; set; }
    public string? VoiceName { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Agent was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether this agent is inherited from a parent organization
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// The name of the organization that owns this agent (for inherited agents)
    /// </summary>
    public string? OwnerOrganizationName { get; set; }
}

/// <summary>
/// Request model for creating a new Agent
/// </summary>
public class CreateAgentResource
{
    /// <summary>
    /// The group this agent belongs to (optional - will use default if not specified)
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// The ATS organization this agent is scoped to (required)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The visibility scope for this agent (organization_only, organization_and_descendants, descendants_only)
    /// </summary>
    public string? VisibilityScope { get; set; }

    /// <summary>
    /// The display name of the agent
    /// </summary>
    [Required(ErrorMessage = "DisplayName is required")]
    public string DisplayName { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }
    public string? Tone { get; set; }
    public string? Pace { get; set; }
    public string? AcknowledgmentStyle { get; set; }
    public string? AdditionalInstructions { get; set; }
    public string? ElevenlabsVoiceId { get; set; }
    public decimal VoiceStability { get; set; } = 0.50m;
    public decimal VoiceSimilarityBoost { get; set; } = 0.75m;
    public string? VoiceProvider { get; set; }
    public string? VoiceType { get; set; }
    public string? VoiceName { get; set; }
}

/// <summary>
/// Request model for updating an existing Agent
/// </summary>
public class UpdateAgentResource
{
    public string? DisplayName { get; set; }
    public string? VisibilityScope { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Tone { get; set; }
    public string? Pace { get; set; }
    public string? AcknowledgmentStyle { get; set; }
    public string? AdditionalInstructions { get; set; }
    public string? ElevenlabsVoiceId { get; set; }
    public decimal? VoiceStability { get; set; }
    public decimal? VoiceSimilarityBoost { get; set; }
    public string? VoiceProvider { get; set; }
    public string? VoiceType { get; set; }
    public string? VoiceName { get; set; }
}

/// <summary>
/// Lightweight agent resource for ATS integration endpoints
/// </summary>
public class AtsAgentResource
{
    /// <summary>
    /// The unique identifier of the Agent
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the agent
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The profile image URL for this agent
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Request model for searching Agents
/// </summary>
public class SearchAgentRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by group ID
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Filter by display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Filter by created by user
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Sort by field (e.g., "displayName", "createdAt")
    /// </summary>
    public new string? SortBy { get; set; }

    /// <summary>
    /// Filter by source: "local" for agents created at the current org,
    /// "inherited" for agents from ancestor orgs. Omit for legacy behavior.
    /// </summary>
    public string? Source { get; set; }
}
