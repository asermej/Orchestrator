using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new InterviewGuide with optional questions
    /// </summary>
    public async Task<InterviewGuide> CreateInterviewGuide(InterviewGuide guide)
    {
        return await InterviewGuideManager.CreateGuide(guide).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewGuide by ID
    /// </summary>
    public async Task<InterviewGuide?> GetInterviewGuideById(Guid id)
    {
        return await InterviewGuideManager.GetGuideById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewGuide by ID with all questions
    /// </summary>
    public async Task<InterviewGuide?> GetInterviewGuideByIdWithQuestions(Guid id)
    {
        return await InterviewGuideManager.GetGuideByIdWithQuestions(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for InterviewGuides
    /// </summary>
    public async Task<PaginatedResult<InterviewGuide>> SearchInterviewGuides(
        Guid? groupId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await InterviewGuideManager.SearchGuides(groupId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewGuide
    /// </summary>
    public async Task<InterviewGuide> UpdateInterviewGuide(InterviewGuide guide)
    {
        return await InterviewGuideManager.UpdateGuide(guide).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewGuide and replaces all its questions
    /// </summary>
    public async Task<InterviewGuide> UpdateInterviewGuideWithQuestions(InterviewGuide guide, List<InterviewGuideQuestion> questions)
    {
        return await InterviewGuideManager.UpdateGuideWithQuestions(guide, questions).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an InterviewGuide (soft delete)
    /// </summary>
    public async Task<bool> DeleteInterviewGuide(Guid id, string? deletedBy = null)
    {
        return await InterviewGuideManager.DeleteGuide(id, deletedBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a question to an existing guide
    /// </summary>
    public async Task<InterviewGuideQuestion> AddInterviewGuideQuestion(InterviewGuideQuestion question)
    {
        return await InterviewGuideManager.AddQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a guide question
    /// </summary>
    public async Task<InterviewGuideQuestion> UpdateInterviewGuideQuestion(InterviewGuideQuestion question)
    {
        return await InterviewGuideManager.UpdateQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a guide question
    /// </summary>
    public async Task<bool> DeleteInterviewGuideQuestion(Guid questionId)
    {
        return await InterviewGuideManager.DeleteQuestion(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all questions for a guide
    /// </summary>
    public async Task<List<InterviewGuideQuestion>> GetInterviewGuideQuestions(Guid guideId)
    {
        return await InterviewGuideManager.GetQuestionsByGuideId(guideId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an interview guide question by ID
    /// </summary>
    public async Task<InterviewGuideQuestion?> GetInterviewGuideQuestionById(Guid questionId)
    {
        var dataFacade = new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
        return await dataFacade.GetInterviewGuideQuestionById(questionId).ConfigureAwait(false);
    }
}
