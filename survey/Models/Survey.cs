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
    /// <summary>When true, survey is manually closed and no longer accepting responses.</summary>
    public bool IsClosed { get; set; }
    /// <summary>When set, survey is soft-deleted and excluded from all queries.</summary>
    public DateTime? DeletedAt { get; set; }

    public User Researcher { get; set; } = null!;
    public ICollection<SurveyPage> Pages { get; set; } = new List<SurveyPage>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
