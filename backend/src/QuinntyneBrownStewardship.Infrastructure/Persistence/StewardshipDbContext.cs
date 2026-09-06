using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class StewardshipDbContext(DbContextOptions<StewardshipDbContext> options) : DbContext(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ParticipantSession> Sessions => Set<ParticipantSession>();
    public DbSet<SignInAttempt> SignInAttempts => Set<SignInAttempt>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Participant>(b => { b.HasKey(x => x.Id); b.Property(x => x.EmailAddress).HasMaxLength(254); b.Property(x => x.NormalizedEmail).HasMaxLength(254); b.HasIndex(x => x.NormalizedEmail).IsUnique(); });
        model.Entity<ParticipantSession>(b => { b.HasKey(x => x.Id); b.Property(x => x.TokenHash).HasMaxLength(64); b.HasIndex(x => x.TokenHash).IsUnique(); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId); });
        model.Entity<SignInAttempt>(b => { b.HasKey(x => x.Id); b.Property(x => x.NormalizedEmail).HasMaxLength(254); b.Property(x => x.Origin).HasMaxLength(64); b.HasIndex(x => new { x.NormalizedEmail, x.At }); b.HasIndex(x => new { x.Origin, x.At }); });
        model.Entity<Cohort>(b => { b.HasKey(x => x.Id); b.Property(x => x.MentorName).HasMaxLength(254); });
        model.Entity<Enrollment>(b => { b.HasKey(x => x.Id); b.HasIndex(x => x.ParticipantId).IsUnique().HasFilter("[IsActive] = 1"); b.HasOne(x => x.Cohort).WithMany().HasForeignKey(x => x.CohortId); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId); });
    }
}
