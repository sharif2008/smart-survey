namespace SurveyApi.DTOs.Question;

/// <summary>
/// Conditional display: show this question only when the answer to another question matches.
/// Client evaluates when rendering (e.g. if answer to questionId equals value, show this question).
/// </summary>
public class ShowIfDto
{
    public int QuestionId { get; set; }
    public string Operator { get; set; } = "equals";
    public string? Value { get; set; }
}
