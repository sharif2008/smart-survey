using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Survey;

public class UpdateSurveyDto
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? EndsAt { get; set; }
    /// <summary>0 = Draft, 1 = Active, -1 = Closed.</summary>
    public int? Status { get; set; }
}
