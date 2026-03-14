namespace SurveyApi.DTOs.Response;

public class SurveyResponseListItemDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string? ParticipantName { get; set; }
    public DateTime SubmittedAt { get; set; }
}
