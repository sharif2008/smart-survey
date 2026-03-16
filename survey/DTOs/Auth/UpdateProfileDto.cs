using System.ComponentModel.DataAnnotations;

namespace SurveyApi.DTOs.Auth;

public class UpdateProfileDto
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}

