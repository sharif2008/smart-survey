namespace SurveyApi.DTOs.Dashboard;

/// <summary>
/// Summary of one survey for researcher dashboard (id, title, response count).
/// </summary>
public class ResearcherSurveySummaryDto
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
}
