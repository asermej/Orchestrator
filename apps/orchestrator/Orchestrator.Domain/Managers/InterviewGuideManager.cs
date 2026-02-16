using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for InterviewGuide entities
/// </summary>
internal sealed class InterviewGuideManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewGuideManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new InterviewGuide with optional questions
    /// </summary>
    public async Task<InterviewGuide> CreateGuide(InterviewGuide guide)
    {
        InterviewGuideValidator.Validate(guide);

        // Create the guide
        var createdGuide = await DataFacade.AddInterviewGuide(guide).ConfigureAwait(false);

        // Add questions if provided
        if (guide.Questions != null && guide.Questions.Count > 0)
        {
            for (int i = 0; i < guide.Questions.Count; i++)
            {
                var question = guide.Questions[i];
                question.InterviewGuideId = createdGuide.Id;
                question.DisplayOrder = i;
                await DataFacade.AddInterviewGuideQuestion(question).ConfigureAwait(false);
            }

            // Reload with questions
            createdGuide = await DataFacade.GetInterviewGuideByIdWithQuestions(createdGuide.Id).ConfigureAwait(false);
        }

        return createdGuide!;
    }

    /// <summary>
    /// Gets an InterviewGuide by ID
    /// </summary>
    public async Task<InterviewGuide?> GetGuideById(Guid id)
    {
        return await DataFacade.GetInterviewGuideById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewGuide by ID with all questions
    /// </summary>
    public async Task<InterviewGuide?> GetGuideByIdWithQuestions(Guid id)
    {
        return await DataFacade.GetInterviewGuideByIdWithQuestions(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for InterviewGuides
    /// </summary>
    public async Task<PaginatedResult<InterviewGuide>> SearchGuides(
        Guid? groupId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await DataFacade.SearchInterviewGuides(groupId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewGuide
    /// </summary>
    public async Task<InterviewGuide> UpdateGuide(InterviewGuide guide)
    {
        InterviewGuideValidator.Validate(guide);
        return await DataFacade.UpdateInterviewGuide(guide).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewGuide and replaces all its questions
    /// </summary>
    public async Task<InterviewGuide> UpdateGuideWithQuestions(InterviewGuide guide, List<InterviewGuideQuestion> questions)
    {
        InterviewGuideValidator.Validate(guide);

        // Validate all questions
        foreach (var question in questions)
        {
            question.InterviewGuideId = guide.Id;
            InterviewGuideValidator.ValidateQuestion(question);
        }

        // Update the guide
        var updatedGuide = await DataFacade.UpdateInterviewGuide(guide).ConfigureAwait(false);

        // Replace all questions
        await DataFacade.ReplaceInterviewGuideQuestions(guide.Id, questions).ConfigureAwait(false);

        // Reload with questions
        return (await DataFacade.GetInterviewGuideByIdWithQuestions(guide.Id).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Deletes an InterviewGuide (soft delete)
    /// </summary>
    public async Task<bool> DeleteGuide(Guid id, string? deletedBy = null)
    {
        return await DataFacade.DeleteInterviewGuide(id, deletedBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a question to an existing guide
    /// </summary>
    public async Task<InterviewGuideQuestion> AddQuestion(InterviewGuideQuestion question)
    {
        InterviewGuideValidator.ValidateQuestion(question);
        return await DataFacade.AddInterviewGuideQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a question
    /// </summary>
    public async Task<InterviewGuideQuestion> UpdateQuestion(InterviewGuideQuestion question)
    {
        InterviewGuideValidator.ValidateQuestion(question);
        return await DataFacade.UpdateInterviewGuideQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a question
    /// </summary>
    public async Task<bool> DeleteQuestion(Guid questionId)
    {
        return await DataFacade.DeleteInterviewGuideQuestion(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all questions for a guide
    /// </summary>
    public async Task<List<InterviewGuideQuestion>> GetQuestionsByGuideId(Guid guideId)
    {
        return await DataFacade.GetInterviewGuideQuestions(guideId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
