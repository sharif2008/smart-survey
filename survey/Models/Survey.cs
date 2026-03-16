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
    /// <summary>Optional end date/time; after this the survey is no longer accepting responses.</summary>
    public DateTime? EndsAt { get; set; }
    /// <summary>0 = Draft, 1 = Active, -1 = Closed. Only Active (1) surveys accept responses (subject to EndsAt).</summary>
    public int Status { get; set; } = 1;
    /// <summary>When set, survey is soft-deleted and excluded from all queries.</summary>
    public DateTime? DeletedAt { get; set; }

    public User Researcher { get; set; } = null!;
    public ICollection<SurveyPage> Pages { get; set; } = new List<SurveyPage>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
