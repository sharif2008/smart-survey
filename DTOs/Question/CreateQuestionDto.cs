using System.ComponentModel.DataAnnotations;
using SurveyApi.Models;

namespace SurveyApi.DTOs.Question;

public class CreateQuestionDto
{
    [Required]
    public int SurveyId { get; set; }

    [Required, MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }
}
