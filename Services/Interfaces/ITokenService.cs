using SurveyApi.Models;

namespace SurveyApi.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, string? roleName = null);
}
