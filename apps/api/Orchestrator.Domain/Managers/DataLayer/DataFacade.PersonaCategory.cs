using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private PersonaCategoryDataManager PersonaCategoryDataManager => new(_dbConnectionString);

    public Task<PersonaCategory> AddPersonaCategory(PersonaCategory personaCategory)
    {
        return PersonaCategoryDataManager.Add(personaCategory);
    }

    public async Task<PersonaCategory?> GetPersonaCategoryById(Guid id)
    {
        return await PersonaCategoryDataManager.GetById(id);
    }
    
    public Task<bool> DeletePersonaCategory(Guid id)
    {
        return PersonaCategoryDataManager.Delete(id);
    }

    public Task<bool> DeletePersonaCategoryByPersonaAndCategory(Guid personaId, Guid categoryId)
    {
        return PersonaCategoryDataManager.DeleteByPersonaAndCategory(personaId, categoryId);
    }

    public async Task<IEnumerable<PersonaCategory>> GetPersonaCategoriesByPersonaId(Guid personaId)
    {
        return await PersonaCategoryDataManager.GetByPersonaId(personaId);
    }

    public async Task<IEnumerable<PersonaCategory>> GetPersonaCategoriesByCategoryId(Guid categoryId)
    {
        return await PersonaCategoryDataManager.GetByCategoryId(categoryId);
    }

    public async Task<PersonaCategory?> GetPersonaCategoryByPersonaAndCategory(Guid personaId, Guid categoryId)
    {
        return await PersonaCategoryDataManager.GetByPersonaAndCategory(personaId, categoryId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesByPersonaId(Guid personaId)
    {
        return await PersonaCategoryDataManager.GetCategoriesByPersonaId(personaId);
    }
}

