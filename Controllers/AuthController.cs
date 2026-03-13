using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Auth;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>POST /api/auth/register — Register a new user (Admin or Researcher).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and Password are required.");
        if (dto.Role != "Admin" && dto.Role != "Researcher")
            return BadRequest("Role must be Admin or Researcher.");

        var result = await _authService.RegisterAsync(dto).ConfigureAwait(false);
        if (result == null)
            return Conflict("A user with this email already exists.");
        return Ok(result);
    }

    /// <summary>POST /api/auth/login — Login and get JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto).ConfigureAwait(false);
        if (result == null)
            return Unauthorized("Invalid email or password.");
        return Ok(result);
    }
}
