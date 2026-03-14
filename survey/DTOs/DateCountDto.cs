namespace SurveyApi.DTOs;

/// <summary>
/// Count of responses on a given date (for Date question summary).
/// </summary>
public class DateCountDto
{
    public string Date { get; set; } = string.Empty; // ISO date string
    public int Count { get; set; }
}
