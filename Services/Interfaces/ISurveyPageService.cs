using SurveyApi.DTOs.SurveyPage;
using SurveyApi.Models;

namespace SurveyApi.Services.Interfaces;

public interface ISurveyPageService
{
    Task<IEnumerable<SurveyPageDto>> GetPagesBySurveyIdAsync(int surveyId, int? userId, bool isAdmin);
    Task<SurveyPageDto?> GetByIdAsync(int pageId, int? userId, bool isAdmin);
    Task<SurveyPageDto?> GetBySurveyIdAndPageIdAsync(int surveyId, int pageId, int? userId, bool isAdmin);
    Task<SurveyPageDto?> CreateAsync(CreateSurveyPageDto dto, int researcherId);
    Task<SurveyPageDto?> UpdateAsync(int pageId, UpdateSurveyPageDto dto, int researcherId);
    Task<bool> DeleteAsync(int pageId, int researcherId);
    Task<SurveyPageDto?> UpdateBySurveyIdAndPageIdAsync(int surveyId, int pageId, UpdateSurveyPageDto dto, int researcherId);
    Task<bool> DeleteBySurveyIdAndPageIdAsync(int surveyId, int pageId, int researcherId);
    Task<SurveyPage?> GetFirstPageOfSurveyAsync(int surveyId);
}
