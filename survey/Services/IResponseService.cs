using SurveyApi.DTOs;
using SurveyApi.DTOs.Response;

namespace SurveyApi.Services;

/// <summary>
/// Service for survey responses and analytics.
/// </summary>
public interface IResponseService
{
    Task<SurveySummaryDto?> GetSurveySummaryAsync(int surveyId, int currentUserId, string currentUserRole);
    Task<SubmitResultDto> SubmitAsync(SubmitSurveyResponseDto dto);
    Task<IEnumerable<SurveyResponseListItemDto>?> GetBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin);
    Task<IEnumerable<SurveyResponseDetailDto>?> GetDetailedBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin);
}

