using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs;
using SurveyApi.Services;
using SurveyApi.Services.Interfaces;

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
    private readonly IExportService _exportService;

    public AnalyticsController(IResponseService responseService, IExportService exportService)
    {
        _responseService = responseService;
        _exportService = exportService;
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

    /// <summary>
    /// GET /api/surveys/{surveyId}/responses/export/excel
    /// Returns an Excel (.xlsx) file with one row per response and one column per question.
    /// </summary>
    [HttpGet("{surveyId:int}/responses/export/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportResponsesToExcel(int surveyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            return Forbid();

        var isAdmin = string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase);

        var bytes = await _exportService.ExportSurveyResponsesToExcelAsync(surveyId, currentUserId, isAdmin)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (bytes == null || bytes.Length == 0)
            return NotFound();

        var fileName = $"survey-{surveyId}-responses.xlsx";
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, fileName);
    }
}
