using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs;
using SurveyApi.DTOs.Question;
using SurveyApi.DTOs.Response;
using SurveyApi.Helpers;
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

    public async Task<SubmitResultDto> SubmitAsync(SubmitSurveyResponseDto dto)
    {
        var survey = await _db.Surveys.FindAsync(dto.SurveyId).ConfigureAwait(false);
        if (survey == null)
            return new SubmitResultDto();

        var questions = await _db.Questions
            .AsNoTracking()
            .Where(q => q.SurveyId == dto.SurveyId)
            .ToListAsync().ConfigureAwait(false);
        var questionMap = questions.ToDictionary(q => q.Id);

        var validationErrors = new List<string>();
        foreach (var question in questions)
        {
            var validation = ValidationHelper.FromJson(question.ValidationJson);
            var required = validation?.Required ?? question.IsRequired;
            if (!required)
                continue;
            var answerDto = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var responseText = answerDto?.ResponseText;
            if (string.IsNullOrWhiteSpace(responseText))
                validationErrors.Add($"Question {question.Id} is required.");
        }
        foreach (var a in dto.Answers)
        {
            if (!questionMap.TryGetValue(a.QuestionId, out var question))
                continue;
            var validation = ValidationHelper.FromJson(question.ValidationJson);
            var errors = AnswerValidator.Validate(question, a.ResponseText, validation);
            validationErrors.AddRange(errors);
        }

        if (validationErrors.Count > 0)
            return new SubmitResultDto { ValidationErrors = validationErrors };

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

        return new SubmitResultDto
        {
            Result = new SurveySubmissionResponseDto
            {
                SurveyResponseId = response.Id,
                SurveyId = response.SurveyId,
                SubmittedAt = response.SubmittedAt
            }
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
        // Load survey with questions and pages for ordering; ensure we only get surveys the user can access
        var survey = await _db.Surveys
            .Include(s => s.Questions)
            .ThenInclude(q => q.Page)
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
        foreach (var question in survey.Questions.OrderBy(q => q.Page.Order).ThenBy(q => q.Order))
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
    /// Builds the summary object for one question. Analytics are automatic per question type:
    /// text (response count, samples, frequent words), yes/no (counts, %), single/multiple choice (count per option, %, bar chart for single),
    /// rating (avg, min, max, distribution), number (min, max, avg), date (grouped by day/week/month).
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
        var responseCount = texts.Count;
        var sampleAnswers = texts.Take(MaxSampleResponses).ToList();
        var repeated = texts
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { response = g.Key, count = g.Count() })
            .ToList();
        var frequentWords = GetFrequentWords(texts, minLength: 2, topN: 20);
        return new
        {
            responseCount,
            sampleAnswers,
            topRepeated = repeated,
            frequentWords
        };
    }

    private static List<object> GetFrequentWords(List<string> texts, int minLength, int topN)
    {
        var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in texts)
        {
            var words = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                var word = w.Trim();
                if (word.Length >= minLength)
                    wordCounts[word] = wordCounts.GetValueOrDefault(word, 0) + 1;
            }
        }
        return wordCounts
            .OrderByDescending(p => p.Value)
            .Take(topN)
            .Select(p => new { word = p.Key, count = p.Value })
            .Cast<object>()
            .ToList();
    }

    private static object BuildYesNoSummary(List<string> texts)
    {
        var responseCount = texts.Count;
        var yesCount = texts.Count(t => string.Equals(t, "Yes", StringComparison.OrdinalIgnoreCase));
        var noCount = texts.Count(t => string.Equals(t, "No", StringComparison.OrdinalIgnoreCase));
        var yesPct = responseCount > 0 ? Math.Round(100.0 * yesCount / responseCount, 1) : 0;
        var noPct = responseCount > 0 ? Math.Round(100.0 * noCount / responseCount, 1) : 0;
        return new
        {
            responseCount,
            yesCount,
            noCount,
            yesPercentage = yesPct,
            noPercentage = noPct
        };
    }

    private static object BuildSingleChoiceSummary(List<string> texts)
    {
        var total = texts.Count;
        var options = texts
            .GroupBy(t => t)
            .Select(g => new OptionCountDto
            {
                Label = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(100.0 * g.Count() / total, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();
        var barChartData = options.Select(o => new { label = o.Label, count = o.Count }).ToList();
        return new { responseCount = total, options, barChartData };
    }

    /// <summary>
    /// MultipleChoice: answers stored as comma-separated values; split and count each option. Percentage = % of respondents who selected this option.
    /// </summary>
    private static object BuildMultipleChoiceSummary(List<string> texts)
    {
        var respondentCount = texts.Count;
        var responseOptions = texts.Select(t => t.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet()).ToList();
        var optionToRespondentCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var set in responseOptions)
        {
            foreach (var opt in set)
            {
                if (!string.IsNullOrEmpty(opt))
                    optionToRespondentCount[opt] = optionToRespondentCount.GetValueOrDefault(opt, 0) + 1;
            }
        }
        var totalSelections = optionToRespondentCount.Values.Sum();
        var options = optionToRespondentCount
            .OrderByDescending(p => p.Value)
            .Select(g => new OptionCountDto
            {
                Label = g.Key,
                Count = g.Value,
                Percentage = respondentCount > 0 ? Math.Round(100.0 * g.Value / respondentCount, 1) : 0
            })
            .ToList();
        return new { responseCount = respondentCount, options };
    }

    private static object BuildRatingSummary(List<string> texts)
    {
        var values = ParseDecimals(texts);
        if (values.Count == 0)
            return new { responseCount = 0, average = 0.0, min = 0.0, max = 0.0, distribution = new List<RatingDistributionDto>() };

        var distribution = values
            .GroupBy(v => v)
            .OrderBy(g => g.Key)
            .Select(g => new RatingDistributionDto { Label = g.Key.ToString("0.##"), Count = g.Count() })
            .ToList();

        return new
        {
            responseCount = values.Count,
            average = Math.Round((double)values.Average(), 2),
            min = (double)values.Min(),
            max = (double)values.Max(),
            distribution
        };
    }

    private static object BuildNumberSummary(List<string> texts)
    {
        var values = ParseDecimals(texts);
        if (values.Count == 0)
            return new { responseCount = 0, average = 0.0, min = 0.0, max = 0.0 };
        return new
        {
            responseCount = values.Count,
            average = Math.Round((double)values.Average(), 2),
            min = (double)values.Min(),
            max = (double)values.Max()
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
            return new
            {
                responseCount = 0,
                earliest = (string?)null,
                latest = (string?)null,
                groupedByDay = new List<DateCountDto>(),
                groupedByWeek = new List<DateCountDto>(),
                groupedByMonth = new List<DateCountDto>()
            };

        var earliest = dates.Min();
        var latest = dates.Max();
        var groupedByDay = dates
            .GroupBy(d => d.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateCountDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .ToList();
        var groupedByWeek = dates
            .GroupBy(d => GetStartOfWeek(d))
            .OrderBy(g => g.Key)
            .Select(g => new DateCountDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .ToList();
        var groupedByMonth = dates
            .GroupBy(d => new DateTime(d.Year, d.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new DateCountDto { Date = g.Key.ToString("yyyy-MM"), Count = g.Count() })
            .ToList();

        return new
        {
            responseCount = dates.Count,
            earliest = earliest.ToString("yyyy-MM-dd"),
            latest = latest.ToString("yyyy-MM-dd"),
            groupedByDay,
            groupedByWeek,
            groupedByMonth
        };
    }

    private static DateTime GetStartOfWeek(DateTime d)
    {
        var diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
        return d.AddDays(-diff).Date;
    }

    private static List<decimal> ParseDecimals(List<string> texts)
    {
        var values = new List<decimal>();
        foreach (var t in texts)
        {
            if (decimal.TryParse(t, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var n))
                values.Add(n);
        }
        return values;
    }
}
