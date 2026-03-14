namespace SurveyApi.Models;

/// <summary>
/// A page/step in a survey; contains multiple ordered questions.
/// </summary>
public class SurveyPage
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }

    public Survey Survey { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
