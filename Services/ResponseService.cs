using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs;
using SurveyApi.DTOs.Response;
using SurveyApi.Models;

namespace SurveyApi.Services;

/// <summary>
/// Implements response submission, listing, and analytics. Generates dynamic summaries by question type.
/// </summary>
public class ResponseService : IResponseService
{
    private const int MaxSampleResponses = 5;
    private readonly AppDbContext _db;

    public ResponseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SurveySubmissionResponseDto?> SubmitAsync(SubmitSurveyResponseDto dto)
    {
        var survey = await _db.Surveys.FindAsync(dto.SurveyId).ConfigureAwait(false);
        if (survey == null)
            return null;

        var response = new SurveyResponse
        {
            SurveyId = dto.SurveyId,
            ParticipantName = dto.ParticipantName
        };
        _db.SurveyResponses.Add(response);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var a in dto.Answers)
        {
            var answer = new Answer
            {
                SurveyResponseId = response.Id,
                QuestionId = a.QuestionId,
                ResponseText = a.ResponseText
            };
            _db.Answers.Add(answer);
        }
        await _db.SaveChangesAsync().ConfigureAwait(false);

        return new SurveySubmissionResponseDto
        {
            SurveyResponseId = response.Id,
            SurveyId = response.SurveyId,
            SubmittedAt = response.SubmittedAt
        };
    }

    public async Task<IEnumerable<SurveyResponseListItemDto>?> GetBySurveyIdAsync(int surveyId, int? researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == surveyId).ConfigureAwait(false);
        if (survey == null)
            return null;
        if (!isAdmin && (!researcherId.HasValue || survey.ResearcherId != researcherId.Value))
            return null;

        return await _db.SurveyResponses
            .AsNoTracking()
            .Where(r => r.SurveyId == surveyId)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new SurveyResponseListItemDto
            {
                Id = r.Id,
                SurveyId = r.SurveyId,
                ParticipantName = r.ParticipantName,
                SubmittedAt = r.SubmittedAt
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SurveySummaryDto?> GetSurveySummaryAsync(int surveyId, int currentUserId, string currentUserRole)
    {
        // Load survey with questions; ensure we only get surveys the user can access
        var survey = await _db.Surveys
            .Include(s => s.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(s => s.Id == surveyId)
            .ConfigureAwait(false);

        if (survey == null)
            return null;

        // Authorization: Admin can access any; Researcher only their own
        if (!string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
            survey.ResearcherId != currentUserId)
            throw new UnauthorizedAccessException("Access denied to this survey.");

        // Load all answers for this survey's responses (with question for type)
        var responseIds = await _db.SurveyResponses
            .Where(r => r.SurveyId == surveyId)
            .Select(r => r.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var answers = await _db.Answers
            .Where(a => responseIds.Contains(a.SurveyResponseId))
            .Include(a => a.Question)
            .ToListAsync()
            .ConfigureAwait(false);

        var totalResponses = responseIds.Count;
        var answersByQuestion = answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var questionSummaries = new List<QuestionSummaryDto>();
        foreach (var question in survey.Questions)
        {
            var questionAnswers = answersByQuestion.GetValueOrDefault(question.Id) ?? new List<Answer>();
            var summary = BuildQuestionSummary(question, questionAnswers);
            questionSummaries.Add(new QuestionSummaryDto
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                QuestionType = question.Type,
                TotalAnswers = questionAnswers.Count,
                Summary = summary
            });
        }

        return new SurveySummaryDto
        {
            SurveyId = survey.Id,
            SurveyTitle = survey.Title,
            TotalResponses = totalResponses,
            GeneratedAt = DateTime.UtcNow,
            Questions = questionSummaries
        };
    }

    /// <summary>
    /// Builds the summary object for one question based on its type.
    /// </summary>
    private static object BuildQuestionSummary(Question question, List<Answer> questionAnswers)
    {
        var texts = questionAnswers
            .Select(a => (a.ResponseText ?? string.Empty).Trim())
            .Where(t => t.Length > 0)
            .ToList();

        return question.Type switch
        {
            QuestionType.Text or QuestionType.TextArea => BuildTextSummary(texts),
            QuestionType.YesNo => BuildYesNoSummary(texts),
            QuestionType.SingleChoice => BuildSingleChoiceSummary(texts),
            QuestionType.MultipleChoice => BuildMultipleChoiceSummary(texts),
            QuestionType.Rating => BuildRatingSummary(texts),
            QuestionType.Number => BuildNumberSummary(texts),
            QuestionType.Date => BuildDateSummary(texts),
            _ => new { message = "Unknown question type" }
        };
    }

    private static object BuildTextSummary(List<string> texts)
    {
        var sampleResponses = texts.Take(MaxSampleResponses).ToList();
        // Optional: top repeated responses (simple approach)
        var repeated = texts
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { response = g.Key, count = g.Count() })
            .ToList();
        return new
        {
            sampleResponses,
            topRepeated = repeated
        };
    }

    private static object BuildYesNoSummary(List<string> texts)
    {
        var yesCount = texts.Count(t => string.Equals(t, "Yes", StringComparison.OrdinalIgnoreCase));
        var noCount = texts.Count(t => string.Equals(t, "No", StringComparison.OrdinalIgnoreCase));
        var total = texts.Count;
        var yesPct = total > 0 ? Math.Round(100.0 * yesCount / total, 1) : 0;
        var noPct = total > 0 ? Math.Round(100.0 * noCount / total, 1) : 0;
        return new
        {
            yesCount,
            noCount,
            yesPercentage = yesPct,
            noPercentage = noPct
        };
    }

    private static object BuildSingleChoiceSummary(List<string> texts)
    {
        var total = texts.Count;
        var groups = texts
            .GroupBy(t => t)
            .Select(g => new OptionCountDto
            {
                Label = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(100.0 * g.Count() / total, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();
        return new { options = groups };
    }

    /// <summary>
    /// MultipleChoice: answers stored as comma-separated values; split and count each option.
    /// </summary>
    private static object BuildMultipleChoiceSummary(List<string> texts)
    {
        var selected = new List<string>();
        foreach (var t in texts)
        {
            var parts = t.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            selected.AddRange(parts);
        }
        var total = texts.Count; // number of respondents who answered
        var groups = selected
            .GroupBy(x => x)
            .Select(g => new OptionCountDto
            {
                Label = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(100.0 * g.Count() / total, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();
        return new { options = groups };
    }

    private static object BuildRatingSummary(List<string> texts)
    {
        var values = ParseNumbers(texts);
        if (values.Count == 0)
            return new { average = 0.0, min = 0, max = 0, distribution = new List<RatingDistributionDto>() };

        var distribution = values
            .GroupBy(v => v)
            .OrderBy(g => g.Key)
            .Select(g => new RatingDistributionDto { Label = g.Key.ToString(), Count = g.Count() })
            .ToList();

        return new
        {
            average = Math.Round(values.Average(), 2),
            min = values.Min(),
            max = values.Max(),
            distribution
        };
    }

    private static object BuildNumberSummary(List<string> texts)
    {
        var values = ParseNumbers(texts);
        if (values.Count == 0)
            return new { average = 0.0, min = 0, max = 0 };
        return new
        {
            average = Math.Round(values.Average(), 2),
            min = values.Min(),
            max = values.Max()
        };
    }

    private static object BuildDateSummary(List<string> texts)
    {
        var dates = new List<DateTime>();
        foreach (var t in texts)
        {
            if (DateTime.TryParse(t, out var d))
                dates.Add(d);
        }
        if (dates.Count == 0)
            return new { earliest = (string?)null, latest = (string?)null, dateCounts = new List<DateCountDto>() };

        var earliest = dates.Min();
        var latest = dates.Max();
        var dateCounts = dates
            .GroupBy(d => d.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateCountDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .ToList();

        return new
        {
            earliest = earliest.ToString("yyyy-MM-dd"),
            latest = latest.ToString("yyyy-MM-dd"),
            dateCounts
        };
    }

    private static List<int> ParseNumbers(List<string> texts)
    {
        var values = new List<int>();
        foreach (var t in texts)
        {
            if (int.TryParse(t, out var n))
                values.Add(n);
        }
        return values;
    }
}
