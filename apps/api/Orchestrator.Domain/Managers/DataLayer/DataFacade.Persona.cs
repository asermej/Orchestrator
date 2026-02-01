namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private PersonaDataManager PersonaDataManager => new(_dbConnectionString);

    public Task<Persona> AddPersona(Persona persona)
    {
        return PersonaDataManager.Add(persona);
    }

    public async Task<Persona?> GetPersonaById(System.Guid id)
    {
        return await PersonaDataManager.GetById(id);
    }
    
    public Task<Persona> UpdatePersona(Persona persona)
    {
        return PersonaDataManager.Update(persona);
    }

    public Task<bool> DeletePersona(System.Guid id)
    {
        return PersonaDataManager.Delete(id);
    }

    public Task<PaginatedResult<Persona>> SearchPersonas(string? firstName, string? lastName, string? displayName, string? createdBy, System.Guid? categoryId, string? sortBy, int pageNumber, int pageSize)
    {
        return PersonaDataManager.Search(firstName, lastName, displayName, createdBy, categoryId, sortBy, pageNumber, pageSize);
    }
}

