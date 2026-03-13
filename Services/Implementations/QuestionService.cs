using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Question;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;

    public QuestionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<QuestionResponseDto>> GetBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == surveyId).ConfigureAwait(false);
        if (survey == null)
            return Array.Empty<QuestionResponseDto>();
        // Public: participants can retrieve questions to display the survey form

        return await _db.Questions
            .AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.Order)
            .Select(q => new QuestionResponseDto
            {
                Id = q.Id,
                SurveyId = q.SurveyId,
                Text = q.Text,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Order = q.Order,
                OptionsJson = q.OptionsJson
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<QuestionResponseDto?> CreateAsync(CreateQuestionDto dto, int researcherId)
    {
        var survey = await _db.Surveys.FindAsync(dto.SurveyId).ConfigureAwait(false);
        if (survey == null || survey.ResearcherId != researcherId)
            return null;

        var q = new Question
        {
            SurveyId = dto.SurveyId,
            Text = dto.Text,
            Type = dto.Type,
            IsRequired = dto.IsRequired,
            Order = dto.Order,
            OptionsJson = dto.OptionsJson
        };
        _db.Questions.Add(q);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new QuestionResponseDto
        {
            Id = q.Id,
            SurveyId = q.SurveyId,
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Order = q.Order,
            OptionsJson = q.OptionsJson
        };
    }

    public async Task<QuestionResponseDto?> UpdateAsync(int id, UpdateQuestionDto dto, int researcherId)
    {
        var q = await _db.Questions.Include(q => q.Survey).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        if (q == null || q.Survey.ResearcherId != researcherId)
            return null;

        q.Text = dto.Text;
        q.Type = dto.Type;
        q.IsRequired = dto.IsRequired;
        q.Order = dto.Order;
        q.OptionsJson = dto.OptionsJson;
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new QuestionResponseDto
        {
            Id = q.Id,
            SurveyId = q.SurveyId,
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Order = q.Order,
            OptionsJson = q.OptionsJson
        };
    }

    public async Task<bool> DeleteAsync(int id, int researcherId)
    {
        var q = await _db.Questions.Include(q => q.Survey).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        if (q == null || q.Survey.ResearcherId != researcherId)
            return false;
        _db.Questions.Remove(q);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
