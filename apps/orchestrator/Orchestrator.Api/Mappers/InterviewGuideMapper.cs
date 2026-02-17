using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between InterviewGuide domain objects and API models.
/// </summary>
public static class InterviewGuideMapper
{
    /// <summary>
    /// Maps an InterviewGuide domain object to an InterviewGuideResource for API responses.
    /// </summary>
    public static InterviewGuideResource ToResource(InterviewGuide guide, bool isInherited = false, string? ownerOrganizationName = null)
    {
        ArgumentNullException.ThrowIfNull(guide);

        var questions = guide.Questions?.Select(ToQuestionResource).ToList() ?? new List<InterviewGuideQuestionResource>();

        return new InterviewGuideResource
        {
            Id = guide.Id,
            GroupId = guide.GroupId,
            OrganizationId = guide.OrganizationId,
            VisibilityScope = guide.VisibilityScope,
            Name = guide.Name,
            Description = guide.Description,
            OpeningTemplate = guide.OpeningTemplate,
            ClosingTemplate = guide.ClosingTemplate,
            ScoringRubric = guide.ScoringRubric,
            IsActive = guide.IsActive,
            Questions = questions,
            QuestionCount = guide.QuestionCount > 0 ? guide.QuestionCount : questions.Count,
            CreatedAt = guide.CreatedAt,
            UpdatedAt = guide.UpdatedAt,
            CreatedBy = guide.CreatedBy,
            UpdatedBy = guide.UpdatedBy,
            IsInherited = isInherited,
            OwnerOrganizationName = ownerOrganizationName
        };
    }

    /// <summary>
    /// Maps a collection of InterviewGuide domain objects to resources.
    /// </summary>
    public static IEnumerable<InterviewGuideResource> ToResource(IEnumerable<InterviewGuide> guides, bool isInherited = false, IDictionary<Guid, string>? orgNameLookup = null)
    {
        ArgumentNullException.ThrowIfNull(guides);

        return guides.Select(g =>
        {
            string? ownerOrgName = null;
            if (orgNameLookup != null && g.OrganizationId.HasValue)
            {
                orgNameLookup.TryGetValue(g.OrganizationId.Value, out ownerOrgName);
            }
            return ToResource(g, isInherited, ownerOrgName);
        });
    }

    /// <summary>
    /// Maps an InterviewGuideQuestion domain object to a resource.
    /// </summary>
    public static InterviewGuideQuestionResource ToQuestionResource(InterviewGuideQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        return new InterviewGuideQuestionResource
        {
            Id = question.Id,
            InterviewGuideId = question.InterviewGuideId,
            Question = question.Question,
            DisplayOrder = question.DisplayOrder,
            ScoringWeight = question.ScoringWeight,
            ScoringGuidance = question.ScoringGuidance,
            FollowUpsEnabled = question.FollowUpsEnabled,
            MaxFollowUps = question.MaxFollowUps,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a CreateInterviewGuideResource to an InterviewGuide domain object.
    /// </summary>
    public static InterviewGuide ToDomain(CreateInterviewGuideResource createResource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        var guide = new InterviewGuide
        {
            GroupId = groupId,
            OrganizationId = createResource.OrganizationId,
            VisibilityScope = createResource.VisibilityScope ?? Domain.VisibilityScope.OrganizationOnly,
            Name = createResource.Name,
            Description = createResource.Description,
            OpeningTemplate = createResource.OpeningTemplate,
            ClosingTemplate = createResource.ClosingTemplate,
            ScoringRubric = createResource.ScoringRubric,
            IsActive = createResource.IsActive,
            CreatedBy = createResource.CreatedBy
        };

        if (createResource.Questions != null)
        {
            guide.Questions = createResource.Questions.Select((q, index) => new InterviewGuideQuestion
            {
                Question = q.Question,
                DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : index,
                ScoringWeight = q.ScoringWeight,
                ScoringGuidance = q.ScoringGuidance,
                FollowUpsEnabled = q.FollowUpsEnabled,
                MaxFollowUps = q.MaxFollowUps
            }).ToList();
        }

        return guide;
    }

    /// <summary>
    /// Maps an UpdateInterviewGuideResource to an InterviewGuide domain object.
    /// </summary>
    public static InterviewGuide ToDomain(UpdateInterviewGuideResource updateResource, InterviewGuide existingGuide)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingGuide);

        return new InterviewGuide
        {
            Id = existingGuide.Id,
            GroupId = existingGuide.GroupId,
            OrganizationId = existingGuide.OrganizationId,
            VisibilityScope = updateResource.VisibilityScope ?? existingGuide.VisibilityScope,
            Name = updateResource.Name ?? existingGuide.Name,
            Description = updateResource.Description ?? existingGuide.Description,
            OpeningTemplate = updateResource.OpeningTemplate ?? existingGuide.OpeningTemplate,
            ClosingTemplate = updateResource.ClosingTemplate ?? existingGuide.ClosingTemplate,
            ScoringRubric = updateResource.ScoringRubric ?? existingGuide.ScoringRubric,
            IsActive = updateResource.IsActive ?? existingGuide.IsActive,
            CreatedAt = existingGuide.CreatedAt,
            CreatedBy = existingGuide.CreatedBy,
            UpdatedBy = updateResource.UpdatedBy
        };
    }

    /// <summary>
    /// Maps a list of CreateInterviewGuideQuestionResource to domain objects.
    /// </summary>
    public static List<InterviewGuideQuestion> ToQuestionsDomain(List<CreateInterviewGuideQuestionResource> questions)
    {
        if (questions == null) return new List<InterviewGuideQuestion>();

        return questions.Select((q, index) => new InterviewGuideQuestion
        {
            Question = q.Question,
            DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : index,
            ScoringWeight = q.ScoringWeight,
            ScoringGuidance = q.ScoringGuidance,
            FollowUpsEnabled = q.FollowUpsEnabled,
            MaxFollowUps = q.MaxFollowUps
        }).ToList();
    }
}
