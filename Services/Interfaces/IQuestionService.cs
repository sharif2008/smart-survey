using SurveyApi.DTOs.Question;

namespace SurveyApi.Services.Interfaces;

public interface IQuestionService
{
    Task<IEnumerable<QuestionResponseDto>> GetBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin);
    Task<QuestionResponseDto?> CreateAsync(CreateQuestionDto dto, int researcherId);
    Task<QuestionResponseDto?> UpdateAsync(int id, UpdateQuestionDto dto, int researcherId);
    Task<bool> DeleteAsync(int id, int researcherId);
}
