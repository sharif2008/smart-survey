using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Question;
using SurveyApi.Helpers;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;
    private readonly ISurveyPageService _pageService;

    public QuestionService(AppDbContext db, ISurveyPageService pageService)
    {
        _db = db;
        _pageService = pageService;
    }

    public async Task<IEnumerable<QuestionResponseDto>> GetBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == surveyId).ConfigureAwait(false);
        if (survey == null)
            return Array.Empty<QuestionResponseDto>();
        // Public: participants can retrieve questions to display the survey form

        var list = await _db.Questions
            .AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.Page!.Order)
            .ThenBy(q => q.Order)
            .Select(q => new { q.Id, q.SurveyId, q.PageId, q.Text, q.Type, q.IsRequired, q.Order, q.OptionsJson, q.ShowIfJson, q.ValidationJson })
            .ToListAsync()
            .ConfigureAwait(false);
        return list.Select(q => new QuestionResponseDto
        {
            Id = q.Id,
            SurveyId = q.SurveyId,
            PageId = q.PageId,
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Order = q.Order,
            OptionsJson = q.OptionsJson,
            ShowIf = ShowIfHelper.FromJson(q.ShowIfJson),
            Validation = ValidationHelper.FromJson(q.ValidationJson)
        }).ToList();
    }

    public async Task<QuestionResponseDto?> CreateAsync(CreateQuestionDto dto, int researcherId)
    {
        var survey = await _db.Surveys.FindAsync(dto.SurveyId).ConfigureAwait(false);
        if (survey == null || survey.ResearcherId != researcherId)
            return null;

        int pageId;
        if (dto.PageId.HasValue)
        {
            var page = await _db.SurveyPages.Include(p => p.Survey).FirstOrDefaultAsync(p => p.Id == dto.PageId.Value).ConfigureAwait(false);
            if (page == null || page.SurveyId != dto.SurveyId || page.Survey.ResearcherId != researcherId)
                return null;
            pageId = page.Id;
        }
        else
        {
            var firstPage = await _pageService.GetFirstPageOfSurveyAsync(dto.SurveyId).ConfigureAwait(false);
            if (firstPage == null)
                return null;
            pageId = firstPage.Id;
        }

        var q = new Question
        {
            SurveyId = dto.SurveyId,
            PageId = pageId,
            Text = dto.Text,
            Type = dto.Type,
            IsRequired = dto.IsRequired,
            Order = dto.Order,
            OptionsJson = dto.OptionsJson,
            ShowIfJson = ShowIfHelper.ToJson(dto.ShowIf),
            ValidationJson = ValidationHelper.ToJson(dto.Validation)
        };
        _db.Questions.Add(q);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new QuestionResponseDto
        {
            Id = q.Id,
            SurveyId = q.SurveyId,
            PageId = q.PageId,
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Order = q.Order,
            OptionsJson = q.OptionsJson,
            ShowIf = dto.ShowIf ?? ShowIfHelper.FromJson(q.ShowIfJson),
            Validation = dto.Validation ?? ValidationHelper.FromJson(q.ValidationJson)
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
        q.ShowIfJson = ShowIfHelper.ToJson(dto.ShowIf);
        q.ValidationJson = ValidationHelper.ToJson(dto.Validation);
        if (dto.PageId.HasValue)
        {
            var page = await _db.SurveyPages.Include(p => p.Survey).FirstOrDefaultAsync(p => p.Id == dto.PageId.Value).ConfigureAwait(false);
            if (page != null && page.SurveyId == q.SurveyId && page.Survey.ResearcherId == researcherId)
                q.PageId = page.Id;
        }
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new QuestionResponseDto
        {
            Id = q.Id,
            SurveyId = q.SurveyId,
            PageId = q.PageId,
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Order = q.Order,
            OptionsJson = q.OptionsJson,
            ShowIf = ShowIfHelper.FromJson(q.ShowIfJson),
            Validation = ValidationHelper.FromJson(q.ValidationJson)
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
