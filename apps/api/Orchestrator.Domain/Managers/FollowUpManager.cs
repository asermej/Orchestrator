using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Follow-up questions
/// </summary>
internal sealed class FollowUpManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    public FollowUpManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Generates follow-up suggestions for a main question using AI (for Interview Questions)
    /// </summary>
    public async Task<List<FollowUpSuggestion>> GenerateFollowUpSuggestions(Guid questionId, string questionText)
    {
        return await GenerateFollowUpSuggestionsInternal(questionId, null, questionText).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates follow-up suggestions for a main question using AI (for Interview Configuration Questions)
    /// </summary>
    public async Task<List<FollowUpSuggestion>> GenerateFollowUpSuggestionsForConfigQuestion(Guid configQuestionId, string questionText)
    {
        return await GenerateFollowUpSuggestionsInternal(null, configQuestionId, questionText).ConfigureAwait(false);
    }

    private async Task<List<FollowUpSuggestion>> GenerateFollowUpSuggestionsInternal(Guid? questionId, Guid? configQuestionId, string questionText)
    {
        var systemPrompt = @"You are an expert interview question designer. Generate 6-10 relevant follow-up questions for the given main interview question.

Rules:
- Questions must be job-related and directly relevant to the main question
- Avoid protected class topics (age, race, religion, disability, family status, etc.)
- Use safe templates like: 'Can you give a specific example of...?', 'What steps did you take when...?', 'How did you ensure safety/compliance when...?'
- Keep each follow-up short and clear (under 100 words)
- Each follow-up should explore a different competency or aspect
- Return ONLY valid JSON array, no other text

Return format (strict JSON array):
[
  {
    ""competencyTag"": ""Safety"",
    ""triggerHints"": [""safety"", ""protocol"", ""procedure""],
    ""canonicalText"": ""Can you give a specific example of how you ensured safety in that situation?""
  },
  ...
]";

        var userPrompt = $"Main question: {questionText}\n\nGenerate 6-10 follow-up questions as a JSON array.";

        var chatHistory = new List<ConversationTurn>
        {
            new ConversationTurn { Role = "user", Content = userPrompt }
        };

        var response = await GatewayFacade.GenerateChatCompletion(systemPrompt, chatHistory).ConfigureAwait(false);

        // Parse JSON response
        try
        {
            var suggestions = JsonSerializer.Deserialize<List<FollowUpSuggestion>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (suggestions == null || !suggestions.Any())
            {
                throw new FollowUpGenerationException("AI returned empty or invalid suggestions");
            }

            // Validate and create templates
            var templates = new List<FollowUpTemplate>();
            foreach (var suggestion in suggestions)
            {
                if (string.IsNullOrWhiteSpace(suggestion.CanonicalText))
                {
                    continue; // Skip invalid suggestions
                }

                var template = new FollowUpTemplate
                {
                    InterviewQuestionId = questionId,
                    InterviewConfigurationQuestionId = configQuestionId,
                    CompetencyTag = suggestion.CompetencyTag,
                    TriggerHints = suggestion.TriggerHints?.ToArray(),
                    CanonicalText = suggestion.CanonicalText.Trim(),
                    AllowParaphrase = false,
                    IsApproved = false
                };
                template.CreatedBy = "ai_suggested";

                templates.Add(template);
            }

            // Save templates to database
            var savedTemplates = new List<FollowUpSuggestion>();
            foreach (var template in templates)
            {
                var saved = await DataFacade.AddFollowUpTemplate(template).ConfigureAwait(false);
                savedTemplates.Add(new FollowUpSuggestion
                {
                    Id = saved.Id,
                    CompetencyTag = saved.CompetencyTag,
                    TriggerHints = saved.TriggerHints?.ToList() ?? new List<string>(),
                    CanonicalText = saved.CanonicalText,
                    IsApproved = saved.IsApproved
                });
            }

            return savedTemplates;
        }
        catch (JsonException ex)
        {
            throw new FollowUpGenerationException($"Failed to parse AI response as JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Approves multiple follow-up templates
    /// </summary>
    public async Task ApproveFollowUps(List<Guid> templateIds)
    {
        if (templateIds == null || !templateIds.Any())
        {
            return;
        }

        await DataFacade.BulkApproveFollowUpTemplates(templateIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets eligible follow-up templates for a question, excluding already asked ones and checking budgets (for Interview Questions)
    /// </summary>
    public async Task<List<FollowUpTemplate>> GetEligibleFollowUps(Guid questionId, List<Guid> alreadyAskedIds, FollowUpBudgets budgets)
    {
        // Get approved follow-ups for this question
        var allApproved = await DataFacade.GetApprovedFollowUpTemplatesByInterviewQuestionId(questionId).ConfigureAwait(false);

        // Filter out already asked ones
        var eligible = allApproved
            .Where(t => !alreadyAskedIds.Contains(t.Id))
            .ToList();

        // Check budgets - hard limit of 2 per question
        if (budgets.FollowUpsAskedForQuestion >= Math.Min(budgets.MaxFollowUpsPerQuestion, 2))
        {
            return new List<FollowUpTemplate>(); // Budget exhausted for this question
        }

        // Check total interview budget - hard limit of 4 per interview
        if (budgets.TotalFollowUpsAsked >= Math.Min(budgets.MaxFollowUpsPerInterview, 4))
        {
            return new List<FollowUpTemplate>(); // Total budget exhausted
        }

        return eligible;
    }

    /// <summary>
    /// Selects a follow-up question using AI, with fallback to keyword matching
    /// </summary>
    public async Task<FollowUpSelectionResult> SelectFollowUp(
        Guid interviewId,
        Guid questionId,
        string answerText,
        List<Guid> alreadyAskedIds,
        FollowUpBudgets budgets)
    {
        // Pre-filter by answer quality
        if (string.IsNullOrWhiteSpace(answerText) || answerText.Trim().Length < 20)
        {
            // Too short - skip follow-up
            await LogSelection(interviewId, questionId, answerText, new List<Guid>(), null, null, "Answer too short", "rules_fallback").ConfigureAwait(false);
            return new FollowUpSelectionResult { SelectedTemplateId = null, Rationale = "Answer too short (< 20 characters)" };
        }

        // Get eligible follow-ups
        var eligible = await GetEligibleFollowUps(questionId, alreadyAskedIds, budgets).ConfigureAwait(false);

        if (!eligible.Any())
        {
            await LogSelection(interviewId, questionId, answerText, new List<Guid>(), null, null, "No eligible follow-ups", "rules_fallback").ConfigureAwait(false);
            return new FollowUpSelectionResult { SelectedTemplateId = null, Rationale = "No eligible follow-ups available" };
        }

        // For very short answers (20-50 chars), only consider "clarify" style follow-ups
        if (answerText.Trim().Length < 50)
        {
            var clarifyFollowUps = eligible.Where(t => 
                t.CanonicalText.ToLower().Contains("clarify") || 
                t.CanonicalText.ToLower().Contains("can you explain") ||
                t.CanonicalText.ToLower().Contains("what do you mean")
            ).ToList();

            if (!clarifyFollowUps.Any())
            {
                await LogSelection(interviewId, questionId, answerText, eligible.Select(t => t.Id).ToList(), null, null, "No clarify follow-ups for short answer", "rules_fallback").ConfigureAwait(false);
                return new FollowUpSelectionResult { SelectedTemplateId = null, Rationale = "Answer too short and no clarify follow-ups available" };
            }

            eligible = clarifyFollowUps;
        }

        // Try AI selection
        try
        {
            var selected = await SelectFollowUpWithAI(answerText, eligible).ConfigureAwait(false);

            if (selected != null && selected.SelectedTemplateId.HasValue && selected.ShouldAsk)
            {
                // Validate selected ID is in eligible list
                if (!eligible.Any(t => t.Id == selected.SelectedTemplateId.Value))
                {
                    throw new FollowUpSelectionException($"AI selected invalid template ID: {selected.SelectedTemplateId.Value}");
                }

                var selectedTemplate = eligible.First(t => t.Id == selected.SelectedTemplateId.Value);
                await LogSelection(interviewId, questionId, answerText, eligible.Select(t => t.Id).ToList(), 
                    selected.SelectedTemplateId, selectedTemplate.CompetencyTag, selected.Rationale, "ai_select").ConfigureAwait(false);

                return new FollowUpSelectionResult
                {
                    SelectedTemplateId = selected.SelectedTemplateId,
                    MatchedCompetencyTag = selectedTemplate.CompetencyTag,
                    Rationale = selected.Rationale
                };
            }
            else
            {
                // AI decided not to ask
                await LogSelection(interviewId, questionId, answerText, eligible.Select(t => t.Id).ToList(), 
                    null, null, selected?.Rationale ?? "AI determined follow-up not needed", "ai_select").ConfigureAwait(false);
                return new FollowUpSelectionResult { SelectedTemplateId = null, Rationale = selected?.Rationale ?? "AI determined follow-up not needed" };
            }
        }
        catch
        {
            // Fallback to keyword matching
            var keywordMatch = SelectFollowUpWithKeywords(answerText, eligible);
            
            await LogSelection(interviewId, questionId, answerText, eligible.Select(t => t.Id).ToList(), 
                keywordMatch?.Id, keywordMatch?.CompetencyTag, "Keyword match fallback", "rules_fallback").ConfigureAwait(false);

            return new FollowUpSelectionResult
            {
                SelectedTemplateId = keywordMatch?.Id,
                MatchedCompetencyTag = keywordMatch?.CompetencyTag,
                Rationale = keywordMatch != null ? "Keyword match fallback" : "No keyword match found"
            };
        }
    }

    private async Task<AIFollowUpSelection?> SelectFollowUpWithAI(string answerText, List<FollowUpTemplate> candidates)
    {
        var candidateList = candidates.Select(c => new
        {
            id = c.Id.ToString(),
            competencyTag = c.CompetencyTag ?? "",
            canonicalText = c.CanonicalText,
            triggerHints = c.TriggerHints ?? Array.Empty<string>()
        }).ToList();

        var systemPrompt = @"You are an expert interviewer. Analyze the candidate's answer and determine if a follow-up question would be valuable.

Rules:
- Only select a follow-up if the answer would benefit from clarification, deeper exploration, or specific examples
- Return shouldAsk: false if the answer is already complete, comprehensive, or off-topic
- Return shouldAsk: false if the answer is too brief or vague to warrant a follow-up
- Return null/selectedId: null if no follow-up makes sense, even if eligible templates exist
- selectedId must be one of the provided candidate IDs or null
- rationale must be <= 200 characters

Return format (strict JSON):
{
  ""selectedId"": ""<uuid or null>"",
  ""matchedCompetencyTag"": ""<tag or null>"",
  ""rationale"": ""<brief explanation>"",
  ""shouldAsk"": <true or false>
}";

        var userPrompt = $@"Candidate's answer: {answerText}

Available follow-up questions:
{JsonSerializer.Serialize(candidateList, new JsonSerializerOptions { WriteIndented = true })}

Select the most appropriate follow-up question, or return null if none are needed.";

        var chatHistory = new List<ConversationTurn>
        {
            new ConversationTurn { Role = "user", Content = userPrompt }
        };

        var response = await GatewayFacade.GenerateChatCompletion(systemPrompt, chatHistory).ConfigureAwait(false);

        try
        {
            var selection = JsonSerializer.Deserialize<AIFollowUpSelection>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (selection == null)
            {
                return null;
            }

            // Validate rationale length
            if (!string.IsNullOrEmpty(selection.Rationale) && selection.Rationale.Length > 200)
            {
                selection.Rationale = selection.Rationale.Substring(0, 200);
            }

            // Parse selectedId to Guid if not null
            if (!string.IsNullOrEmpty(selection.SelectedId) && Guid.TryParse(selection.SelectedId, out var selectedGuid))
            {
                selection.SelectedTemplateId = selectedGuid;
            }
            else
            {
                selection.SelectedTemplateId = null;
            }

            return selection;
        }
        catch (JsonException)
        {
            return null; // Invalid JSON, will fallback to keyword matching
        }
    }

    private FollowUpTemplate? SelectFollowUpWithKeywords(string answerText, List<FollowUpTemplate> candidates)
    {
        var answerLower = answerText.ToLower();

        foreach (var candidate in candidates)
        {
            if (candidate.TriggerHints == null || !candidate.TriggerHints.Any())
            {
                continue;
            }

            // Check if any trigger hint appears in the answer
            foreach (var hint in candidate.TriggerHints)
            {
                if (answerLower.Contains(hint.ToLower()))
                {
                    return candidate; // First match wins
                }
            }
        }

        return null; // No keyword match
    }

    private async Task LogSelection(
        Guid interviewId,
        Guid questionId,
        string answerText,
        List<Guid> candidateIds,
        Guid? selectedId,
        string? competencyTag,
        string? rationale,
        string method)
    {
        var excerpt = answerText.Length > 300 ? answerText.Substring(0, 300) : answerText;

        var log = new FollowUpSelectionLog
        {
            InterviewId = interviewId,
            InterviewQuestionId = questionId,
            AnswerExcerpt = excerpt,
            CandidateTemplateIdsPresented = candidateIds.ToArray(),
            SelectedTemplateId = selectedId,
            MatchedCompetencyTag = competencyTag,
            Rationale = rationale?.Length > 200 ? rationale.Substring(0, 200) : rationale,
            Method = method,
            Timestamp = DateTime.UtcNow
        };

        await DataFacade.AddFollowUpSelectionLog(log).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade and GatewayFacade don't implement IDisposable, so no disposal needed
    }
}

/// <summary>
/// Represents a follow-up suggestion from AI
/// </summary>
public class FollowUpSuggestion
{
    public Guid? Id { get; set; }
    public string? CompetencyTag { get; set; }
    public List<string> TriggerHints { get; set; } = new List<string>();
    public string CanonicalText { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

/// <summary>
/// Result of AI follow-up selection
/// </summary>
internal class AIFollowUpSelection
{
    public string? SelectedId { get; set; }
    public Guid? SelectedTemplateId { get; set; }
    public string? MatchedCompetencyTag { get; set; }
    public string? Rationale { get; set; }
    public bool ShouldAsk { get; set; } = true;
}

/// <summary>
/// Result of follow-up selection
/// </summary>
public class FollowUpSelectionResult
{
    public Guid? SelectedTemplateId { get; set; }
    public string? MatchedCompetencyTag { get; set; }
    public string? Rationale { get; set; }
}

/// <summary>
/// Exception thrown when follow-up generation fails
/// </summary>
public class FollowUpGenerationException : Exception
{
    public FollowUpGenerationException(string message) : base(message) { }
    public FollowUpGenerationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when follow-up selection fails
/// </summary>
public class FollowUpSelectionException : Exception
{
    public FollowUpSelectionException(string message) : base(message) { }
}
