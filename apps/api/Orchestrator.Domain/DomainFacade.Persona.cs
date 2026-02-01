using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Persona> CreatePersona(Persona persona)
    {
        return await PersonaManager.CreatePersona(persona);
    }

    public async Task<Persona?> GetPersonaById(Guid id)
    {
        return await PersonaManager.GetPersonaById(id);
    }

    public async Task<PaginatedResult<Persona>> SearchPersonas(string? firstName, string? lastName, string? displayName, string? createdBy, Guid? categoryId, string? sortBy, int pageNumber, int pageSize)
    {
        return await PersonaManager.SearchPersonas(firstName, lastName, displayName, createdBy, categoryId, sortBy, pageNumber, pageSize);
    }

    public async Task<Persona> UpdatePersona(Persona persona)
    {
        return await PersonaManager.UpdatePersona(persona);
    }

    public async Task<bool> DeletePersona(Guid id)
    {
        return await PersonaManager.DeletePersona(id);
    }

    public async Task<Persona> UpdatePersonaTraining(Guid personaId, string trainingContent)
    {
        return await PersonaManager.UpdatePersonaTraining(personaId, trainingContent);
    }

    public async Task<string> GetPersonaTraining(Guid personaId)
    {
        return await PersonaManager.GetPersonaTraining(personaId);
    }

    public async Task<PersonaCategory> AddCategoryToPersona(Guid personaId, Guid categoryId)
    {
        return await PersonaManager.AddCategoryToPersona(personaId, categoryId);
    }

    public async Task<bool> RemoveCategoryFromPersona(Guid personaId, Guid categoryId)
    {
        return await PersonaManager.RemoveCategoryFromPersona(personaId, categoryId);
    }

    public async Task<IEnumerable<Category>> GetPersonaCategories(Guid personaId)
    {
        return await PersonaManager.GetPersonaCategories(personaId);
    }
}

