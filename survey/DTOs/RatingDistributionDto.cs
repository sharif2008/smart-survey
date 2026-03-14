namespace SurveyApi.DTOs;

/// <summary>
/// One bucket in rating distribution (e.g. "1" -> 2, "2" -> 6).
/// </summary>
public class RatingDistributionDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
