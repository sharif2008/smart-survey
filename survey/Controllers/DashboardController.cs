using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyApi.DTOs.Dashboard;
using SurveyApi.Services.Interfaces;

namespace SurveyApi.Controllers;

/// <summary>
/// Dashboard statistics. Admin: all surveys/responses/researchers. Researcher: own summary.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Researcher")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private (int? userId, bool isAdmin) GetCurrentUser()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var userId = int.TryParse(idClaim, out var id) ? id : (int?)null;
        return (userId, isAdmin);
    }

    /// <summary>GET /api/dashboard/stats — Dashboard stats (role-based).</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var (userId, isAdmin) = GetCurrentUser();
        var stats = await _dashboardService.GetStatsAsync(userId, isAdmin).ConfigureAwait(false);
        return Ok(stats);
    }
}
