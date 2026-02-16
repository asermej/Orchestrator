using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class CandidateMapper
{
    public static CandidateSessionResponse ToSessionResponse(CandidateSessionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new CandidateSessionResponse
        {
            Token = result.Token,
            Interview = ToInterviewResource(result.Interview),
            Agent = result.Agent != null ? ToAgentResource(result.Agent) : null,
            Job = result.Job != null ? ToJobResource(result.Job) : null,
            Applicant = result.Applicant != null ? ToApplicantResource(result.Applicant) : null,
            Questions = result.Questions
                .OrderBy(q => q.DisplayOrder)
                .Select(ToQuestionResource)
                .ToList(),
        };
    }

    public static CandidateQuestionResource ToQuestionResource(InterviewGuideQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        return new CandidateQuestionResource
        {
            Id = question.Id,
            Text = question.Question,
            DisplayOrder = question.DisplayOrder,
            FollowUpsEnabled = question.FollowUpsEnabled,
            MaxFollowUps = question.MaxFollowUps,
        };
    }

    public static CandidateInterviewResource ToInterviewResource(Interview interview)
    {
        ArgumentNullException.ThrowIfNull(interview);
        return new CandidateInterviewResource
        {
            Id = interview.Id,
            Status = interview.Status,
            InterviewType = interview.InterviewType,
            CurrentQuestionIndex = interview.CurrentQuestionIndex,
            StartedAt = interview.StartedAt,
            CompletedAt = interview.CompletedAt,
        };
    }

    public static CandidateAgentResource ToAgentResource(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return new CandidateAgentResource
        {
            Id = agent.Id,
            DisplayName = agent.DisplayName,
            ProfileImageUrl = agent.ProfileImageUrl,
        };
    }

    public static CandidateJobResource ToJobResource(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new CandidateJobResource
        {
            Id = job.Id,
            Title = job.Title,
        };
    }

    public static CandidateApplicantResource ToApplicantResource(Applicant applicant)
    {
        ArgumentNullException.ThrowIfNull(applicant);
        return new CandidateApplicantResource
        {
            FirstName = applicant.FirstName ?? "Candidate",
        };
    }

    public static InterviewInviteResource ToInviteResource(InterviewInvite invite, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(invite);
        return new InterviewInviteResource
        {
            Id = invite.Id,
            InterviewId = invite.InterviewId,
            GroupId = invite.GroupId,
            ShortCode = invite.ShortCode,
            Status = invite.Status,
            ExpiresAt = invite.ExpiresAt,
            MaxUses = invite.MaxUses,
            UseCount = invite.UseCount,
            InviteUrl = baseUrl != null ? $"{baseUrl}/i/{invite.ShortCode}" : null,
            CreatedAt = invite.CreatedAt,
            UpdatedAt = invite.UpdatedAt,
        };
    }
}
