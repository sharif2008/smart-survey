using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Dashboard;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(int? userId, bool isAdmin)
    {
        if (isAdmin)
            return await GetAdminStatsAsync().ConfigureAwait(false);
        if (userId.HasValue)
            return await GetResearcherStatsAsync(userId.Value).ConfigureAwait(false);

        return new DashboardStatsDto();
    }

    private async Task<DashboardStatsDto> GetAdminStatsAsync()
    {
        var totalSurveys = await _db.Surveys.CountAsync().ConfigureAwait(false);
        var researcherRoleId = await _db.Roles
            .AsNoTracking()
            .Where(r => r.Name == "Researcher")
            .Select(r => r.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        var researcherCount = researcherRoleId == 0
            ? 0
            : await _db.Users
                .AsNoTracking()
                .Where(u => u.RoleId == researcherRoleId)
                .CountAsync()
                .ConfigureAwait(false);

        var hourlyResponses = await GetHourlyResponsesAsync().ConfigureAwait(false);

        return new DashboardStatsDto
        {
            TotalSurveys = totalSurveys,
            ResearcherCount = researcherCount,
            HourlyResponses = hourlyResponses,
            SurveySummaries = new List<ResearcherSurveySummaryDto>()
        };
    }

    private async Task<DashboardStatsDto> GetResearcherStatsAsync(int researcherId)
    {
        var surveyIds = await _db.Surveys
            .AsNoTracking()
            .Where(s => s.ResearcherId == researcherId)
            .Select(s => s.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var totalSurveys = surveyIds.Count;
        var hourlyResponses = await GetHourlyResponsesAsync(surveyIds).ConfigureAwait(false);

        var summaries = await _db.Surveys
            .AsNoTracking()
            .Where(s => s.ResearcherId == researcherId)
            .OrderBy(s => s.Id)
            .Select(s => new ResearcherSurveySummaryDto
            {
                SurveyId = s.Id,
                Title = s.Title,
                ResponseCount = s.SurveyResponses.Count
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return new DashboardStatsDto
        {
            TotalSurveys = totalSurveys,
            ResearcherCount = 0,
            HourlyResponses = hourlyResponses,
            SurveySummaries = summaries
        };
    }

    private async Task<List<int>> GetHourlyResponsesAsync(IReadOnlyCollection<int>? surveyIds = null)
    {
        var now = DateTime.UtcNow;
        var from = now.AddHours(-23);

        var query = _db.SurveyResponses.AsNoTracking().Where(r => r.SubmittedAt >= from && r.SubmittedAt <= now);
        if (surveyIds is { Count: > 0 })
            query = query.Where(r => surveyIds.Contains(r.SurveyId));

        var items = await query
            .Select(r => r.SubmittedAt)
            .ToListAsync()
            .ConfigureAwait(false);

        var buckets = Enumerable.Repeat(0, 24).ToArray();
        foreach (var ts in items)
        {
            var diffHours = (int)Math.Floor((ts.ToUniversalTime() - from).TotalHours);
            if (diffHours < 0 || diffHours > 23) continue;
            buckets[diffHours]++;
        }

        return buckets.ToList();
    }
}
