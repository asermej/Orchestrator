namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private FollowUpManager? _followUpManager;
    private FollowUpManager FollowUpManager => _followUpManager ??= new FollowUpManager(_serviceLocator);
    private DataFacade? _followUpDataFacade;
    private DataFacade FollowUpDataFacade => _followUpDataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    /// <summary>
    /// Gets an interview question by ID
    /// </summary>
    public async Task<InterviewQuestion?> GetInterviewQuestionById(Guid questionId)
    {
        var dataFacade = new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
        return await dataFacade.GetInterviewQuestionById(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates follow-up suggestions for an interview question using AI
    /// </summary>
    public async Task<List<FollowUpSuggestion>> GenerateFollowUpSuggestions(Guid questionId)
    {
        var question = await GetInterviewQuestionById(questionId).ConfigureAwait(false);
        if (question == null)
        {
            throw new InterviewQuestionNotFoundException($"Interview question with ID {questionId} not found");
        }

        return await FollowUpManager.GenerateFollowUpSuggestions(questionId, question.QuestionText).ConfigureAwait(false);
    }

    /// <summary>
    /// Approves multiple follow-up templates
    /// </summary>
    public async Task ApproveFollowUps(List<Guid> templateIds)
    {
        await FollowUpManager.ApproveFollowUps(templateIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all follow-up templates for an interview question
    /// </summary>
    public async Task<IEnumerable<FollowUpTemplate>> GetFollowUpTemplatesByQuestionId(Guid questionId)
    {
        return await FollowUpDataFacade.GetFollowUpTemplatesByInterviewQuestionId(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a follow-up template by ID
    /// </summary>
    public async Task<FollowUpTemplate?> GetFollowUpTemplateById(Guid templateId)
    {
        return await FollowUpDataFacade.GetFollowUpTemplateById(templateId).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a follow-up template
    /// </summary>
    public async Task<FollowUpTemplate> UpdateFollowUpTemplate(FollowUpTemplate template)
    {
        return await FollowUpDataFacade.UpdateFollowUpTemplate(template).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a follow-up template
    /// </summary>
    public async Task<bool> DeleteFollowUpTemplate(Guid templateId)
    {
        return await FollowUpDataFacade.DeleteFollowUpTemplate(templateId).ConfigureAwait(false);
    }

    /// <summary>
    /// Selects and returns a follow-up question for an interview response
    /// </summary>
    public async Task<FollowUpSelectionResult> SelectAndReturnFollowUp(
        Guid interviewId,
        Guid questionId,
        string answerText)
    {
        // Get the interview to check status
        var interview = await InterviewManager.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found");
        }

        // Get the question to check follow-up settings
        var question = await GetInterviewQuestionById(questionId).ConfigureAwait(false);
        if (question == null)
        {
            throw new InterviewQuestionNotFoundException($"Interview question with ID {questionId} not found");
        }

        // Check if follow-ups are enabled for this question
        if (!question.FollowUpsEnabled)
        {
            return new FollowUpSelectionResult { SelectedTemplateId = null, Rationale = "Follow-ups disabled for this question" };
        }

        // Get all responses for this interview to calculate budgets
        var allResponses = await InterviewManager.GetResponsesByInterviewId(interviewId).ConfigureAwait(false);
        
        // Calculate budgets
        var budgets = new FollowUpBudgets
        {
            MaxFollowUpsPerQuestion = Math.Min(question.MaxFollowUps, 2), // Hard limit of 2
            MaxFollowUpsPerInterview = 4, // Hard limit of 4
            FollowUpsAskedForQuestion = allResponses.Count(r => r.QuestionId == questionId && r.QuestionType == "followup"),
            TotalFollowUpsAsked = allResponses.Count(r => r.QuestionType == "followup")
        };

        // Get already asked follow-ups for this question
        var alreadyAskedIds = allResponses
            .Where(r => r.QuestionId == questionId && r.QuestionType == "followup" && r.FollowUpTemplateId.HasValue)
            .Select(r => r.FollowUpTemplateId!.Value)
            .ToList();

        // Select follow-up
        return await FollowUpManager.SelectFollowUp(interviewId, questionId, answerText, alreadyAskedIds, budgets).ConfigureAwait(false);
    }
}

/// <summary>
/// Exception thrown when an interview question is not found
/// </summary>
public class InterviewQuestionNotFoundException : Exception
{
    public InterviewQuestionNotFoundException(string message) : base(message) { }
}
