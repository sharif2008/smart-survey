namespace SurveyApi.Models;

/// <summary>
/// User – linked to a Role via RoleId (User and Role are separate entities).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;
    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
