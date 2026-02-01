using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Tag domain objects and TagResource API models.
/// </summary>
public static class TagMapper
{
    /// <summary>
    /// Maps a Tag domain object to a TagResource for API responses.
    /// </summary>
    /// <param name="tag">The domain Tag object to map</param>
    /// <returns>A TagResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null</exception>
    public static TagResource ToResource(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new TagResource
        {
            Id = tag.Id,
            Name = tag.Name,
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Tag domain objects to TagResource objects.
    /// </summary>
    /// <param name="tags">The collection of domain Tag objects to map</param>
    /// <returns>A collection of TagResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
    public static IEnumerable<TagResource> ToResource(IEnumerable<Tag> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        return tags.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateTagResource to a Tag domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateTagResource from API request</param>
    /// <returns>A Tag domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Tag ToDomain(CreateTagResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Tag
        {
            Name = createResource.Name
        };
    }
}

