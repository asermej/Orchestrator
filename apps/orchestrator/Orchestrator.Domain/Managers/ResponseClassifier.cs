using System.Text;
using System.Text.Json;

namespace Orchestrator.Domain;

/// <summary>
/// Pre-processing classifier that categorizes candidate responses before STAR evaluation.
/// Completely separate from the STAR evaluator — this prompt never scores, the evaluator never classifies.
/// </summary>
internal sealed class ResponseClassifier : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);

    public ResponseClassifier(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    public async Task<ResponseClassificationResult> ClassifyResponseAsync(
        string systemPrompt,
        string candidateResponse,
        string currentQuestion,
        string competencyName,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildClassificationPrompt(candidateResponse, currentQuestion, competencyName);
        var history = new List<ConversationTurn>
        {
            new() { Role = "user", Content = prompt }
        };

        var aiResponse = await GatewayFacade.GenerateAnthropicCompletion(
            systemPrompt, history, temperatureOverride: 0.3
        ).ConfigureAwait(false);
        return ParseClassification(aiResponse);
    }

    private static string BuildClassificationPrompt(
        string candidateResponse,
        string currentQuestion,
        string competencyName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a response classifier for a structured behavioral interview.");
        sb.AppendLine("Your ONLY job is to classify the candidate's response into exactly one category.");
        sb.AppendLine("You do NOT score, evaluate, or judge the quality of the response.");
        sb.AppendLine();

        sb.AppendLine("## Interview Context");
        sb.AppendLine($"Competency being assessed: {competencyName}");
        sb.AppendLine($"Current interview question: \"{currentQuestion}\"");
        sb.AppendLine($"Candidate's response: \"{candidateResponse}\"");
        sb.AppendLine();

        sb.AppendLine("## Classification Categories");
        sb.AppendLine();

        sb.AppendLine("### on_topic");
        sb.AppendLine("The candidate is genuinely attempting to answer the interview question, even if the answer is weak or vague.");
        sb.AppendLine("If there is ANY attempt to address the question, classify as on_topic.");
        sb.AppendLine();

        sb.AppendLine("### clarification_request");
        sb.AppendLine("Candidate asks to have the question repeated, asks what it means, or says they don't understand.");
        sb.AppendLine("Examples: \"Can you repeat that?\", \"What do you mean by that?\", \"I'm not sure I understand\"");
        sb.AppendLine();

        sb.AppendLine("### nervous_deflection");
        sb.AppendLine("Candidate stalls, expresses uncertainty, or gives a tangential non-answer that seems anxiety-driven rather than deliberate avoidance.");
        sb.AppendLine("Examples: \"That's a good question, let me think...\", \"I've never really thought about that\", \"Hmm I'm not sure where to start\"");
        sb.AppendLine();

        sb.AppendLine("### process_question");
        sb.AppendLine("Candidate asks about the interview process, data usage, recording, or who will see their answers.");
        sb.AppendLine("Examples: \"Why are you asking this?\", \"Is this being recorded?\", \"Who will see my answers?\", \"How will this be used?\"");
        sb.AppendLine();

        sb.AppendLine("### off_topic");
        sb.AppendLine("Candidate responds with something completely unrelated to the interview question and not explained by nervousness.");
        sb.AppendLine("Examples: \"What day is it?\", \"What's the weather like?\", \"Do you have jobs in marketing?\"");
        sb.AppendLine();

        sb.AppendLine("### adversarial");
        sb.AppendLine("Candidate makes a political statement, accuses the AI of bias, objects to being interviewed by an AI, or attempts to manipulate or destabilize the interview.");
        sb.AppendLine("Examples: \"Do you agree with the war in Iran?\", \"You're biased against [group]\", \"This is discriminatory\", \"I don't want to talk to an AI\"");
        sb.AppendLine();

        sb.AppendLine("### refusal_to_elaborate");
        sb.AppendLine("Candidate indicates they have already answered or refuses to provide more detail on the same topic.");
        sb.AppendLine("Examples: \"I already told you\", \"I just said that\", \"I feel like I already covered that\", \"I don't have anything else to add\"");
        sb.AppendLine();

        sb.AppendLine("### language_switch_request");
        sb.AppendLine("Candidate asks to switch the interview to a different language.");
        sb.AppendLine("Examples: \"Can we do this in Spanish?\", \"Puedo hablar en español?\", \"I'm more comfortable in French\"");
        sb.AppendLine();

        sb.AppendLine("### sensitive_disclosure");
        sb.AppendLine("Candidate volunteers information about a protected characteristic, disability, medical condition, or personal hardship unprompted.");
        sb.AppendLine("Examples: Mentions a disability, health condition, pregnancy, religion, national origin, age, or expresses significant personal distress.");
        sb.AppendLine();

        sb.AppendLine("## Response Generation Rules");
        sb.AppendLine();
        sb.AppendLine("For each category, generate the appropriate response_text:");
        sb.AppendLine();

        sb.AppendLine("- **on_topic**: requires_response = false, response_text = null");
        sb.AppendLine();

        sb.AppendLine("- **clarification_request**: requires_response = true");
        sb.AppendLine("  response_text: \"Of course. [Rephrase the original question using DIFFERENT words and a DIFFERENT sentence structure — do NOT repeat the original phrasing.] Take your time.\"");
        sb.AppendLine("  consumes_redirect = false, abandon_competency = false");
        sb.AppendLine();

        sb.AppendLine("- **nervous_deflection**: requires_response = true");
        sb.AppendLine("  response_text: \"Take your time — there's no rush. Just think of a specific situation where this came up for you at work, even a small one.\"");
        sb.AppendLine("  consumes_redirect = false, abandon_competency = false");
        sb.AppendLine();

        sb.AppendLine("- **process_question**: requires_response = true");
        sb.AppendLine("  response_text: \"This interview is part of your application. Your responses help the hiring team understand your experience and background. [Answer the specific question honestly and briefly.] Now, back to the question I asked — [restate question briefly].\"");
        sb.AppendLine("  consumes_redirect = false, abandon_competency = false");
        sb.AppendLine();

        sb.AppendLine("- **off_topic**: requires_response = true");
        sb.AppendLine("  response_text: \"I'm not able to help with that, but I'd love to hear your answer to the question I asked. [Restate question briefly.]\"");
        sb.AppendLine("  consumes_redirect = true, abandon_competency = false");
        sb.AppendLine();

        sb.AppendLine("- **refusal_to_elaborate**: requires_response = true");
        sb.AppendLine("  response_text: \"I appreciate your answer, thank you. Let's move on.\"");
        sb.AppendLine("  consumes_redirect = false, abandon_competency = true");
        sb.AppendLine();

        sb.AppendLine("- **language_switch_request**: requires_response = true");
        sb.AppendLine("  response_text: \"Of course! [Restate the current interview question in the requested language.]\"");
        sb.AppendLine("  consumes_redirect = false, abandon_competency = false");
        sb.AppendLine();

        sb.AppendLine("- **adversarial**: requires_response = true, consumes_redirect = false, abandon_competency = false");
        sb.AppendLine("  Apply these sub-rules in order for response_text:");
        sb.AppendLine("  * Political opinion requests: \"I'm not able to discuss that topic, but I'm here to learn more about your experience. [Restate question.]\"");
        sb.AppendLine("  * Bias or discrimination accusations: \"I understand your concern. If you'd like to speak with someone from the hiring team directly, please contact them to discuss. I'm here to give you the opportunity to share your experience.\"");
        sb.AppendLine("  * Objects to talking to an AI: \"I completely understand. You're welcome to contact the hiring team directly to arrange an alternative. If you'd like to continue, I'm ready when you are.\"");
        sb.AppendLine("  store_note: \"Candidate raised adversarial concern: [brief category-level description, e.g. 'political opinion request' or 'bias accusation' — do NOT reproduce the candidate's exact words]\"");
        sb.AppendLine();

        sb.AppendLine("- **sensitive_disclosure**: requires_response = true, consumes_redirect = false, abandon_competency = false");
        sb.AppendLine("  response_text: \"Thank you for sharing that. [Warm, brief acknowledgment — do NOT ask follow-up questions about the disclosure.] Whenever you're ready, [restate the interview question briefly].\"");
        sb.AppendLine("  CRITICAL: store_note MUST be null. NEVER store the content of sensitive disclosures. NEVER reference the disclosure again.");
        sb.AppendLine();

        sb.AppendLine("## Output Format");
        sb.AppendLine("Respond in this exact JSON format (no markdown, no code fences, just raw JSON):");
        sb.AppendLine("{");
        sb.AppendLine("  \"classification\": \"<one of: on_topic, clarification_request, nervous_deflection, process_question, off_topic, refusal_to_elaborate, language_switch_request, adversarial, sensitive_disclosure>\",");
        sb.AppendLine("  \"requires_response\": <true or false>,");
        sb.AppendLine("  \"response_text\": <\"spoken response text\" or null>,");
        sb.AppendLine("  \"consumes_redirect\": <true or false>,");
        sb.AppendLine("  \"abandon_competency\": false,");
        sb.AppendLine("  \"store_note\": <\"brief note\" or null>");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static ResponseClassificationResult ParseClassification(string aiResponse)
    {
        var result = new ResponseClassificationResult();

        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                result.Classification = root.TryGetProperty("classification", out var cls)
                    && cls.ValueKind == JsonValueKind.String
                    ? cls.GetString() ?? "on_topic"
                    : "on_topic";

                result.RequiresResponse = root.TryGetProperty("requires_response", out var rr)
                    && rr.ValueKind == JsonValueKind.True;

                result.ResponseText = root.TryGetProperty("response_text", out var rt)
                    && rt.ValueKind == JsonValueKind.String
                    ? rt.GetString()
                    : null;

                result.ConsumesRedirect = root.TryGetProperty("consumes_redirect", out var cr)
                    && cr.ValueKind == JsonValueKind.True;

                result.AbandonCompetency = root.TryGetProperty("abandon_competency", out var ac)
                    && ac.ValueKind == JsonValueKind.True;

                result.StoreNote = root.TryGetProperty("store_note", out var sn)
                    && sn.ValueKind == JsonValueKind.String
                    ? sn.GetString()
                    : null;

                if (result.Classification == "sensitive_disclosure")
                {
                    result.StoreNote = null;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
        {
            Console.WriteLine($"Failed to parse classification JSON: {ex.Message}. Raw response: {aiResponse}");
            result.Classification = "on_topic";
            result.RequiresResponse = false;
        }

        return result;
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
    }
}
