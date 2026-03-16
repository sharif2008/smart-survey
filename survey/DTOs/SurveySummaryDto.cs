namespace SurveyApi.DTOs;

/// <summary>
/// Top-level DTO for survey analytics/summary.
/// </summary>
public class SurveySummaryDto
{
    public int SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public int TotalResponses { get; set; }
    /// <summary>Number of responses considered active for this summary (currently equals TotalResponses).</summary>
    public int ActiveResponses { get; set; }
    /// <summary>Average time in seconds from survey creation until a response is submitted.</summary>
    public double AverageCompletionSeconds { get; set; }
    /// <summary>Duration in whole days between the first and latest response.</summary>
    public int DurationDays { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<QuestionSummaryDto> Questions { get; set; } = new();
}
