using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class MigrationAcceptanceTests
{
    // Traces to: L2-053 AC4, L2-057 AC2. Given a database from the previous release, when it is
    // migrated, then its cohorts follow a published programme carrying the former constants,
    // and a key no module ever carried becomes a draft programme.
    [Fact]
    public async Task Given_a_previous_release_database_when_migrated_then_existing_cohorts_follow_a_published_programme()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("STEWARDSHIP_TEST_SQL") ?? "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True") { InitialCatalog = "StewardshipMigration_" + Guid.NewGuid().ToString("N") };
        await using var db = new StewardshipDbContext(new DbContextOptionsBuilder<StewardshipDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20260906133421_CompleteProgramme");
            var module = Guid.NewGuid(); var section = Guid.NewGuid(); var cohort = Guid.NewGuid(); var orphan = Guid.NewGuid();
            await db.Database.ExecuteSqlAsync($"INSERT INTO Modules (Id, CurriculumKey, Ordinal, Title, Summary, EffortEstimate, PracticeSteps) VALUES ({module}, 'starter', 1, 'Begin with stewardship', 'Summary', '30 minutes', '[\"Listen\"]')");
            await db.Database.ExecuteSqlAsync($"INSERT INTO Sections (Id, ModuleId, Ordinal, Title, Reading, CreatedAt) VALUES ({section}, {module}, 1, 'Section', 'Reading', SYSDATETIMEOFFSET())");
            await db.Database.ExecuteSqlAsync($"INSERT INTO Cohorts (Id, StartDate, MentorName, MentorId, CurriculumKey, TimeZone) VALUES ({cohort}, '2026-09-01', 'Mentor', NULL, 'starter', 'America/Toronto')");
            await db.Database.ExecuteSqlAsync($"INSERT INTO Cohorts (Id, StartDate, MentorName, MentorId, CurriculumKey, TimeZone) VALUES ({orphan}, '2026-09-01', 'Mentor', NULL, 'orphan', 'America/Toronto')");
            await db.GetService<IMigrator>().MigrateAsync();
            var curricula = await db.Curricula.Include(x => x.Modules).ToListAsync();
            var starter = Assert.Single(curricula, x => x.Key == "starter");
            Assert.Equal(PublicationState.Published, starter.State); Assert.NotNull(starter.PublishedAt);
            var stored = Assert.Single(starter.Modules);
            Assert.Equal(module, stored.Id); Assert.Equal(PublicationState.Published, stored.State); Assert.NotEqual(Guid.Empty, stored.Revision);
            var storedCohort = await db.Cohorts.SingleAsync(x => x.Id == cohort);
            Assert.Equal(starter.Id, storedCohort.CurriculumId); Assert.Equal(12, storedCohort.DurationWeeks); Assert.Equal(2, storedCohort.SessionCadenceWeeks);
            Assert.NotEqual(Guid.Empty, (await db.Sections.SingleAsync()).Revision);
            var draft = Assert.Single(curricula, x => x.Key == "orphan");
            Assert.Equal(PublicationState.Draft, draft.State); Assert.Null(draft.PublishedAt);
            Assert.Equal(draft.Id, (await db.Cohorts.SingleAsync(x => x.Id == orphan)).CurriculumId);
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE name = 'IX_Modules_CurriculumKey_Ordinal'").SingleAsync());
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE name = 'CurriculumKey'").SingleAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
