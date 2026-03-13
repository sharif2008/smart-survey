using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs;
using SurveyApi.Services;

namespace SurveyApi.Controllers;

/// <summary>
/// Analytics and summary endpoints for surveys. Admin and Researcher only.
/// </summary>
[ApiController]
[Route("api/surveys")]
[Authorize(Roles = "Admin,Researcher")]
public class AnalyticsController : ControllerBase
{
    private readonly IResponseService _responseService;

    public AnalyticsController(IResponseService responseService)
    {
        _responseService = responseService;
    }

    /// <summary>
    /// GET /api/surveys/{surveyId}/summary
    /// Returns dynamic summary and analytics for the survey. Researchers can only view their own surveys.
    /// </summary>
    [HttpGet("{surveyId:int}/summary")]
    [ProducesResponseType(typeof(SurveySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSurveySummary(int surveyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            return Forbid();

        var currentUserRole = roleClaim ?? "Researcher";

        try
        {
            var summary = await _responseService.GetSurveySummaryAsync(surveyId, currentUserId, currentUserRole)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (summary == null)
                return NotFound();

            return Ok(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
