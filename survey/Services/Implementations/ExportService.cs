using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

/// <summary>
/// Excel/CSV export implementations for survey data.
/// </summary>
public class ExportService : IExportService
{
    private readonly AppDbContext _db;

    public ExportService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<byte[]?> ExportSurveyResponsesToExcelAsync(int surveyId, int? researcherId, bool isAdmin)
    {
        var survey = await _db.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == surveyId)
            .ConfigureAwait(false);

        if (survey == null)
            return null;

        if (!isAdmin && (!researcherId.HasValue || survey.ResearcherId != researcherId.Value))
            return null;

        var questions = survey.Questions
            .OrderBy(q => q.Page != null ? q.Page.Order : 0)
            .ThenBy(q => q.Order)
            .ToList();

        var responses = await _db.SurveyResponses
            .AsNoTracking()
            .Where(r => r.SurveyId == surveyId)
            .Include(r => r.Answers)
            .ThenInclude(a => a.Question)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync()
            .ConfigureAwait(false);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Responses");

        var col = 1;
        ws.Cell(1, col++).Value = "SurveyId";
        ws.Cell(1, col++).Value = "ResponseId";
        ws.Cell(1, col++).Value = "ParticipantName";
        ws.Cell(1, col++).Value = "SubmittedAtUtc";

        foreach (var q in questions)
        {
            ws.Cell(1, col++).Value = $"Q{q.Id}: {q.Text}";
        }

        for (var i = 0; i < responses.Count; i++)
        {
            var r = responses[i];
            var row = i + 2;
            var c = 1;

            ws.Cell(row, c++).Value = r.SurveyId;
            ws.Cell(row, c++).Value = r.Id;
            ws.Cell(row, c++).Value = r.ParticipantName ?? string.Empty;
            ws.Cell(row, c++).Value = r.SubmittedAt.ToUniversalTime();

            foreach (var q in questions)
            {
                var answer = r.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                ws.Cell(row, c++).Value = answer?.ResponseText ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

