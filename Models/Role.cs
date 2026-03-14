namespace SurveyApi.Models;

/// <summary>
/// Role entity – separate from User. e.g. Admin, Researcher, Participant.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
