using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Question;
using SurveyApi.DTOs.SurveyPage;
using SurveyApi.Helpers;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class SurveyPageService : ISurveyPageService
{
    private readonly AppDbContext _db;

    public SurveyPageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SurveyPageDto>> GetPagesBySurveyIdAsync(int surveyId, int? userId, bool isAdmin)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == surveyId).ConfigureAwait(false);
        if (survey == null)
            return Array.Empty<SurveyPageDto>();

        var pages = await _db.SurveyPages
            .AsNoTracking()
            .Include(p => p.Questions.OrderBy(q => q.Order))
            .Where(p => p.SurveyId == surveyId)
            .OrderBy(p => p.Order)
            .ToListAsync()
            .ConfigureAwait(false);

        return pages.Select(p => new SurveyPageDto
        {
            Id = p.Id,
            SurveyId = p.SurveyId,
            Title = p.Title,
            Description = p.Description,
            Order = p.Order,
            Questions = p.Questions.Select(q => new QuestionResponseDto
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
            }).ToList()
        }).ToList();
    }

    public async Task<SurveyPageDto?> GetByIdAsync(int pageId, int? userId, bool isAdmin)
    {
        var page = await _db.SurveyPages
            .AsNoTracking()
            .Include(p => p.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(p => p.Id == pageId).ConfigureAwait(false);
        if (page == null)
            return null;
        if (!isAdmin && userId.HasValue)
        {
            var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == page.SurveyId).ConfigureAwait(false);
            if (survey?.ResearcherId != userId.Value)
                return null;
        }

        return new SurveyPageDto
        {
            Id = page.Id,
            SurveyId = page.SurveyId,
            Title = page.Title,
            Description = page.Description,
            Order = page.Order,
            Questions = page.Questions.Select(q => new QuestionResponseDto
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
            }).ToList()
        };
    }

    public async Task<SurveyPageDto?> GetBySurveyIdAndPageIdAsync(int surveyId, int pageId, int? userId, bool isAdmin)
    {
        var page = await _db.SurveyPages
            .AsNoTracking()
            .Include(p => p.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(p => p.Id == pageId && p.SurveyId == surveyId).ConfigureAwait(false);
        if (page == null)
            return null;
        if (!isAdmin && userId.HasValue)
        {
            var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == page.SurveyId).ConfigureAwait(false);
            if (survey?.ResearcherId != userId.Value)
                return null;
        }

        return new SurveyPageDto
        {
            Id = page.Id,
            SurveyId = page.SurveyId,
            Title = page.Title,
            Description = page.Description,
            Order = page.Order,
            Questions = page.Questions.Select(q => new QuestionResponseDto
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
            }).ToList()
        };
    }

    public async Task<SurveyPageDto?> CreateAsync(CreateSurveyPageDto dto, int researcherId)
    {
        var survey = await _db.Surveys.FindAsync(dto.SurveyId).ConfigureAwait(false);
        if (survey == null || survey.ResearcherId != researcherId)
            return null;

        var page = new SurveyPage
        {
            SurveyId = dto.SurveyId,
            Title = dto.Title,
            Description = dto.Description,
            Order = dto.Order
        };
        _db.SurveyPages.Add(page);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new SurveyPageDto
        {
            Id = page.Id,
            SurveyId = page.SurveyId,
            Title = page.Title,
            Description = page.Description,
            Order = page.Order,
            Questions = new List<QuestionResponseDto>()
        };
    }

    public async Task<SurveyPageDto?> UpdateAsync(int pageId, UpdateSurveyPageDto dto, int researcherId)
    {
        var page = await _db.SurveyPages.Include(p => p.Survey).Include(p => p.Questions).FirstOrDefaultAsync(p => p.Id == pageId).ConfigureAwait(false);
        if (page == null || page.Survey.ResearcherId != researcherId)
            return null;

        page.Title = dto.Title;
        page.Description = dto.Description;
        page.Order = dto.Order;
        await _db.SaveChangesAsync().ConfigureAwait(false);

        return new SurveyPageDto
        {
            Id = page.Id,
            SurveyId = page.SurveyId,
            Title = page.Title,
            Description = page.Description,
            Order = page.Order,
            Questions = page.Questions.OrderBy(q => q.Order).Select(q => new QuestionResponseDto
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
            }).ToList()
        };
    }

    public async Task<bool> DeleteAsync(int pageId, int researcherId)
    {
        var page = await _db.SurveyPages.Include(p => p.Survey).FirstOrDefaultAsync(p => p.Id == pageId).ConfigureAwait(false);
        if (page == null || page.Survey.ResearcherId != researcherId)
            return false;
        _db.SurveyPages.Remove(page);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<SurveyPageDto?> UpdateBySurveyIdAndPageIdAsync(int surveyId, int pageId, UpdateSurveyPageDto dto, int researcherId)
    {
        var page = await _db.SurveyPages.Include(p => p.Survey).Include(p => p.Questions).FirstOrDefaultAsync(p => p.Id == pageId && p.SurveyId == surveyId).ConfigureAwait(false);
        if (page == null || page.Survey.ResearcherId != researcherId)
            return null;
        page.Title = dto.Title;
        page.Description = dto.Description;
        page.Order = dto.Order;
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return new SurveyPageDto
        {
            Id = page.Id,
            SurveyId = page.SurveyId,
            Title = page.Title,
            Description = page.Description,
            Order = page.Order,
            Questions = page.Questions.OrderBy(q => q.Order).Select(q => new QuestionResponseDto
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
            }).ToList()
        };
    }

    public async Task<bool> DeleteBySurveyIdAndPageIdAsync(int surveyId, int pageId, int researcherId)
    {
        var page = await _db.SurveyPages.Include(p => p.Survey).FirstOrDefaultAsync(p => p.Id == pageId && p.SurveyId == surveyId).ConfigureAwait(false);
        if (page == null || page.Survey.ResearcherId != researcherId)
            return false;
        _db.SurveyPages.Remove(page);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<SurveyPage?> GetFirstPageOfSurveyAsync(int surveyId)
    {
        var page = await _db.SurveyPages
            .Where(p => p.SurveyId == surveyId)
            .OrderBy(p => p.Order)
            .FirstOrDefaultAsync().ConfigureAwait(false);
        if (page != null)
            return page;

        var survey = await _db.Surveys.FindAsync(surveyId).ConfigureAwait(false);
        if (survey == null)
            return null;

        var newPage = new SurveyPage { SurveyId = surveyId, Order = 1 };
        _db.SurveyPages.Add(newPage);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return newPage;
    }
}
