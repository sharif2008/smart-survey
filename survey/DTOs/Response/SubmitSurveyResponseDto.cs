using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Response;

public class SubmitSurveyResponseDto
{
    [Required]
    public int SurveyId { get; set; }

    [MaxLength(200)]
    public string? ParticipantName { get; set; }

    [Required]
    public List<SubmitAnswerDto> Answers { get; set; } = new();
}
