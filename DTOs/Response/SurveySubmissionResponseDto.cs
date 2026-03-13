namespace SurveyApi.DTOs.Response;

public class SurveySubmissionResponseDto
{
    public int SurveyResponseId { get; set; }
    public int SurveyId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
