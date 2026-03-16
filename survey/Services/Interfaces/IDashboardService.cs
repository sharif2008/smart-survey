using SurveyApi.DTOs.Dashboard;

namespace SurveyApi.Services.Interfaces;

/// <summary>
/// Dashboard statistics: Admin sees all; Researcher sees own summary.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(int? userId, bool isAdmin);
}
