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

    /// <summary>
    /// The system prompt for the agent's AI behavior
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// The interview guidelines for the agent
    /// </summary>
    public string? InterviewGuidelines { get; set; }

    /// <summary>
    /// The ElevenLabs voice ID for text-to-speech
    /// </summary>
    public string? ElevenlabsVoiceId { get; set; }

    /// <summary>
    /// The voice stability setting (0.0 to 1.0)
    /// </summary>
    public decimal VoiceStability { get; set; }

    /// <summary>
    /// The voice similarity boost setting (0.0 to 1.0)
    /// </summary>
    public decimal VoiceSimilarityBoost { get; set; }

    /// <summary>
    /// The voice provider (e.g., ElevenLabs)
    /// </summary>
    public string? VoiceProvider { get; set; }

    /// <summary>
    /// The voice type (e.g., cloned, preset)
    /// </summary>
    public string? VoiceType { get; set; }

    /// <summary>
    /// The voice name
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// When this Agent was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Agent was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
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
    /// The ATS organization this agent is scoped to (null = group-wide)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The display name of the agent
    /// </summary>
    [Required(ErrorMessage = "DisplayName is required")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The profile image URL for this agent
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// The system prompt for the agent's AI behavior
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// The interview guidelines for the agent
    /// </summary>
    public string? InterviewGuidelines { get; set; }

    /// <summary>
    /// The ElevenLabs voice ID for text-to-speech
    /// </summary>
    public string? ElevenlabsVoiceId { get; set; }

    /// <summary>
    /// The voice stability setting (0.0 to 1.0)
    /// </summary>
    public decimal VoiceStability { get; set; } = 0.50m;

    /// <summary>
    /// The voice similarity boost setting (0.0 to 1.0)
    /// </summary>
    public decimal VoiceSimilarityBoost { get; set; } = 0.75m;

    /// <summary>
    /// The voice provider (e.g., ElevenLabs)
    /// </summary>
    public string? VoiceProvider { get; set; }

    /// <summary>
    /// The voice type (e.g., cloned, preset)
    /// </summary>
    public string? VoiceType { get; set; }

    /// <summary>
    /// The voice name
    /// </summary>
    public string? VoiceName { get; set; }
}

/// <summary>
/// Request model for updating an existing Agent
/// </summary>
public class UpdateAgentResource
{
    /// <summary>
    /// The display name of the agent
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The profile image URL for this agent
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// The system prompt for the agent's AI behavior
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// The interview guidelines for the agent
    /// </summary>
    public string? InterviewGuidelines { get; set; }

    /// <summary>
    /// The ElevenLabs voice ID for text-to-speech
    /// </summary>
    public string? ElevenlabsVoiceId { get; set; }

    /// <summary>
    /// The voice stability setting (0.0 to 1.0)
    /// </summary>
    public decimal? VoiceStability { get; set; }

    /// <summary>
    /// The voice similarity boost setting (0.0 to 1.0)
    /// </summary>
    public decimal? VoiceSimilarityBoost { get; set; }

    /// <summary>
    /// The voice provider (e.g., ElevenLabs)
    /// </summary>
    public string? VoiceProvider { get; set; }

    /// <summary>
    /// The voice type (e.g., cloned, preset)
    /// </summary>
    public string? VoiceType { get; set; }

    /// <summary>
    /// The voice name
    /// </summary>
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
}
