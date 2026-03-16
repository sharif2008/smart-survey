using SurveyApi.DTOs.Response;

namespace SurveyApi.Services.Interfaces;

/// <summary>
/// Creates export files (Excel, etc.) for survey responses.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Builds an Excel file (.xlsx) containing all responses for a survey.
    /// </summary>
    /// <param name="surveyId">Survey id.</param>
    /// <param name="researcherId">Current user id (for ownership checks).</param>
    /// <param name="isAdmin">True if caller is an Admin.</param>
    /// <returns>Byte array with XLSX content, or null when survey not found or access denied.</returns>
    Task<byte[]?> ExportSurveyResponsesToExcelAsync(int surveyId, int? researcherId, bool isAdmin);
}

