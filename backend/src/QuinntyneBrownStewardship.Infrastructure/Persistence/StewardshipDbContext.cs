using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Domain.Scheduling;
using QuinntyneBrownStewardship.Domain.Notes;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class StewardshipDbContext(DbContextOptions<StewardshipDbContext> options) : DbContext(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ParticipantSession> Sessions => Set<ParticipantSession>();
    public DbSet<SignInAttempt> SignInAttempts => Set<SignInAttempt>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<Curriculum> Curricula => Set<Curriculum>();
    public DbSet<CurriculumModule> Modules => Set<CurriculumModule>();
    public DbSet<ModuleSection> Sections => Set<ModuleSection>();
    public DbSet<PreparationPrompt> Prompts => Set<PreparationPrompt>();
    public DbSet<SectionCompletion> Completions => Set<SectionCompletion>();
    public DbSet<CurriculumAudit> CurriculumAudits => Set<CurriculumAudit>();
    public DbSet<AvailabilitySlot> Availability => Set<AvailabilitySlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingAudit> BookingAudits => Set<BookingAudit>();
    public DbSet<Note> Notes => Set<Note>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Participant>(b => { b.HasKey(x => x.Id); b.Property(x => x.EmailAddress).HasMaxLength(254); b.Property(x => x.NormalizedEmail).HasMaxLength(254); b.HasIndex(x => x.NormalizedEmail).IsUnique(); });
        model.Entity<ParticipantSession>(b => { b.HasKey(x => x.Id); b.Property(x => x.TokenHash).HasMaxLength(64); b.HasIndex(x => x.TokenHash).IsUnique(); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId); });
        model.Entity<SignInAttempt>(b => { b.HasKey(x => x.Id); b.Property(x => x.NormalizedEmail).HasMaxLength(254); b.Property(x => x.Origin).HasMaxLength(64); b.HasIndex(x => new { x.NormalizedEmail, x.At }); b.HasIndex(x => new { x.Origin, x.At }); });
        model.Entity<Cohort>(b => { b.HasKey(x => x.Id); b.Property(x => x.MentorName).HasMaxLength(254); });
        model.Entity<Enrollment>(b => { b.HasKey(x => x.Id); b.HasIndex(x => x.ParticipantId).IsUnique().HasFilter("[IsActive] = 1"); b.HasOne(x => x.Cohort).WithMany().HasForeignKey(x => x.CohortId); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId); });
        model.Entity<Participant>().Property(x => x.DisplayName).HasMaxLength(254);
        model.Entity<Cohort>(b => { b.Property(x => x.TimeZone).HasMaxLength(100); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.MentorId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Curriculum).WithMany().HasForeignKey(x => x.CurriculumId).OnDelete(DeleteBehavior.Restrict); });
        // Publication state is stored as text so that operational SQL reads Draft and Published rather than 0 and 1.
        model.Entity<Curriculum>(b => { b.HasKey(x => x.Id); b.Property(x => x.Key).HasMaxLength(100); b.Property(x => x.Title).HasMaxLength(254); b.Property(x => x.State).HasConversion<string>().HasMaxLength(16); b.HasIndex(x => x.Key).IsUnique(); b.HasMany(x => x.Modules).WithOne().HasForeignKey(x => x.CurriculumId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<CurriculumModule>(b => { b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(254); b.Property(x => x.State).HasConversion<string>().HasMaxLength(16); b.Property(x => x.Revision).IsConcurrencyToken(); b.HasIndex(x => new { x.CurriculumId, x.Ordinal }).IsUnique(); b.HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict); b.HasMany(x => x.PreparationPrompts).WithOne().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<ModuleSection>(b => { b.HasKey(x => x.Id); b.Property(x => x.Revision).IsConcurrencyToken(); b.HasIndex(x => new { x.ModuleId, x.Ordinal }).IsUnique(); });
        model.Entity<PreparationPrompt>(b => { b.HasKey(x => x.Id); b.HasIndex(x => new { x.ModuleId, x.Ordinal }).IsUnique(); });
        model.Entity<SectionCompletion>(b => { b.HasKey(x => x.Id); b.HasIndex(x => new { x.EnrollmentId, x.SectionId }).IsUnique(); b.HasOne<Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict); b.HasOne<ModuleSection>().WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<CurriculumAudit>(b => { b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(40); b.Property(x => x.CorrelationId).HasMaxLength(128); b.HasIndex(x => new { x.TargetId, x.At }); });
        model.Entity<AvailabilitySlot>(b => { b.HasKey(x => x.Id); b.HasIndex(x => new { x.MentorId, x.StartsAt }).IsUnique(); b.HasOne<Participant>().WithMany().HasForeignKey(x => x.MentorId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<Booking>(b => { b.HasKey(x => x.Id); b.HasIndex(x => x.SlotId).IsUnique().HasFilter("[CancelledAt] IS NULL"); b.HasOne(x => x.Slot).WithMany().HasForeignKey(x => x.SlotId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<BookingAudit>(b => { b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(30); b.Property(x => x.CorrelationId).HasMaxLength(128); b.HasIndex(x => new { x.BookingId, x.At }); b.HasOne<Booking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<Note>(b => { b.HasKey(x => x.Id); b.Property(x => x.Revision).IsConcurrencyToken(); b.HasIndex(x => new { x.EnrollmentId, x.RevisedAt }); b.HasIndex(x => new { x.EnrollmentId, x.PromptId }).IsUnique().HasFilter("[PromptId] IS NOT NULL"); b.HasOne<Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict); b.HasOne<CurriculumModule>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Booking>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict); b.HasOne<PreparationPrompt>().WithMany().HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Restrict); b.ToTable(t => t.HasCheckConstraint("CK_Note_Attachment", "([ModuleId] IS NOT NULL AND [SessionId] IS NULL) OR ([ModuleId] IS NULL AND [SessionId] IS NOT NULL)")); });
    }
}
