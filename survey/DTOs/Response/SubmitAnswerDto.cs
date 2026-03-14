using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Response;

public class SubmitAnswerDto
{
    [Required]
    public int QuestionId { get; set; }

    [MaxLength(4000)]
    public string? ResponseText { get; set; }
}
