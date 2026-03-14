using System.ComponentModel.DataAnnotations;
using SurveyApi.Models;

namespace SurveyApi.DTOs.Question;

public class UpdateQuestionDto
{
    [Required, MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int? PageId { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }
    public ShowIfDto? ShowIf { get; set; }
    public ValidationDto? Validation { get; set; }
}
