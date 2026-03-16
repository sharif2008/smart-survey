namespace SurveyApi.Models;

/// <summary>
/// Supported question types for dynamic survey summary generation.
/// </summary>
public enum QuestionType
{
    Text = 0,
    TextArea = 1,
    Number = 2,
    YesNo = 3,
    SingleChoice = 4,
    MultipleChoice = 5,
    Rating = 6,
    Date = 7,
    Like = 8,
    Ranking = 9,
    NetPromoterScore = 10
}
