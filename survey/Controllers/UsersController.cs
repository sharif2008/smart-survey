using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Auth;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
// Admin and Researcher can access user management endpoints,
// but only Admin can create new users (see [Authorize] on Create).
[Authorize(Roles = "Admin,Researcher")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>GET /api/users — List all users (Admin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _userService.GetAllAsync().ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>GET /api/users/{id}</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id).ConfigureAwait(false);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    /// <summary>POST /api/users — Create user (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and Password are required.");
        if (dto.Role != "Admin" && dto.Role != "Researcher")
            return BadRequest("Role must be Admin or Researcher.");

        var user = await _userService.CreateAsync(dto).ConfigureAwait(false);
        if (user == null)
            return Conflict("A user with this email already exists.");
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>PUT /api/users/{id}</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] RegisterDto dto)
    {
        if (dto.Role != "Admin" && dto.Role != "Researcher")
            return BadRequest("Role must be Admin or Researcher.");
        var user = await _userService.UpdateAsync(id, dto).ConfigureAwait(false);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    /// <summary>DELETE /api/users/{id}</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _userService.DeleteAsync(id).ConfigureAwait(false);
        if (!ok)
            return NotFound();
        return NoContent();
    }
}
