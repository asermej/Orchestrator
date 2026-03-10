namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private QuestionPackageAIManager? _questionPackageAIManager;
    private QuestionPackageAIManager QuestionPackageAIManager =>
        _questionPackageAIManager ??= new QuestionPackageAIManager(_serviceLocator);

    public async Task<List<AISuggestedCompetency>> GenerateCompetencySuggestionsAsync(string roleName, string industry)
    {
        return await QuestionPackageAIManager.GenerateCompetencySuggestions(roleName, industry).ConfigureAwait(false);
    }

    public async Task<string> GenerateCanonicalExampleAsync(string competencyName, string? description, string roleContext)
    {
        return await QuestionPackageAIManager.GenerateCanonicalExample(competencyName, description, roleContext).ConfigureAwait(false);
    }
}
