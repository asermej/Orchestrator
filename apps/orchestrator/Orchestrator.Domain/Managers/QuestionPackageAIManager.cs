using System.Text.Json;

namespace Orchestrator.Domain;

internal sealed class QuestionPackageAIManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    public QuestionPackageAIManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<List<AISuggestedCompetency>> GenerateCompetencySuggestions(string roleName, string industry)
    {
        var systemPrompt = @"You are an expert in structured behavioral interviewing and competency-based assessment design.
Given a job role and industry, suggest 3-5 competencies that should be assessed during an interview.

Respond with ONLY a JSON array (no markdown, no explanation). Each element:
{
  ""name"": ""Competency Name"",
  ""defaultWeight"": 25,
  ""description"": ""One sentence describing what this competency measures""
}

Weights must sum to 100. Use clear, professional competency names.";

        var userMessage = $"Role: {roleName}\nIndustry: {industry}";
        var response = await CallAI(systemPrompt, userMessage).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<AISuggestedCompetency>>(response, JsonOpts) ?? new();
    }

    /// <summary>
    /// Suggests a single canonical example question for a competency (model for tone/length, not a script).
    /// </summary>
    public async Task<string> GenerateCanonicalExample(string competencyName, string? description, string roleContext)
    {
        var systemPrompt = @"You are an expert in structured behavioral interviewing.
Given a competency name, optional description, and role context, suggest ONE example interview question that an interviewer could use as a model for tone and framing. The question should be conversational, open-ended, and prompt the candidate to share a specific past experience. Do not output JSON or markdown—output only the single question text, one sentence or short paragraph.";

        var userMessage = $"Competency: {competencyName}\n"
            + (string.IsNullOrWhiteSpace(description) ? "" : $"Description: {description}\n")
            + $"Role context: {roleContext}";

        var response = await CallAI(systemPrompt, userMessage).ConfigureAwait(false);
        return response?.Trim() ?? "Tell me about a time when you demonstrated this.";
    }

    private async Task<string> CallAI(string systemPrompt, string userMessage)
    {
        var history = new List<ConversationTurn>
        {
            new() { Role = "user", Content = userMessage }
        };
        var raw = await GatewayFacade.GenerateAnthropicCompletion(systemPrompt, history).ConfigureAwait(false);

        // Strip markdown code fences if present
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3].Trim();
        }

        return trimmed;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
    }
}

// --- AI suggestion DTOs ---

public class AISuggestedCompetency
{
    public string Name { get; set; } = string.Empty;
    public int DefaultWeight { get; set; }
    public string Description { get; set; } = string.Empty;
}

