using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Question;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ISurveyService _surveyService;

    public QuestionsController(IQuestionService questionService, ISurveyService surveyService)
    {
        _questionService = questionService;
        _surveyService = surveyService;
    }

    private (int? userId, bool isAdmin) GetCurrentUser()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var userId = int.TryParse(idClaim, out var id) ? id : (int?)null;
        return (userId, isAdmin);
    }

    /// <summary>GET /api/surveys/{surveyId}/questions — Only active, non-expired surveys for public.</summary>
    [HttpGet("surveys/{surveyId:int}/questions")]
    [ProducesResponseType(typeof(IEnumerable<QuestionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBySurveyId(int surveyId)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue && !await _surveyService.IsSurveyAvailableToPublicAsync(surveyId).ConfigureAwait(false))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This survey is not available." });
        var list = await _questionService.GetBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>POST /api/questions — Create question (Researcher, own survey only).</summary>
    [HttpPost("questions")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateQuestionDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var q = await _questionService.CreateAsync(dto, userId.Value).ConfigureAwait(false);
        if (q == null)
            return BadRequest("Survey not found or you do not own it.");
        return CreatedAtAction(nameof(GetBySurveyId), new { surveyId = q.SurveyId }, q);
    }

    /// <summary>PUT /api/questions/{id}</summary>
    [HttpPut("questions/{id:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQuestionDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var q = await _questionService.UpdateAsync(id, dto, userId.Value).ConfigureAwait(false);
        if (q == null)
            return NotFound();
        return Ok(q);
    }

    /// <summary>DELETE /api/questions/{id}</summary>
    [HttpDelete("questions/{id:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var ok = await _questionService.DeleteAsync(id, userId.Value).ConfigureAwait(false);
        if (!ok)
            return NotFound();
        return NoContent();
    }
}
