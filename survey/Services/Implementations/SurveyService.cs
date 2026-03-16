using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Survey;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class SurveyService : ISurveyService
{
    private readonly AppDbContext _db;

    public SurveyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SurveyResponseDto>> GetAllAsync(int? researcherId, bool isAdmin)
    {
        var query = _db.Surveys.AsNoTracking();
        if (!isAdmin && researcherId.HasValue)
            query = query.Where(s => s.ResearcherId == researcherId.Value);

        return await query
            .OrderBy(s => s.Id)
            .Select(s => new SurveyResponseDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                ResearcherId = s.ResearcherId,
                CreatedAt = s.CreatedAt,
                EndsAt = s.EndsAt,
                Status = s.Status
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<SurveyResponseDto?> GetByIdAsync(int id, int? researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
        if (survey == null)
            return null;
        // Public: participants can retrieve survey details without auth

        return new SurveyResponseDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            ResearcherId = survey.ResearcherId,
            CreatedAt = survey.CreatedAt,
            EndsAt = survey.EndsAt,
            Status = survey.Status
        };
    }

    public async Task<bool> IsSurveyAvailableToPublicAsync(int surveyId)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == surveyId).ConfigureAwait(false);
        if (survey == null)
            return false;
        if (survey.Status != 1)
            return false;
        if (survey.EndsAt.HasValue && DateTime.UtcNow >= survey.EndsAt.Value)
            return false;
        return true;
    }

    public async Task<SurveyResponseDto?> CreateAsync(CreateSurveyDto dto, int researcherId)
    {
        var survey = new Survey
        {
            Title = dto.Title,
            Description = dto.Description,
            ResearcherId = researcherId,
            EndsAt = dto.EndsAt,
            Status = dto.Status ?? 1
        };
        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return await GetByIdAsync(survey.Id, researcherId, false).ConfigureAwait(false);
    }

    public async Task<SurveyResponseDto?> UpdateAsync(int id, UpdateSurveyDto dto, int researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.FindAsync(id).ConfigureAwait(false);
        if (survey == null)
            return null;
        if (!isAdmin && survey.ResearcherId != researcherId)
            return null;

        survey.Title = dto.Title;
        survey.Description = dto.Description;
        if (dto.EndsAt.HasValue)
            survey.EndsAt = dto.EndsAt;
        if (dto.Status.HasValue)
            survey.Status = dto.Status.Value;
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return await GetByIdAsync(id, researcherId, isAdmin).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id, int researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
        if (survey == null)
            return false;

        if (!isAdmin && survey.ResearcherId != researcherId)
            return false;

        if (survey.DeletedAt != null)
            return false; // already soft-deleted

        survey.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
