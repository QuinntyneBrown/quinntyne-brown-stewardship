using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class AdministrationAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;
    // Every CLI verb runs in a process of its own, so each command here gets the scope one would have.
    private async Task<T> Cli<T>(MediatR.IRequest<T> command)
    {
        using var scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }
    private static async Task<CurriculumImport> Content() => JsonSerializer.Deserialize<CurriculumImport>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "starter-curriculum.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    // Traces to: L2-056 AC1, L2-057 AC1, L2-058 AC3–AC4. Given a three-module document,
    // when it is imported, published and followed by a six-week cohort, then the
    // participant path has three markers and every allowance figure reads three.
    [Fact]
    public async Task Given_a_three_module_document_when_imported_then_the_participant_path_has_three_markers()
    {
        var starter = await Content();
        var document = new CurriculumImport("short", "Short programme", starter.Modules.Where(x => x.Ordinal <= 3).ToList());
        Assert.Equal(3, await Cli(new ImportCurriculumCommand(document)));
        await fixture.PublishCurriculum(await fixture.CurriculumId("short"));
        Assert.True(await Cli(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Assigned mentor")));
        var cohortId = Guid.NewGuid();
        await Cli(new CreateCohortCommand(cohortId, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime), "mentor@example.com", "short", 6, 2));
        await Cli(new EnrollParticipantCommand(ApiFixture.Email, cohortId));
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var path = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(3, path.GetProperty("modules").GetArrayLength());
        Assert.Equal(3, path.GetProperty("remaining").GetInt32());
        Assert.Equal(3, (await client.GetFromJsonAsync<JsonElement>("/enrollment")).GetProperty("sessionAllowance").GetInt32());
        Assert.Equal(3, (await client.GetFromJsonAsync<JsonElement>("/sessions/availability")).GetProperty("allowance").GetInt32());
        Assert.Equal(starter.Modules[0].Sections[0].Reading, (await client.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("sections")[0].GetProperty("reading").GetString());
    }

    // Traces to: L2-052 AC1, L2-044 AC2. Given the starter document, when it is imported
    // twice, then one draft programme exists and the second import is refused without changing it.
    [Fact]
    public async Task Given_the_starter_curriculum_when_imported_twice_then_the_second_import_is_refused_and_content_is_intact()
    {
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(5, await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ImportCurriculumCommand(await Content())));
        // Each command has the same scoped lifetime as a separate CLI invocation.
        using (var repeat = fixture.Services.CreateScope())
        {
            var refusal = await Assert.ThrowsAsync<ProgrammeException>(async () => await repeat.ServiceProvider.GetRequiredService<ISender>().Send(new ImportCurriculumCommand(await Content())));
            Assert.Equal(409, refusal.StatusCode); Assert.Contains("starter", refusal.Message);
        }
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var curriculum = await db.Curricula.SingleAsync();
        Assert.Equal(PublicationState.Draft, curriculum.State); Assert.Null(curriculum.PublishedAt); Assert.Equal("Redemptive Technology Design", curriculum.Title);
        Assert.Equal(5, await db.Modules.CountAsync()); Assert.Equal(25, await db.Sections.CountAsync()); Assert.Equal(15, await db.Prompts.CountAsync());
        Assert.All(await db.Modules.ToListAsync(), module => Assert.Equal(PublicationState.Draft, module.State));
    }

    // Traces to: L2-053 AC1, L2-057 AC1–AC4. Given a draft curriculum, when a cohort is created,
    // then it is refused until the curriculum is published, and the cohort's own duration and
    // cadence derive its allowance and cap its week.
    [Fact]
    public async Task Given_a_draft_curriculum_when_a_cohort_is_created_then_it_is_refused_until_the_curriculum_is_published()
    {
        await Cli(new ImportCurriculumCommand(await Content()));
        await Cli(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Assigned mentor"));
        var start = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime);
        var refused = await Assert.ThrowsAsync<ProgrammeException>(() => Cli(new CreateCohortCommand(Guid.NewGuid(), start, "mentor@example.com", "starter", 12, 2)));
        Assert.Equal(409, refused.StatusCode);
        var missing = await Assert.ThrowsAsync<ProgrammeException>(() => Cli(new CreateCohortCommand(Guid.NewGuid(), start, "mentor@example.com", "unknown", 12, 2)));
        Assert.Equal(404, missing.StatusCode);
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => Cli(new CreateCohortCommand(Guid.NewGuid(), start, "mentor@example.com", "starter", 4, 6)));
        await fixture.PublishCurriculum(await fixture.CurriculumId("starter"));
        var cohortId = Guid.NewGuid();
        Assert.Equal(cohortId, await Cli(new CreateCohortCommand(cohortId, start.AddDays(-80), "mentor@example.com", "starter", 10, 3)));
        await Cli(new EnrollParticipantCommand(ApiFixture.Email, cohortId));
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var enrollment = await client.GetFromJsonAsync<JsonElement>("/enrollment");
        Assert.Equal(3, enrollment.GetProperty("sessionAllowance").GetInt32());
        Assert.Equal(10, enrollment.GetProperty("currentWeek").GetInt32());
        Assert.Equal(10, enrollment.GetProperty("durationWeeks").GetInt32());
        Assert.Equal(3, enrollment.GetProperty("sessionCadenceWeeks").GetInt32());
        Assert.True(enrollment.GetProperty("isProgrammePublished").GetBoolean());
    }

    // Traces to: L2-005–006, L2-017. Invalid operations leave persisted programme
    // state unchanged: no second active cohort and no overlapping mentor slots.
    [Fact]
    public async Task Given_programme_setup_when_enrollment_or_slots_conflict_then_the_operation_is_rejected()
    {
        await Cli(new ImportCurriculumCommand(await Content()));
        await fixture.PublishCurriculum(await fixture.CurriculumId("starter"));
        await Cli(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Assigned mentor"));
        var cohort = Guid.NewGuid(); var other = Guid.NewGuid(); var start = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime);
        await Cli(new CreateCohortCommand(cohort, start, "mentor@example.com", "starter", 12, 2));
        await Cli(new CreateCohortCommand(other, start, "mentor@example.com", "starter", 12, 2));
        await Cli(new EnrollParticipantCommand(ApiFixture.Email, cohort));
        await Assert.ThrowsAsync<ProgrammeException>(() => Cli(new EnrollParticipantCommand(ApiFixture.Email, other)));
        await Assert.ThrowsAsync<ProgrammeException>(() => Cli(new PublishAvailabilityCommand(new("mentor@example.com", [new(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2)), new(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2).AddMinutes(30))]))));
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(1, await db.Enrollments.CountAsync()); Assert.Equal(0, await db.Availability.CountAsync());
    }
}
