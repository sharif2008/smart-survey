using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SurveyApi.Data;
using SurveyApi.DTOs.Auth;
using SurveyApi.Models;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var list = await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.Name,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync()
            .ConfigureAwait(false);
        return list;
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id).ConfigureAwait(false);
        return user == null ? null : new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto?> CreateAsync(RegisterDto dto)
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
        return await GetByIdAsync(user.Id).ConfigureAwait(false);
    }

    public async Task<UserResponseDto?> UpdateAsync(int id, RegisterDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id).ConfigureAwait(false);
        if (user == null)
            return null;

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id).ConfigureAwait(false);
        if (existing != null)
            return null;

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        if (dto.Role is "Admin" or "Researcher")
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role).ConfigureAwait(false);
            if (role != null)
                user.RoleId = role.Id;
        }
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = _hasher.HashPassword(null!, dto.Password);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return await GetByIdAsync(user.Id).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id).ConfigureAwait(false);
        if (user == null)
            return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
