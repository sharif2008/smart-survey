using System.Text.Json;
using SurveyApi.DTOs.Question;

namespace SurveyApi.Helpers;

public static class ShowIfHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static ShowIfDto? FromJson(string? showIfJson)
    {
        if (string.IsNullOrWhiteSpace(showIfJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<ShowIfDto>(showIfJson, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static string? ToJson(ShowIfDto? showIf)
    {
        if (showIf == null)
            return null;
        try
        {
            return JsonSerializer.Serialize(showIf, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
