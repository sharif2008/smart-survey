using SurveyApi.Models;

namespace SurveyApi.DTOs;

/// <summary>
/// Summary for a single question. Summary is a generic object (e.g. YesNoSummary, RatingSummary).
/// </summary>
public class QuestionSummaryDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int TotalAnswers { get; set; }
    /// <summary>
    /// Dynamic summary shape depending on QuestionType (e.g. yes/no counts, distribution, samples).
    /// </summary>
    public object Summary { get; set; } = null!;
}
