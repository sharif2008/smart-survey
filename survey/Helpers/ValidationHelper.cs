using System.Text.Json;
using SurveyApi.DTOs.Question;

namespace SurveyApi.Helpers;

public static class ValidationHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static ValidationDto? FromJson(string? validationJson)
    {
        if (string.IsNullOrWhiteSpace(validationJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<ValidationDto>(validationJson, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static string? ToJson(ValidationDto? validation)
    {
        if (validation == null)
            return null;
        try
        {
            return JsonSerializer.Serialize(validation, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
