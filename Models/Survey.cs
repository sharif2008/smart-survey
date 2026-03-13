namespace SurveyApi.Models;

/// <summary>
/// Survey owned by a researcher; contains questions and collects responses.
/// </summary>
public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResearcherId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Researcher { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
