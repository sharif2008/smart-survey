using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Response;
using SurveyApi.Services;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api")]
public class ResponsesController : ControllerBase
{
    private readonly IResponseService _responseService;

    public ResponsesController(IResponseService responseService)
    {
        _responseService = responseService;
    }

    private (int? userId, bool isAdmin) GetCurrentUser()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var userId = int.TryParse(idClaim, out var id) ? id : (int?)null;
        return (userId, isAdmin);
    }

    /// <summary>POST /api/responses — Submit a survey response (no auth required for participants). Validates against question validation rules.</summary>
    [HttpPost("responses")]
    [ProducesResponseType(typeof(SurveySubmissionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromBody] SubmitSurveyResponseDto dto)
    {
        var submitResult = await _responseService.SubmitAsync(dto).ConfigureAwait(false);
        if (submitResult.ValidationErrors is { Count: > 0 })
            return BadRequest(new { message = "Validation failed.", errors = submitResult.ValidationErrors });
        if (submitResult.Result == null)
            return NotFound("Survey not found.");
        return CreatedAtAction(nameof(GetBySurveyId), new { surveyId = submitResult.Result.SurveyId }, submitResult.Result);
    }

    /// <summary>GET /api/surveys/{surveyId}/responses — List responses (Admin: any; Researcher: own surveys).</summary>
    [HttpGet("surveys/{surveyId:int}/responses")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(IEnumerable<SurveyResponseListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySurveyId(int surveyId)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var list = await _responseService.GetBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
        if (list == null)
            return NotFound();
        return Ok(list);
    }
}
