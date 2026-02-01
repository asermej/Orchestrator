using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Persona domain objects and PersonaResource API models.
/// </summary>
public static class PersonaMapper
{
    /// <summary>
    /// Maps a Persona domain object to a PersonaResource for API responses.
    /// </summary>
    /// <param name="persona">The domain Persona object to map</param>
    /// <returns>A PersonaResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when persona is null</exception>
    public static PersonaResource ToResource(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);

        return new PersonaResource
        {
            Id = persona.Id,
            FirstName = persona.FirstName,
            LastName = persona.LastName,
            DisplayName = persona.DisplayName,
            ProfileImageUrl = persona.ProfileImageUrl,
            ElevenLabsVoiceId = persona.ElevenLabsVoiceId,
            VoiceStability = persona.VoiceStability,
            VoiceSimilarityBoost = persona.VoiceSimilarityBoost,
            VoiceProvider = persona.VoiceProvider,
            VoiceType = persona.VoiceType,
            VoiceName = persona.VoiceName,
            VoiceCreatedAt = persona.VoiceCreatedAt,
            VoiceCreatedByUserId = persona.VoiceCreatedByUserId,
            CreatedAt = persona.CreatedAt,
            UpdatedAt = persona.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Persona domain objects to PersonaResource objects.
    /// </summary>
    /// <param name="personas">The collection of domain Persona objects to map</param>
    /// <returns>A collection of PersonaResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when personas is null</exception>
    public static IEnumerable<PersonaResource> ToResource(IEnumerable<Persona> personas)
    {
        ArgumentNullException.ThrowIfNull(personas);

        return personas.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreatePersonaResource to a Persona domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreatePersonaResource from API request</param>
    /// <returns>A Persona domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Persona ToDomain(CreatePersonaResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Persona
        {
            FirstName = createResource.FirstName,
            LastName = createResource.LastName,
            DisplayName = createResource.DisplayName,
            ProfileImageUrl = createResource.ProfileImageUrl
        };
    }

    /// <summary>
    /// Maps an UpdatePersonaResource to a Persona domain object for updates.
    /// </summary>
    /// <param name="updateResource">The UpdatePersonaResource from API request</param>
    /// <param name="existingPersona">The existing Persona domain object to update</param>
    /// <returns>A Persona domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingPersona is null</exception>
    public static Persona ToDomain(UpdatePersonaResource updateResource, Persona existingPersona)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingPersona);

        return new Persona
        {
            Id = existingPersona.Id,
            FirstName = updateResource.FirstName ?? existingPersona.FirstName,
            LastName = updateResource.LastName ?? existingPersona.LastName,
            DisplayName = updateResource.DisplayName ?? existingPersona.DisplayName,
            ProfileImageUrl = updateResource.ProfileImageUrl ?? existingPersona.ProfileImageUrl,
            ElevenLabsVoiceId = updateResource.ElevenLabsVoiceId ?? existingPersona.ElevenLabsVoiceId,
            VoiceStability = updateResource.VoiceStability ?? existingPersona.VoiceStability,
            VoiceSimilarityBoost = updateResource.VoiceSimilarityBoost ?? existingPersona.VoiceSimilarityBoost,
            VoiceProvider = updateResource.VoiceProvider ?? existingPersona.VoiceProvider,
            VoiceType = updateResource.VoiceType ?? existingPersona.VoiceType,
            VoiceName = updateResource.VoiceName ?? existingPersona.VoiceName,
            VoiceCreatedAt = existingPersona.VoiceCreatedAt,
            VoiceCreatedByUserId = existingPersona.VoiceCreatedByUserId,
            CreatedAt = existingPersona.CreatedAt,
            UpdatedAt = existingPersona.UpdatedAt,
            CreatedBy = existingPersona.CreatedBy,
            UpdatedBy = existingPersona.UpdatedBy,
            IsDeleted = existingPersona.IsDeleted,
            DeletedAt = existingPersona.DeletedAt,
            DeletedBy = existingPersona.DeletedBy
        };
    }
}

