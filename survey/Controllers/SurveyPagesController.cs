using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.SurveyPage;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api")]
public class SurveyPagesController : ControllerBase
{
    private readonly ISurveyPageService _pageService;
    private readonly ISurveyService _surveyService;

    public SurveyPagesController(ISurveyPageService pageService, ISurveyService surveyService)
    {
        _pageService = pageService;
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

    /// <summary>GET /api/surveys/{surveyId}/pages — Get pages with nested questions (ordered by page Order then question Order). Only active, non-expired surveys for public.</summary>
    [HttpGet("surveys/{surveyId:int}/pages")]
    [ProducesResponseType(typeof(IEnumerable<SurveyPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBySurveyId(int surveyId)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue && !await _surveyService.IsSurveyAvailableToPublicAsync(surveyId).ConfigureAwait(false))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This survey is not available." });
        var list = await _pageService.GetPagesBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>GET /api/surveys/{surveyId}/pages/{pageId} — Get a single page by survey id and page id (both required). Only active surveys for public.</summary>
    [HttpGet("surveys/{surveyId:int}/pages/{pageId:int}")]
    [ProducesResponseType(typeof(SurveyPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySurveyIdAndPageId(int surveyId, int pageId)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue && !await _surveyService.IsSurveyAvailableToPublicAsync(surveyId).ConfigureAwait(false))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This survey is not available." });
        var page = await _pageService.GetBySurveyIdAndPageIdAsync(surveyId, pageId, userId, isAdmin).ConfigureAwait(false);
        if (page == null)
            return NotFound();
        return Ok(page);
    }

    /// <summary>POST /api/surveys/{surveyId}/pages — Create a survey page (Researcher, own survey only).</summary>
    [HttpPost("surveys/{surveyId:int}/pages")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(SurveyPageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(int surveyId, [FromBody] CreateSurveyPageDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        dto.SurveyId = surveyId;
        var page = await _pageService.CreateAsync(dto, userId.Value).ConfigureAwait(false);
        if (page == null)
            return BadRequest("Survey not found or you do not own it.");
        return CreatedAtAction(nameof(GetBySurveyIdAndPageId), new { surveyId, pageId = page.Id }, page);
    }

    /// <summary>PUT /api/surveys/{surveyId}/pages/{pageId} — Update page (both survey id and page id required).</summary>
    [HttpPut("surveys/{surveyId:int}/pages/{pageId:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(SurveyPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBySurveyIdAndPageId(int surveyId, int pageId, [FromBody] UpdateSurveyPageDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var page = await _pageService.UpdateBySurveyIdAndPageIdAsync(surveyId, pageId, dto, userId.Value).ConfigureAwait(false);
        if (page == null)
            return NotFound();
        return Ok(page);
    }

    /// <summary>DELETE /api/surveys/{surveyId}/pages/{pageId} — Delete page (both survey id and page id required).</summary>
    [HttpDelete("surveys/{surveyId:int}/pages/{pageId:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBySurveyIdAndPageId(int surveyId, int pageId)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var ok = await _pageService.DeleteBySurveyIdAndPageIdAsync(surveyId, pageId, userId.Value).ConfigureAwait(false);
        if (!ok)
            return NotFound();
        return NoContent();
    }
}
