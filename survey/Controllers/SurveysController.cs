using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Survey;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveysController(ISurveyService surveyService)
    {
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

    /// <summary>GET /api/surveys — List surveys (Admin: all; Researcher: own).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(IEnumerable<SurveyResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var (userId, isAdmin) = GetCurrentUser();
        var list = await _surveyService.GetAllAsync(userId, isAdmin).ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>GET /api/surveys/{id} — Get survey by id (public for participants to view survey details).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SurveyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var survey = await _surveyService.GetByIdAsync(id, userId, isAdmin).ConfigureAwait(false);
        if (survey == null)
            return NotFound();
        return Ok(survey);
    }

    /// <summary>POST /api/surveys — Create survey (Researcher).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(SurveyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateSurveyDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var survey = await _surveyService.CreateAsync(dto, userId.Value).ConfigureAwait(false);
        if (survey == null)
            return BadRequest();
        return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
    }

    /// <summary>PUT /api/surveys/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(SurveyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSurveyDto dto)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var survey = await _surveyService.UpdateAsync(id, dto, userId.Value, isAdmin).ConfigureAwait(false);
        if (survey == null)
            return NotFound();
        return Ok(survey);
    }

    /// <summary>DELETE /api/surveys/{id}</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue)
            return Unauthorized();
        var ok = await _surveyService.DeleteAsync(id, userId.Value, isAdmin).ConfigureAwait(false);
        if (!ok)
            return NotFound();
        return NoContent();
    }
}
