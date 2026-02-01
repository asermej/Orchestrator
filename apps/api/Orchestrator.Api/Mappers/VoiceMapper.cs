using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper for voice-related API models.
/// </summary>
public static class VoiceMapper
{
    /// <summary>
    /// Maps a domain voice item to a resource model.
    /// </summary>
    public static VoiceResource? ToResource(ElevenLabsVoiceItem? item)
    {
        if (item == null)
            return null;

        return new VoiceResource
        {
            Id = item.Id,
            Name = item.Name,
            PreviewText = item.PreviewText,
            Category = item.Category,
            VoiceType = item.VoiceType
        };
    }

    /// <summary>
    /// Maps a stock voice to a resource model.
    /// </summary>
    public static VoiceResource ToResource(StockVoice item)
    {
        var tags = string.IsNullOrWhiteSpace(item.Tags)
            ? null
            : item.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return new VoiceResource
        {
            Id = item.VoiceId,
            Name = item.Name,
            Description = item.Description,
            PreviewText = item.PreviewText,
            Tags = tags,
            VoiceType = "prebuilt"
        };
    }

    /// <summary>
    /// Maps a list of stock voices to API response.
    /// </summary>
    public static List<VoiceResource> ToStockVoicesResponse(IReadOnlyList<StockVoice> stockVoices)
    {
        return stockVoices.Select(ToResource).ToList();
    }

    public static AvailableVoicesResponse ToAvailableVoicesResponse(IReadOnlyList<ElevenLabsVoiceItem> prebuiltVoices, IReadOnlyList<ElevenLabsVoiceItem>? userVoices = null)
    {
        return new AvailableVoicesResponse
        {
            CuratedPrebuiltVoices = prebuiltVoices.Select(ToResource).Where(r => r != null).Cast<VoiceResource>().ToList(),
            UserVoices = (userVoices ?? new List<ElevenLabsVoiceItem>()).Select(ToResource).Where(r => r != null).Cast<VoiceResource>().ToList()
        };
    }

    /// <summary>
    /// Maps a domain clone result to a resource model.
    /// </summary>
    public static CloneVoiceResponse? ToResource(VoiceCloneResult? result)
    {
        if (result == null)
            return null;

        return new CloneVoiceResponse
        {
            VoiceId = result.VoiceId,
            VoiceName = result.VoiceName
        };
    }
}
