using System.Linq;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Maps between domain models and resource models for Follow-up questions
/// </summary>
public static class FollowUpMapper
{
    /// <summary>
    /// Maps a domain model to a resource model
    /// </summary>
    public static FollowUpTemplateResource ToResource(FollowUpTemplate domain)
    {
        if (domain == null)
            return null!;

        return new FollowUpTemplateResource
        {
            Id = domain.Id,
            InterviewQuestionId = domain.InterviewQuestionId,
            CompetencyTag = domain.CompetencyTag,
            TriggerHints = domain.TriggerHints?.ToList(),
            CanonicalText = domain.CanonicalText,
            AllowParaphrase = domain.AllowParaphrase,
            IsApproved = domain.IsApproved,
            CreatedBy = domain.CreatedBy,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of domain models to resource models
    /// </summary>
    public static IEnumerable<FollowUpTemplateResource> ToResources(IEnumerable<FollowUpTemplate> domains)
    {
        return domains?.Select(ToResource) ?? Enumerable.Empty<FollowUpTemplateResource>();
    }

    /// <summary>
    /// Maps a create resource model to a domain model
    /// </summary>
    public static FollowUpTemplate ToDomain(CreateFollowUpTemplateResource resource, Guid interviewQuestionId)
    {
        if (resource == null)
            return null!;

        return new FollowUpTemplate
        {
            InterviewQuestionId = interviewQuestionId,
            CompetencyTag = resource.CompetencyTag,
            TriggerHints = resource.TriggerHints?.ToArray(),
            CanonicalText = resource.CanonicalText,
            AllowParaphrase = resource.AllowParaphrase,
            IsApproved = false,
            CreatedBy = "admin_created"
        };
    }

    /// <summary>
    /// Maps an update resource model to a domain model
    /// </summary>
    public static FollowUpTemplate ToDomain(UpdateFollowUpTemplateResource resource, FollowUpTemplate existing)
    {
        if (resource == null || existing == null)
            return null!;

        return new FollowUpTemplate
        {
            Id = existing.Id,
            InterviewQuestionId = existing.InterviewQuestionId,
            CompetencyTag = resource.CompetencyTag ?? existing.CompetencyTag,
            TriggerHints = resource.TriggerHints?.ToArray() ?? existing.TriggerHints,
            CanonicalText = resource.CanonicalText ?? existing.CanonicalText,
            AllowParaphrase = resource.AllowParaphrase ?? existing.AllowParaphrase,
            IsApproved = resource.IsApproved ?? existing.IsApproved,
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a domain suggestion to a resource model
    /// </summary>
    public static FollowUpSuggestionResource ToResource(FollowUpSuggestion domain)
    {
        if (domain == null)
            return null!;

        return new FollowUpSuggestionResource
        {
            Id = domain.Id,
            CompetencyTag = domain.CompetencyTag,
            TriggerHints = domain.TriggerHints ?? new List<string>(),
            CanonicalText = domain.CanonicalText,
            IsApproved = domain.IsApproved
        };
    }

    /// <summary>
    /// Maps a collection of domain suggestions to resource models
    /// </summary>
    public static IEnumerable<FollowUpSuggestionResource> ToResources(IEnumerable<FollowUpSuggestion> domains)
    {
        return domains?.Select(ToResource) ?? Enumerable.Empty<FollowUpSuggestionResource>();
    }

    /// <summary>
    /// Maps a selection result to a response resource
    /// </summary>
    public static FollowUpSelectionResponseResource ToResource(FollowUpSelectionResult result, FollowUpTemplate? template, string nextQuestionType)
    {
        return new FollowUpSelectionResponseResource
        {
            SelectedTemplateId = result.SelectedTemplateId,
            QuestionText = template?.CanonicalText,
            MatchedCompetencyTag = result.MatchedCompetencyTag,
            Rationale = result.Rationale,
            NextQuestionType = nextQuestionType
        };
    }
}
