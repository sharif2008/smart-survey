using System.Text.Json;
using System.Text.RegularExpressions;
using SurveyApi.DTOs.Question;
using SurveyApi.Models;

namespace SurveyApi.Helpers;

public static class AnswerValidator
{
    public static IReadOnlyList<string> Validate(Question question, string? responseText, ValidationDto? validation)
    {
        var errors = new List<string>();
        if (validation == null)
            return errors;

        var required = validation.Required ?? question.IsRequired;
        var isEmpty = string.IsNullOrWhiteSpace(responseText);

        if (required && isEmpty)
        {
            errors.Add($"Question {question.Id} is required.");
            return errors;
        }
        if (isEmpty)
            return errors;

        if (validation.MinLength.HasValue && (responseText?.Length ?? 0) < validation.MinLength.Value)
            errors.Add($"Question {question.Id}: minimum length is {validation.MinLength}.");
        if (validation.MaxLength.HasValue && (responseText?.Length ?? 0) > validation.MaxLength.Value)
            errors.Add($"Question {question.Id}: maximum length is {validation.MaxLength}.");

        if (!string.IsNullOrEmpty(validation.Regex) && !string.IsNullOrEmpty(responseText))
        {
            try
            {
                if (!Regex.IsMatch(responseText, validation.Regex))
                    errors.Add($"Question {question.Id}: format is invalid.");
            }
            catch
            {
                // Invalid regex in config; skip
            }
        }

        if (question.Type == QuestionType.Number || question.Type == QuestionType.Rating || question.Type == QuestionType.NetPromoterScore)
        {
            if (!decimal.TryParse(responseText, out var num))
                errors.Add($"Question {question.Id}: must be a number.");
            else
            {
                if (question.Type == QuestionType.NetPromoterScore && (num < 0 || num > 10))
                    errors.Add($"Question {question.Id}: NPS must be between 0 and 10.");
                if (validation.MinNumber.HasValue && num < validation.MinNumber.Value)
                    errors.Add($"Question {question.Id}: minimum value is {validation.MinNumber}.");
                if (validation.MaxNumber.HasValue && num > validation.MaxNumber.Value)
                    errors.Add($"Question {question.Id}: maximum value is {validation.MaxNumber}.");
            }
        }

        if (question.Type == QuestionType.Date && !string.IsNullOrEmpty(responseText))
        {
            if (!DateTime.TryParse(responseText, out var date))
                errors.Add($"Question {question.Id}: must be a valid date.");
            else
            {
                if (!string.IsNullOrEmpty(validation.DateMin) && DateTime.TryParse(validation.DateMin, out var min) && date < min)
                    errors.Add($"Question {question.Id}: date must be on or after {validation.DateMin}.");
                if (!string.IsNullOrEmpty(validation.DateMax) && DateTime.TryParse(validation.DateMax, out var max) && date > max)
                    errors.Add($"Question {question.Id}: date must be on or before {validation.DateMax}.");
            }
        }

        if ((question.Type == QuestionType.SingleChoice || question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.Ranking) && (validation.OptionMustExist == true || validation.MaxSelectionCount.HasValue || question.Type == QuestionType.Ranking) && !string.IsNullOrEmpty(question.OptionsJson))
        {
            HashSet<string>? options = null;
            try
            {
                var arr = JsonSerializer.Deserialize<string[]>(question.OptionsJson);
                options = (arr ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch { /* ignore */ }
            var selections = responseText?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
            if (validation.OptionMustExist == true && options != null)
            {
                foreach (var s in selections)
                {
                    if (!options.Contains(s))
                    {
                        errors.Add($"Question {question.Id}: selection must be one of the allowed options.");
                        break;
                    }
                }
            }
            if (question.Type == QuestionType.MultipleChoice && validation.MaxSelectionCount.HasValue && selections.Count > validation.MaxSelectionCount.Value)
                errors.Add($"Question {question.Id}: maximum {validation.MaxSelectionCount} selection(s) allowed.");
            if (question.Type == QuestionType.Ranking && options != null)
            {
                if (selections.Count != options.Count)
                    errors.Add($"Question {question.Id}: rank all options exactly once.");
                else
                {
                    foreach (var s in selections)
                    {
                        if (!options.Contains(s))
                        {
                            errors.Add($"Question {question.Id}: ranking must contain only the given options.");
                            break;
                        }
                    }
                }
            }
        }

        return errors;
    }
}
