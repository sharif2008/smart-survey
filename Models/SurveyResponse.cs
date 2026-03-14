namespace SurveyApi.Models;

/// <summary>
/// One submission of a survey (one respondent).
/// </summary>
public class SurveyResponse
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string? ParticipantName { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Survey Survey { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
