using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Survey;

public class CreateSurveyDto
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}
