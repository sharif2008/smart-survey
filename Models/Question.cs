namespace SurveyApi.Models;

/// <summary>
/// A single question in a survey. Type drives how answers are summarized.
/// Options (for SingleChoice/MultipleChoice) stored in OptionsJson.
/// </summary>
public class Question
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? OptionsJson { get; set; }

    public Survey Survey { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
