using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class PublicationAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;

    // Traces to: L2-053 AC1, L2-052 AC1. Given a cohort following a draft programme, when the
    // participant opens the curriculum, then they are told it is not yet available and read no module.
    [Fact]
    public async Task Given_a_cohort_following_a_draft_programme_when_the_curriculum_is_read_then_it_is_reported_unavailable()
    {
        await fixture.Enroll(await fixture.SeedCohort(await fixture.SeedProgramme(publish: false)));
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var enrollment = await client.GetFromJsonAsync<JsonElement>("/enrollment");
        Assert.True(enrollment.GetProperty("isEnrolled").GetBoolean()); Assert.False(enrollment.GetProperty("isProgrammePublished").GetBoolean());
        var path = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.True(path.GetProperty("isEnrolled").GetBoolean()); Assert.False(path.GetProperty("isProgrammePublished").GetBoolean());
        Assert.Equal(0, path.GetProperty("modules").GetArrayLength()); Assert.Equal(0, path.GetProperty("remaining").GetInt32());
        var module = await client.GetAsync("/modules/current");
        Assert.Equal(HttpStatusCode.Conflict, module.StatusCode);
        Assert.Contains("not yet available", (await module.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    // Traces to: L2-053 AC2–AC4, L2-052 AC2, L2-056 AC1, AC3–AC4, L2-009 AC3. Given a published
    // programme of eight modules holding a ninth in draft, when a participant reads, then the
    // draft module is absent, every total reads against eight, and the draft ordinal is refused.
    [Fact]
    public async Task Given_a_published_programme_holding_a_draft_module_when_a_participant_reads_then_every_total_reads_against_eight()
    {
        var curriculum = await fixture.SeedProgramme(modules: 8);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            db.Modules.Add(new CurriculumModule { CurriculumId = curriculum, Ordinal = 9, Title = "Draft module", Summary = "Not yet published", EffortEstimate = "30 minutes", PracticeSteps = ["Listen"], Sections = [new() { Ordinal = 1, Title = "Section 1", Reading = "Draft reading", CreatedAt = fixture.Clock.UtcNow }] });
            await db.SaveChangesAsync();
        }
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var path = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.True(path.GetProperty("isProgrammePublished").GetBoolean());
        Assert.Equal(8, path.GetProperty("modules").GetArrayLength()); Assert.Equal(8, path.GetProperty("remaining").GetInt32());
        Assert.Equal(0, path.GetProperty("completed").GetInt32()); Assert.Equal(0, path.GetProperty("percent").GetInt32());
        Assert.Equal(Enumerable.Range(1, 8), path.GetProperty("modules").EnumerateArray().Select(x => x.GetProperty("ordinal").GetInt32()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/modules/9")).StatusCode);
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("ordinal").GetInt32());
        using var single = await fixture.Participant("single@example.com");
        await fixture.Enroll(await fixture.SeedCohort(await fixture.SeedProgramme(modules: 1, key: "single")), "single@example.com");
        var one = await single.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(1, one.GetProperty("modules").GetArrayLength()); Assert.Equal(1, one.GetProperty("remaining").GetInt32()); Assert.Equal(1, one.GetProperty("currentOrdinal").GetInt32());
    }

    // Traces to: L2-057 AC1–AC4, L2-006 AC1–AC2, AC4, L2-023 AC1, AC3–AC4, L2-058 AC1, AC3–AC4.
    // Given cohorts of different sizes and durations, when their figures are read, then each
    // derives from its own record and an exhausted allowance of four is stated as four.
    [Fact]
    public async Task Given_cohorts_of_different_durations_when_their_figures_are_read_then_each_derives_from_its_own_record()
    {
        var eight = await fixture.SeedProgramme(modules: 8, key: "eight");
        var twelve = await fixture.SeedProgramme(modules: 12, key: "twelve");
        var start = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-7);
        var shortCohort = await fixture.SeedCohort(eight, durationWeeks: 8, cadenceWeeks: 2, start: start);
        await fixture.Enroll(shortCohort);
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var enrollment = await client.GetFromJsonAsync<JsonElement>("/enrollment");
        Assert.Equal(4, enrollment.GetProperty("sessionAllowance").GetInt32()); Assert.Equal(8, enrollment.GetProperty("durationWeeks").GetInt32()); Assert.Equal(2, enrollment.GetProperty("sessionCadenceWeeks").GetInt32());
        Assert.Equal(2, enrollment.GetProperty("currentWeek").GetInt32()); Assert.Equal(start.AddDays(56), DateOnly.Parse(enrollment.GetProperty("endDate").GetString()!));
        var path = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(8, path.GetProperty("modules").GetArrayLength()); Assert.Equal(8, path.GetProperty("cohort").GetProperty("durationWeeks").GetInt32());
        Assert.Equal(4, (await client.GetFromJsonAsync<JsonElement>("/sessions/availability")).GetProperty("allowance").GetInt32());
        using var second = await fixture.Participant("second@example.com");
        await fixture.Enroll(await fixture.SeedCohort(twelve, durationWeeks: 12, cadenceWeeks: 2, start: start), "second@example.com");
        Assert.Equal(6, (await second.GetFromJsonAsync<JsonElement>("/enrollment")).GetProperty("sessionAllowance").GetInt32());
        Assert.Equal(12, (await second.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules").GetArrayLength());
        using var third = await fixture.Participant("third@example.com");
        await fixture.Enroll(await fixture.SeedCohort(eight, durationWeeks: 9, cadenceWeeks: 2, start: start.AddDays(-80)), "third@example.com");
        var nine = await third.GetFromJsonAsync<JsonElement>("/enrollment");
        Assert.Equal(4, nine.GetProperty("sessionAllowance").GetInt32()); Assert.Equal(9, nine.GetProperty("currentWeek").GetInt32());
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            var cohort = await db.Cohorts.SingleAsync(x => x.Id == shortCohort);
            var enrolled = await db.Enrollments.SingleAsync(x => x.CohortId == shortCohort);
            for (var i = 0; i < 4; i++) db.Bookings.Add(new() { EnrollmentId = enrolled.Id, CreatedAt = fixture.Clock.UtcNow.AddDays(-20), Slot = new() { MentorId = cohort.MentorId!.Value, StartsAt = fixture.Clock.UtcNow.AddDays(-i - 1) } });
            await db.SaveChangesAsync();
        }
        var exhausted = await client.GetFromJsonAsync<JsonElement>("/sessions/availability");
        Assert.Equal(4, exhausted.GetProperty("bookedCount").GetInt32());
        Assert.Contains("All 4 sessions", exhausted.GetProperty("bookingReason").GetString());
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    // Traces to: L2-054 AC1–AC2, AC4, L2-060 AC4. Given a programme whose third module has no sections, when
    // publication is attempted, then it is refused naming the module and no participant's reading changes.
    [Fact]
    public async Task Given_an_incomplete_programme_when_published_then_it_is_refused_naming_the_empty_module_and_participants_are_unchanged()
    {
        var published = await fixture.SeedProgramme(modules: 12);
        await fixture.Enroll(await fixture.SeedCohort(published));
        var incomplete = await fixture.SeedProgramme(modules: 4, key: "foundations", publish: false);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            var third = await db.Modules.SingleAsync(x => x.CurriculumId == incomplete && x.Ordinal == 3);
            third.Title = "Choose enough"; await db.Sections.Where(x => x.ModuleId == third.Id).ExecuteDeleteAsync(); await db.SaveChangesAsync();
        }
        using var administrator = await fixture.Administrator();
        var refused = await ApiFixture.Post(administrator, $"/administration/curricula/{incomplete}/publication", new { });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("1 module carries no section: 03 Choose enough. Every module needs at least one before this programme can be published.", (await Body(refused)).GetProperty("title").GetString());
        var bare = (await Body(await ApiFixture.Post(administrator, "/administration/curricula", new { key = "bare", title = "Bare" }))).GetProperty("id").GetGuid();
        var empty = await ApiFixture.Post(administrator, $"/administration/curricula/{bare}/publication", new { });
        Assert.Equal(HttpStatusCode.Conflict, empty.StatusCode); Assert.Contains("has no modules", (await Body(empty)).GetProperty("title").GetString());
        var draft = await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{incomplete}");
        Assert.Equal("Draft", draft.GetProperty("state").GetString()); Assert.All(draft.GetProperty("modules").EnumerateArray(), m => Assert.Equal("Draft", m.GetProperty("state").GetString()));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        Assert.Equal(12, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules").GetArrayLength());
        using (var scope = fixture.Services.CreateScope()) Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.CountAsync(x => x.Action == "CurriculumPublished"));
    }

    // Traces to: L2-054 AC3, L2-055 AC1–AC4, L2-056 AC2, L2-058 AC2, L2-061 AC4. Given a complete draft programme,
    // when it is published, then every module is readable at once and the act is audited; a module added later
    // stays invisible until the next publication, while a revised section reaches the participant without one.
    [Fact]
    public async Task Given_a_complete_draft_programme_when_published_then_participants_read_it_and_later_modules_wait_for_the_next_publication()
    {
        var curriculum = await fixture.SeedProgramme(modules: 12, publish: false);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        Assert.False((await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("isProgrammePublished").GetBoolean());
        using var administrator = await fixture.Administrator();
        var response = await ApiFixture.Post(administrator, $"/administration/curricula/{curriculum}/publication", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var publication = await Body(response);
        Assert.Equal("Published", publication.GetProperty("state").GetString()); Assert.Equal(12, publication.GetProperty("publishedModuleCount").GetInt32());
        Assert.Equal(fixture.Clock.UtcNow, publication.GetProperty("publishedAt").GetDateTimeOffset());
        var path = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.True(path.GetProperty("isProgrammePublished").GetBoolean()); Assert.Equal(12, path.GetProperty("modules").GetArrayLength()); Assert.Equal(12, path.GetProperty("remaining").GetInt32());
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            Assert.All(await db.Modules.ToListAsync(), m => Assert.Equal(PublicationState.Published, m.State));
            Assert.Equal("CurriculumPublished", (await db.CurriculumAudits.SingleAsync(x => x.TargetId == curriculum)).Action);
            // A thirteenth module, and a revision to the first section, made as the authoring screens will make them.
            db.Modules.Add(new CurriculumModule { CurriculumId = curriculum, Ordinal = 13, Title = "Handing the practice on", Summary = "Leave it in good hands.", EffortEstimate = "30 minutes", PracticeSteps = ["Hand it on"], Sections = [new() { Ordinal = 1, Title = "Hand it on", Reading = "Leave the record with the practice.", CreatedAt = fixture.Clock.UtcNow }] });
            var first = await db.Sections.SingleAsync(x => x.Ordinal == 1 && db.Modules.Any(m => m.Id == x.ModuleId && m.Ordinal == 1));
            first.Reading = "Revised reading, visible at once.";
            await db.SaveChangesAsync();
        }
        var unchanged = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(12, unchanged.GetProperty("modules").GetArrayLength()); Assert.Equal(12, unchanged.GetProperty("remaining").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync("/modules/13")).StatusCode);
        Assert.Equal("Revised reading, visible at once.", (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("sections")[0].GetProperty("reading").GetString());
        var draft = await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}");
        Assert.Equal(1, draft.GetProperty("readiness").GetProperty("unpublishedModuleCount").GetInt32()); Assert.Equal(1, draft.GetProperty("readiness").GetProperty("activeCohortCount").GetInt32());
        Assert.Equal(0, draft.GetProperty("readiness").GetProperty("participantsMovedBack").GetInt32());
        fixture.Clock.UtcNow += TimeSpan.FromHours(1);
        Assert.Equal(13, (await Body(await ApiFixture.Post(administrator, $"/administration/curricula/{curriculum}/publication", new { }))).GetProperty("publishedModuleCount").GetInt32());
        var grown = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(13, grown.GetProperty("modules").GetArrayLength()); Assert.Equal(13, grown.GetProperty("remaining").GetInt32());
        // The thirteenth module now exists for the participant: it is withheld by their progress, not by publication.
        var thirteenth = await participant.GetAsync("/modules/13");
        Assert.Equal(HttpStatusCode.Conflict, thirteenth.StatusCode);
        Assert.Equal("Complete the current module to unlock this module.", (await thirteenth.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        Assert.Equal(fixture.Clock.UtcNow, (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("publishedAt").GetDateTimeOffset());
    }

    // Traces to: L2-055 AC5. Given modules one, three and four complete and a draft module at position two,
    // when the readiness is read and the module is published, then the draft names the move back before the
    // act, the participant's current module becomes two, and the completed modules stay complete.
    [Fact]
    public async Task Given_modules_one_to_four_complete_when_a_module_is_published_at_position_two_then_it_becomes_current_and_completed_modules_stay_complete()
    {
        var curriculum = await fixture.SeedProgramme(modules: 4);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            (await db.Modules.SingleAsync(x => x.CurriculumId == curriculum && x.Ordinal == 2)).State = PublicationState.Draft;
            await db.SaveChangesAsync();
        }
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        foreach (var ordinal in new[] { 1, 3, 4 })
        {
            var module = await participant.GetFromJsonAsync<JsonElement>($"/modules/{ordinal}");
            foreach (var section in module.GetProperty("sections").EnumerateArray())
                Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, $"/sections/{section.GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
        }
        var complete = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(3, complete.GetProperty("completed").GetInt32()); Assert.Equal(JsonValueKind.Null, complete.GetProperty("currentOrdinal").ValueKind);
        using var administrator = await fixture.Administrator();
        var readiness = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("readiness");
        Assert.Equal(1, readiness.GetProperty("participantsMovedBack").GetInt32()); Assert.Equal(2, readiness.GetProperty("movedBackToOrdinal").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(administrator, $"/administration/curricula/{curriculum}/publication", new { })).StatusCode);
        var moved = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(2, moved.GetProperty("currentOrdinal").GetInt32()); Assert.Equal(3, moved.GetProperty("completed").GetInt32()); Assert.Equal(4, moved.GetProperty("modules").GetArrayLength());
        Assert.Equal(["Complete", "Current", "Complete", "Complete"], moved.GetProperty("modules").EnumerateArray().Select(m => m.GetProperty("state").GetString()).ToList());
        Assert.Equal(2, (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("ordinal").GetInt32());
    }
}
