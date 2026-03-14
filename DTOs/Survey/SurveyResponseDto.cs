namespace SurveyApi.DTOs.Survey;

public class SurveyResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResearcherId { get; set; }
    public DateTime CreatedAt { get; set; }
}
