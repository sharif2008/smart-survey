namespace SurveyApi.DTOs.Response;

public class SubmitResultDto
{
    public SurveySubmissionResponseDto? Result { get; set; }
    public IReadOnlyList<string>? ValidationErrors { get; set; }
}
