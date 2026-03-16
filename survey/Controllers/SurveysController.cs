using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Survey;
using SurveyApi.DTOs.Response;
using System.Text;
using System.Text.Json;
using SurveyApi.Services;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly IResponseService _responseService;
    private readonly IExportService _exportService;

    public SurveysController(ISurveyService surveyService, IResponseService responseService, IExportService exportService)
    {
        _surveyService = surveyService;
        _responseService = responseService;
        _exportService = exportService;
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

    /// <summary>
    /// GET /api/surveys/{surveyId}/responses/export?format=csv|json
    /// Export all responses for a survey as CSV or JSON. Admins can export any survey; Researchers only their own.
    /// </summary>
    [HttpGet("{surveyId:int}/responses/export")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportResponses(int surveyId, [FromQuery] string? format = "csv")
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!userId.HasValue && !isAdmin)
            return Unauthorized();

        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();

        // JSON export: reuse detailed DTOs for structure
        if (normalizedFormat == "json")
        {
            var details = await _responseService.GetDetailedBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
            if (details == null)
                return NotFound();

            var json = JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileNameJson = $"survey-{surveyId}-responses.json";
            return File(bytes, "application/json", fileNameJson);
        }

        // CSV export: use detailed DTOs to build a wide table (one row per response, one column per question)
        {
            var details = await _responseService.GetDetailedBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
            if (details == null)
                return NotFound();

            // Collect all distinct questions across responses
            var questionMap = new Dictionary<int, string>();
            foreach (var r in details)
            {
                foreach (var a in r.Answers)
                {
                    if (!questionMap.ContainsKey(a.QuestionId))
                        questionMap[a.QuestionId] = a.QuestionText;
                }
            }

            var orderedQuestions = questionMap
                .OrderBy(q => q.Key)
                .ToList();

            var sb = new StringBuilder();
            // Header
            sb.Append("SurveyId,ResponseId,ParticipantName,SubmittedAtUtc,TotalTimeSeconds");
            foreach (var q in orderedQuestions)
            {
                sb.Append(',');
                sb.Append('"');
                sb.Append($"Q{q.Key}: {q.Value.Replace("\"", "\"\"")}");
                sb.Append('"');
            }
            sb.AppendLine();

            // Rows
            foreach (var r in details)
            {
                sb.Append(r.SurveyId);
                sb.Append(',');
                sb.Append(r.Id);
                sb.Append(',');
                sb.Append('"');
                sb.Append((r.ParticipantName ?? string.Empty).Replace("\"", "\"\""));
                sb.Append('"');
                sb.Append(',');
                sb.Append(r.SubmittedAt.ToUniversalTime().ToString("O"));
                sb.Append(',');
                sb.Append(r.TotalTimeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));

                var answerByQuestion = r.Answers.ToDictionary(a => a.QuestionId, a => a.ResponseText ?? string.Empty);
                foreach (var q in orderedQuestions)
                {
                    sb.Append(',');
                    var value = answerByQuestion.TryGetValue(q.Key, out var v) ? v : string.Empty;
                    sb.Append('"');
                    sb.Append(value.Replace("\"", "\"\""));
                    sb.Append('"');
                }

                sb.AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileNameCsv = $"survey-{surveyId}-responses.csv";
            return File(bytes, "text/csv", fileNameCsv);
        }
    }

    /// <summary>GET /api/surveys/{surveyId}/responses/details — Full response details with answers (Admin/Researcher, own surveys for Researcher).</summary>
    [HttpGet("{surveyId:int}/responses/details")]
    [Authorize(Roles = "Admin,Researcher")]
    [ProducesResponseType(typeof(IEnumerable<SurveyResponseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResponseDetails(int surveyId)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var list = await _responseService.GetDetailedBySurveyIdAsync(surveyId, userId, isAdmin).ConfigureAwait(false);
        if (list == null)
            return NotFound();
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
