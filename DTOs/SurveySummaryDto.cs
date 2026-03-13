namespace SurveyApi.DTOs;

/// <summary>
/// Top-level DTO for survey analytics/summary.
/// </summary>
public class SurveySummaryDto
{
    public int SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public int TotalResponses { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<QuestionSummaryDto> Questions { get; set; } = new();
}
