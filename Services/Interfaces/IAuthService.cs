using SurveyApi.DTOs.Auth;

namespace SurveyApi.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
}
