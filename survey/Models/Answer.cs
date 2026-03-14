namespace SurveyApi.Models;

/// <summary>
/// One answer to one question within a survey response.
/// ResponseText: used for all types (number/rating/date stored as string, parse when summarizing).
/// MultipleChoice: comma-separated selected values, e.g. "A,B,C".
/// </summary>
public class Answer
{
    public int Id { get; set; }
    public int SurveyResponseId { get; set; }
    public int QuestionId { get; set; }
    public string? ResponseText { get; set; }

    public SurveyResponse SurveyResponse { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
