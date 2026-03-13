using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Auth;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email).ConfigureAwait(false);
        if (user == null)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return new AuthResponseDto
        {
            Token = _tokenService.GenerateToken(user),
            User = MapToUserResponse(user)
        };
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email).ConfigureAwait(false))
            return null;

        var roleName = dto.Role is "Admin" or "Researcher" ? dto.Role : "Researcher";
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName).ConfigureAwait(false);
        if (role == null)
            return null;

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = _hasher.HashPassword(null!, dto.Password),
            RoleId = role.Id
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        return new AuthResponseDto
        {
            Token = _tokenService.GenerateToken(user, roleName),
            User = MapToUserResponse(user, roleName)
        };
    }

    private static UserResponseDto MapToUserResponse(User u, string? roleName = null) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Role = roleName ?? u.Role?.Name ?? string.Empty,
        CreatedAt = u.CreatedAt
    };
}
