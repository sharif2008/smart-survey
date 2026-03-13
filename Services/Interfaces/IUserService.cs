using SurveyApi.DTOs.Auth;

namespace SurveyApi.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto?> CreateAsync(RegisterDto dto);
    Task<UserResponseDto?> UpdateAsync(int id, RegisterDto dto);
    Task<bool> DeleteAsync(int id);
}
