namespace SurveyApi.DTOs.Dashboard;

/// <summary>
/// Dashboard statistics. Admin sees platform-wide stats; Researcher sees own summary.
/// </summary>
public class DashboardStatsDto
{
    /// <summary>Total surveys (all for Admin, own for Researcher).</summary>
    public int TotalSurveys { get; set; }

    /// <summary>Number of researchers (Admin only; 0 for Researcher).</summary>
    public int ResearcherCount { get; set; }

    /// <summary>
    /// Responses per hour for the last 24 hours (index 0 = oldest, 23 = most recent hour).
    /// </summary>
    public List<int> HourlyResponses { get; set; } = new();

    /// <summary>Researcher view: list of own surveys with response counts.</summary>
    public List<ResearcherSurveySummaryDto> SurveySummaries { get; set; } = new();
}
