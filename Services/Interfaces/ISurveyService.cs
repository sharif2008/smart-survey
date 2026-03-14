using SurveyApi.DTOs.Survey;

namespace SurveyApi.Services.Interfaces;

public interface ISurveyService
{
    Task<IEnumerable<SurveyResponseDto>> GetAllAsync(int? researcherId, bool isAdmin);
    Task<SurveyResponseDto?> GetByIdAsync(int id, int? researcherId, bool isAdmin);
    Task<SurveyResponseDto?> CreateAsync(CreateSurveyDto dto, int researcherId);
    Task<SurveyResponseDto?> UpdateAsync(int id, UpdateSurveyDto dto, int researcherId, bool isAdmin);
    Task<bool> DeleteAsync(int id, int researcherId, bool isAdmin);
}
