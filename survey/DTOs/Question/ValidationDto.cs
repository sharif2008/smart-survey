namespace SurveyApi.DTOs.Question;

/// <summary>
/// Validation rules for a question. Only set properties apply; null means no rule.
/// Type-specific: text/textarea use length/regex; number/rating use min/max number; date uses date range; choice uses optionMustExist/maxSelectionCount.
/// </summary>
public class ValidationDto
{
    public bool? Required { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Regex { get; set; }
    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }
    public string? DateMin { get; set; }
    public string? DateMax { get; set; }
    public bool? OptionMustExist { get; set; }
    public int? MaxSelectionCount { get; set; }
}
