using SurveyApi.Models;

namespace SurveyApi.DTOs.Response;

/// <summary>
/// Full survey response with all answers for analytics drill-down.
/// </summary>
public class SurveyResponseDetailDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string? ParticipantName { get; set; }
    public DateTime SubmittedAt { get; set; }
    /// <summary>
    /// Total time in seconds from survey creation until this response was submitted.
    /// </summary>
    public double TotalTimeSeconds { get; set; }
    public List<SurveyResponseAnswerDto> Answers { get; set; } = new();
}

public class SurveyResponseAnswerDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? ResponseText { get; set; }
}

