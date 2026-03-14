namespace SurveyApi.DTOs;

/// <summary>
/// Count and percentage for a single option (SingleChoice, MultipleChoice, YesNo).
/// </summary>
public class OptionCountDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
