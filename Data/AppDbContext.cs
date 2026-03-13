using Microsoft.EntityFrameworkCore;
using SurveyApi.Models;

namespace SurveyApi.Data;

/// <summary>
/// Main database context for Survey API. Configure for SQL Server in Program.cs.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.HasOne(e => e.Researcher)
                .WithMany(u => u.Surveys)
                .HasForeignKey(e => e.ResearcherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).HasMaxLength(2000);
            entity.HasOne(e => e.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(e => e.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParticipantName).HasMaxLength(200);
            entity.HasOne(e => e.Survey)
                .WithMany(s => s.SurveyResponses)
                .HasForeignKey(e => e.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResponseText).HasMaxLength(4000);
            entity.HasOne(e => e.SurveyResponse)
                .WithMany(r => r.Answers)
                .HasForeignKey(e => e.SurveyResponseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
