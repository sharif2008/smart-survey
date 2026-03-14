using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.SurveyPage;

public class UpdateSurveyPageDto
{
    [MaxLength(500)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int Order { get; set; }
}
