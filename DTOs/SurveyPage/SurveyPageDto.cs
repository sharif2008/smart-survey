using SurveyApi.DTOs.Question;

namespace SurveyApi.DTOs.SurveyPage;

public class SurveyPageDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public List<QuestionResponseDto> Questions { get; set; } = new();
}
