using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Survey;

public class CreateSurveyDto
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Optional end date/time for the survey.</summary>
    public DateTime? EndsAt { get; set; }
    /// <summary>0 = Draft, 1 = Active, -1 = Closed. Defaults to 1 (Active) if not set.</summary>
    public int? Status { get; set; }
}
