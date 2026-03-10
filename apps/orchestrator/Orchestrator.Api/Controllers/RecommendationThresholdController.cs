using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Domain;

namespace Orchestrator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RecommendationThresholdController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public RecommendationThresholdController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RecommendationThresholdResource), 200)]
    public async Task<ActionResult<RecommendationThresholdResource>> GetThresholds()
    {
        var thresholds = await _domainFacade.GetRecommendationThresholds();
        return Ok(new RecommendationThresholdResource
        {
            Id = thresholds.Id,
            StronglyRecommendMin = thresholds.StronglyRecommendMin,
            RecommendMin = thresholds.RecommendMin,
            ConsiderMin = thresholds.ConsiderMin,
            DoNotRecommendMin = thresholds.DoNotRecommendMin
        });
    }

    [HttpPut]
    [ProducesResponseType(typeof(RecommendationThresholdResource), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<RecommendationThresholdResource>> UpdateThresholds(
        [FromBody] UpdateRecommendationThresholdRequest request)
    {
        if (request.StronglyRecommendMin <= request.RecommendMin)
            return BadRequest("Strongly Recommend threshold must be greater than Recommend threshold.");
        if (request.RecommendMin <= request.ConsiderMin)
            return BadRequest("Recommend threshold must be greater than Consider threshold.");

        var current = await _domainFacade.GetRecommendationThresholds();
        current.StronglyRecommendMin = request.StronglyRecommendMin;
        current.RecommendMin = request.RecommendMin;
        current.ConsiderMin = request.ConsiderMin;
        current.DoNotRecommendMin = request.DoNotRecommendMin;

        var updated = await _domainFacade.UpdateRecommendationThresholds(current);
        return Ok(new RecommendationThresholdResource
        {
            Id = updated.Id,
            StronglyRecommendMin = updated.StronglyRecommendMin,
            RecommendMin = updated.RecommendMin,
            ConsiderMin = updated.ConsiderMin,
            DoNotRecommendMin = updated.DoNotRecommendMin
        });
    }
}
