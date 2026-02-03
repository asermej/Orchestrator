using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class JobTypeMapper
{
    public static JobTypeResource ToResource(JobType jobType, IEnumerable<InterviewQuestion>? questions = null, int? questionCount = null)
    {
        ArgumentNullException.ThrowIfNull(jobType);
        var questionsList = questions?.Select(ToQuestionResource).ToList() ?? new List<InterviewQuestionResource>();
        return new JobTypeResource
        {
            Id = jobType.Id,
            OrganizationId = jobType.OrganizationId,
            Name = jobType.Name,
            Description = jobType.Description,
            IsActive = jobType.IsActive,
            InterviewDurationMinutes = 30, // Default duration until added to domain model
            QuestionCount = questionCount ?? questionsList.Count,
            CreatedAt = jobType.CreatedAt,
            UpdatedAt = jobType.UpdatedAt,
            Questions = questionsList
        };
    }

    public static IEnumerable<JobTypeResource> ToResource(IEnumerable<JobType> jobTypes)
    {
        ArgumentNullException.ThrowIfNull(jobTypes);
        return jobTypes.Select(jt => ToResource(jt));
    }

    public static InterviewQuestionResource ToQuestionResource(InterviewQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        return new InterviewQuestionResource
        {
            Id = question.Id,
            JobTypeId = question.JobTypeId,
            QuestionText = question.QuestionText,
            QuestionOrder = question.QuestionOrder,
            IsRequired = question.IsRequired,
            FollowUpPrompt = question.FollowUpPrompt,
            MaxFollowUps = question.MaxFollowUps
        };
    }

    public static JobType ToDomain(CreateJobTypeResource resource, Guid organizationId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new JobType
        {
            OrganizationId = organizationId,
            Name = resource.Name,
            Description = resource.Description
        };
    }

    public static JobType ToDomain(UpdateJobTypeResource resource, JobType existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new JobType
        {
            Id = existing.Id,
            OrganizationId = existing.OrganizationId,
            Name = resource.Name ?? existing.Name,
            Description = resource.Description ?? existing.Description,
            IsActive = resource.IsActive ?? existing.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }

    public static InterviewQuestion ToQuestionDomain(CreateInterviewQuestionResource resource, Guid jobTypeId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new InterviewQuestion
        {
            JobTypeId = jobTypeId,
            QuestionText = resource.QuestionText,
            QuestionOrder = resource.QuestionOrder,
            IsRequired = resource.IsRequired,
            FollowUpPrompt = resource.FollowUpPrompt,
            MaxFollowUps = resource.MaxFollowUps
        };
    }

    public static InterviewQuestion ToQuestionDomain(UpdateInterviewQuestionResource resource, InterviewQuestion existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new InterviewQuestion
        {
            Id = existing.Id,
            JobTypeId = existing.JobTypeId,
            QuestionText = resource.QuestionText ?? existing.QuestionText,
            QuestionOrder = resource.QuestionOrder ?? existing.QuestionOrder,
            IsRequired = resource.IsRequired ?? existing.IsRequired,
            FollowUpPrompt = resource.FollowUpPrompt ?? existing.FollowUpPrompt,
            MaxFollowUps = resource.MaxFollowUps ?? existing.MaxFollowUps,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }
}
