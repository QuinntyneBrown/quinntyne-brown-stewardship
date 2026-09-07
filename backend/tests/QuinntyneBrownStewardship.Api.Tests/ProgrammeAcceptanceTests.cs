using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class ProgrammeAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private Guid curriculumId;
    public async Task InitializeAsync()
    {
        await fixture.Reset();
        curriculumId = await fixture.SeedProgramme();
    }
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid Slot, Guid OtherSlot, Guid Enrollment)> BookingSetup()
    {
        var enrollment = await fixture.Enroll(await fixture.SeedCohort(curriculumId));
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var mentor = await db.Participants.SingleAsync(x => x.EmailAddress == "mentor@example.com");
        var slot = new QuinntyneBrownStewardship.Domain.Scheduling.AvailabilitySlot { MentorId = mentor.Id, StartsAt = fixture.Clock.UtcNow.AddDays(3) };
        var other = new QuinntyneBrownStewardship.Domain.Scheduling.AvailabilitySlot { MentorId = mentor.Id, StartsAt = fixture.Clock.UtcNow.AddDays(4) };
        db.Availability.AddRange(slot, other);
        await db.SaveChangesAsync();
        return (slot.Id, other.Id, enrollment);
    }

    private static Task<HttpResponseMessage> Change(HttpClient client, HttpMethod method, string path, object body) => ApiFixture.Send(client, method, path, body);

    // Traces to: L2-018–024, L2-040. Given a booking, when changed then the
    // prior slot is released; exactly 24 hours is refused and cancellation is audited.
    [Fact]
    public async Task Given_a_booking_when_changed_then_limits_release_and_audit_are_atomic()
    {
        var setup = await BookingSetup();
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        var created = await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var booking = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = booking.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await ApiFixture.Post(client, "/sessions", new { SlotId = setup.OtherSlot })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Change(client, HttpMethod.Put, $"/sessions/{id}/slot", new { SlotId = setup.OtherSlot })).StatusCode);
        fixture.Clock.UtcNow += TimeSpan.FromDays(3);
        Assert.Equal(HttpStatusCode.Conflict, (await Change(client, HttpMethod.Delete, $"/sessions/{id}", new { })).StatusCode);
        fixture.Clock.UtcNow -= TimeSpan.FromMinutes(1);
        Assert.Equal(HttpStatusCode.NoContent, (await Change(client, HttpMethod.Delete, $"/sessions/{id}", new { })).StatusCode);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(3, await db.BookingAudits.CountAsync());
        Assert.All(await db.BookingAudits.ToListAsync(), audit => Assert.NotEmpty(audit.CorrelationId));
        Assert.Equal(0, await db.Bookings.CountAsync(x => x.CancelledAt == null));
    }

    // Traces to: L2-024. Given two participants, when both claim one slot,
    // then only one succeeds and the losing participant has no booking.
    [Fact]
    public async Task Given_competing_participants_when_confirmed_together_then_only_one_claims_the_slot()
    {
        var setup = await BookingSetup();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var owner = await db.Participants.SingleAsync(x => x.EmailAddress == ApiFixture.Email);
        var other = new QuinntyneBrownStewardship.Domain.Access.Participant { EmailAddress = "other@example.com", NormalizedEmail = "OTHER@EXAMPLE.COM", PasswordHash = owner.PasswordHash };
        db.Participants.Add(other);
        db.Enrollments.Add(new() { ParticipantId = other.Id, CohortId = (await db.Enrollments.SingleAsync()).CohortId });
        await db.SaveChangesAsync();
        using var first = fixture.Browser(); using var second = fixture.Browser();
        await ApiFixture.SignIn(first);
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(second, "/authentication/sign-in", new { EmailAddress = other.EmailAddress, ApiFixture.Password })).StatusCode);
        var responses = await Task.WhenAll(ApiFixture.Post(first, "/sessions", new { SlotId = setup.Slot }), ApiFixture.Post(second, "/sessions", new { SlotId = setup.Slot }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await db.Bookings.CountAsync());
        Assert.Equal(1, await db.BookingAudits.CountAsync());
    }

    // Traces to: L2-025–028, L2-036. Given a note, when revised, then text
    // remains literal, stale writes conflict, and only owner and assigned mentor read it.
    [Fact]
    public async Task Given_a_note_when_revised_then_privacy_validation_and_revision_are_preserved()
    {
        await BookingSetup();
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        var module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        var moduleId = module.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await ApiFixture.Post(client, "/notes", new { ModuleId = moduleId, Body = " " })).StatusCode);
        var created = await ApiFixture.Post(client, "/notes", new { ModuleId = moduleId, Body = "<script>alert('literal')</script>" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var note = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = note.GetProperty("id").GetGuid(); var revision = note.GetProperty("revision").GetGuid();
        fixture.Clock.UtcNow += TimeSpan.FromMinutes(1);
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, "/notes", new { Id = id, ModuleId = moduleId, Revision = revision, Body = "Revised reflection" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ApiFixture.Post(client, "/notes", new { Id = id, ModuleId = moduleId, Revision = revision, Body = "Stale reflection" })).StatusCode);
        using var mentor = fixture.Browser();
        await ApiFixture.Post(mentor, "/authentication/sign-in", new { EmailAddress = "mentor@example.com", ApiFixture.Password });
        var visible = await mentor.GetFromJsonAsync<JsonElement>($"/notes/{id}");
        Assert.Equal("Revised reflection", visible.GetProperty("body").GetString());
        Assert.False(visible.GetProperty("canEdit").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(mentor, "/notes", new { Id = id, ModuleId = moduleId, Revision = revision, Body = "Mentor edit" })).StatusCode);
    }

    // Traces to: L2-008–016. Given an enrolled participant, when a section is
    // completed repeatedly, then progress advances once and locked modules refuse access.
    [Fact]
    public async Task Given_enrollment_when_learning_then_progress_and_access_follow_section_records()
    {
        await fixture.Enroll(await fixture.SeedCohort(curriculumId, start: DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime), mentorEmail: null));
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        var response = await client.GetAsync("/curriculum");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var curriculum = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12, curriculum.GetProperty("modules").GetArrayLength());
        Assert.Equal(1, curriculum.GetProperty("currentOrdinal").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync("/modules/2")).StatusCode);
        var module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        var section = module.GetProperty("sections")[0].GetProperty("id").GetGuid();
        for (var repeat = 0; repeat < 2; repeat++)
            Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, $"/sections/{section}/completion", new { })).StatusCode);
        module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(1, module.GetProperty("completedSections").GetInt32());
        Assert.Equal(module.GetProperty("sections")[1].GetProperty("id").GetGuid(), module.GetProperty("resumeSectionId").GetGuid());
    }

    // Traces to: L2-004, L2-035. Every programme endpoint requires authentication.
    // Traces to: L2-008–016. Derived completion stays consistent through the entire programme.
    [Fact]
    public async Task Given_twelve_modules_when_every_section_is_completed_then_all_figures_agree()
    {
        await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        for (var ordinal = 1; ordinal <= 12; ordinal++)
        {
            var module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
            Assert.Equal(ordinal, module.GetProperty("ordinal").GetInt32());
            if (ordinal == 1)
                Assert.Equal(HttpStatusCode.Conflict, (await ApiFixture.Post(client, $"/sections/{module.GetProperty("sections")[1].GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
            foreach (var section in module.GetProperty("sections").EnumerateArray())
                Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, $"/sections/{section.GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
            var path = await client.GetFromJsonAsync<JsonElement>("/curriculum");
            Assert.Equal(ordinal, path.GetProperty("completed").GetInt32());
            Assert.Equal(12 - ordinal, path.GetProperty("remaining").GetInt32());
            if (ordinal == 2) Assert.Equal(17, path.GetProperty("percent").GetInt32());
        }
        var completed = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(JsonValueKind.Null, completed.GetProperty("currentOrdinal").ValueKind);
        Assert.Equal(100, completed.GetProperty("percent").GetInt32());
        Assert.All(completed.GetProperty("modules").EnumerateArray(), m => Assert.Equal("Complete", m.GetProperty("state").GetString()));
        var last = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(last.GetProperty("sections")[4].GetProperty("id").GetGuid(), last.GetProperty("resumeSectionId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/modules/1")).StatusCode);
    }

    // Traces to: L2-019. Two devices belonging to one participant cannot book twice.
    [Fact]
    public async Task Given_one_participant_on_two_devices_when_different_slots_are_claimed_then_one_future_booking_remains()
    {
        var setup = await BookingSetup(); using var first = fixture.Browser(); using var second = fixture.Browser();
        await ApiFixture.SignIn(first); await ApiFixture.SignIn(second);
        var responses = await Task.WhenAll(ApiFixture.Post(first, "/sessions", new { SlotId = setup.Slot }), ApiFixture.Post(second, "/sessions", new { SlotId = setup.OtherSlot }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Bookings.CountAsync());
    }

    // Traces to: L2-022–023, L2-027. History resolves the module at session start,
    // and preparation returns the answer saved against the corresponding prompt.
    [Fact]
    public async Task Given_progress_and_a_prompt_answer_when_a_session_passes_then_history_keeps_its_context()
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var created = await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot });
        var sessionId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        await ApiFixture.Post(client, "/notes", new { ModuleId = module.GetProperty("id").GetGuid(), PromptId = module.GetProperty("preparationPrompts")[0].GetProperty("id").GetGuid(), Body = "I listened before choosing." });
        var preparation = await client.GetFromJsonAsync<JsonElement>($"/sessions/{sessionId}/preparation");
        Assert.Equal("I listened before choosing.", preparation.GetProperty("prompts")[0].GetProperty("answer").GetProperty("body").GetString());
        fixture.Clock.UtcNow += TimeSpan.FromDays(4);
        foreach (var section in module.GetProperty("sections").EnumerateArray()) await ApiFixture.Post(client, $"/sections/{section.GetProperty("id").GetGuid()}/completion", new { });
        var history = await client.GetFromJsonAsync<JsonElement>("/sessions/history");
        Assert.Equal(1, history.GetProperty("sessions")[0].GetProperty("moduleOrdinal").GetInt32());
        Assert.Equal(1, history.GetProperty("bookedCount").GetInt32());
        Assert.Equal(6, history.GetProperty("allowance").GetInt32());
        var current = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(2, current.GetProperty("currentOrdinal").GetInt32());
    }

    // Traces to: L2-006, L2-023. Ended cohorts and exhausted allowances refuse booking.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Given_an_ended_or_exhausted_cohort_when_booking_then_the_reason_is_explicit(bool ended)
    {
        var setup = await BookingSetup();
        using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var enrollment = await db.Enrollments.Include(x => x.Cohort).SingleAsync();
        if (ended) enrollment.Cohort.StartDate = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-100);
        else for (var i = 0; i < 6; i++) db.Bookings.Add(new() { EnrollmentId = setup.Enrollment, CreatedAt = fixture.Clock.UtcNow.AddDays(-20), Slot = new() { MentorId = enrollment.Cohort.MentorId!.Value, StartsAt = fixture.Clock.UtcNow.AddDays(-i - 1) } });
        await db.SaveChangesAsync(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var availability = await client.GetFromJsonAsync<JsonElement>("/sessions/availability");
        Assert.Contains(ended ? "ended" : "All 6 sessions", availability.GetProperty("bookingReason").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot })).StatusCode);
    }

    // Traces to: L2-028, L2-035–036. Foreign identities and invalid text never mutate notes.
    [Fact]
    public async Task Given_foreign_identities_or_invalid_notes_when_submitted_then_content_is_protected()
    {
        await BookingSetup(); using var owner = fixture.Browser(); await ApiFixture.SignIn(owner);
        var module = await owner.GetFromJsonAsync<JsonElement>("/modules/current"); var moduleId = module.GetProperty("id").GetGuid();
        var response = await ApiFixture.Post(owner, "/notes", new { ModuleId = moduleId, Body = "Private reflection" });
        var note = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = note.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await ApiFixture.Post(owner, "/notes", new { ModuleId = moduleId, Body = new string('x', 10001) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ApiFixture.Post(owner, "/notes", new { ModuleId = moduleId, SessionId = Guid.NewGuid(), Body = "Two attachments" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ApiFixture.Post(owner, "/notes", new { Body = "No attachment" })).StatusCode);
        foreach (var mentor in new[] { false, true })
        {
            using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            var passwordHash = (await db.Participants.SingleAsync(x => x.EmailAddress == ApiFixture.Email)).PasswordHash;
            var email = mentor ? "foreign-mentor@example.com" : "foreign@example.com";
            var person = new QuinntyneBrownStewardship.Domain.Access.Participant { EmailAddress = email, NormalizedEmail = email.ToUpperInvariant(), PasswordHash = passwordHash, IsMentor = mentor };
            db.Participants.Add(person); await db.SaveChangesAsync();
            using var foreign = fixture.Browser(); await ApiFixture.Post(foreign, "/authentication/sign-in", new { EmailAddress = email, ApiFixture.Password });
            Assert.Equal(HttpStatusCode.NotFound, (await foreign.GetAsync($"/notes/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(foreign, "/notes", new { Id = id, ModuleId = moduleId, Revision = note.GetProperty("revision").GetGuid(), Body = "Not authorized" })).StatusCode);
        }
        Assert.Equal("Private reflection", (await owner.GetFromJsonAsync<JsonElement>($"/notes/{id}")).GetProperty("body").GetString());
    }

    // Traces to: L2-020 AC2–4. Given a held session at the stated cutoff,
    // when rescheduling, then only a start more than 24 hours away can move.
    [Theory]
    [InlineData(1441, HttpStatusCode.OK)]
    [InlineData(1440, HttpStatusCode.Conflict)]
    [InlineData(1380, HttpStatusCode.Conflict)]
    public async Task Given_a_session_at_the_change_cutoff_when_rescheduled_then_the_window_is_exact(int minutes, HttpStatusCode expected)
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var response = await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot });
        var booking = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = booking.GetProperty("id").GetGuid();
        fixture.Clock.UtcNow = booking.GetProperty("startsAt").GetDateTimeOffset().AddMinutes(-minutes);
        Assert.Equal(expected, (await Change(client, HttpMethod.Put, $"/sessions/{id}/slot", new { SlotId = setup.OtherSlot })).StatusCode);
        var current = await client.GetFromJsonAsync<JsonElement>($"/sessions/{id}");
        Assert.Equal(expected == HttpStatusCode.OK ? setup.OtherSlot : setup.Slot, current.GetProperty("slotId").GetGuid());
        if (expected != HttpStatusCode.OK)
        {
            Assert.False(current.GetProperty("canChange").GetBoolean());
            Assert.Contains("24 hours", current.GetProperty("changeReason").GetString());
        }
    }

    // Traces to: L2-005 AC2, L2-035 AC1–4, L2-036 AC4. Given participants in
    // different curricula, when a caller supplies foreign identifiers, then no
    // foreign reading, completion, booking or notes can be read or changed.
    [Fact]
    public async Task Given_another_cohort_when_identifiers_are_forged_then_every_programme_resource_stays_private()
    {
        var setup = await BookingSetup(); using var owner = fixture.Browser(); await ApiFixture.SignIn(owner);
        var bookingResponse = await ApiFixture.Post(owner, "/sessions", new { SlotId = setup.Slot });
        var bookingId = (await bookingResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var participant = await db.Participants.SingleAsync(x => x.EmailAddress == ApiFixture.Email);
        var foreign = new QuinntyneBrownStewardship.Domain.Access.Participant { EmailAddress = "separate@example.com", NormalizedEmail = "SEPARATE@EXAMPLE.COM", PasswordHash = participant.PasswordHash };
        db.Participants.Add(foreign);
        var separate = new QuinntyneBrownStewardship.Domain.Learning.Curriculum { Key = "separate", Title = "Separate programme", State = QuinntyneBrownStewardship.Domain.Learning.PublicationState.Published, CreatedAt = fixture.Clock.UtcNow, PublishedAt = fixture.Clock.UtcNow };
        var foreignModule = new QuinntyneBrownStewardship.Domain.Learning.CurriculumModule
        {
            CurriculumId = separate.Id, State = QuinntyneBrownStewardship.Domain.Learning.PublicationState.Published, Ordinal = 1, Title = "Another cohort's reading", Summary = "Private curriculum", EffortEstimate = "30 minutes", PracticeSteps = ["Listen"],
            Sections = [new() { Ordinal = 1, Title = "Other section", Reading = "Other cohort content", CreatedAt = fixture.Clock.UtcNow }]
        };
        db.Curricula.Add(separate); db.Modules.Add(foreignModule);
        db.Enrollments.Add(new() { ParticipantId = foreign.Id, Cohort = new() { CurriculumId = separate.Id, DurationWeeks = 12, SessionCadenceWeeks = 2, MentorName = "Other mentor", StartDate = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime) } });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(owner, $"/sections/{foreignModule.Sections[0].Id}/completion", new { ParticipantId = foreign.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(owner, "/notes", new { ModuleId = foreignModule.Id, Body = "Foreign note" })).StatusCode);
        var curriculum = await owner.GetFromJsonAsync<JsonElement>($"/curriculum?participantId={foreign.Id}");
        Assert.Equal("Module 1", curriculum.GetProperty("modules")[0].GetProperty("title").GetString());
        using var other = fixture.Browser(); await ApiFixture.Post(other, "/authentication/sign-in", new { EmailAddress = foreign.EmailAddress, ApiFixture.Password });
        Assert.Equal("Another cohort's reading", (await other.GetFromJsonAsync<JsonElement>("/modules/1")).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/sessions/{bookingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/sessions/{bookingId}/preparation")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Change(other, HttpMethod.Put, $"/sessions/{bookingId}/slot", new { SlotId = setup.OtherSlot, ParticipantId = participant.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Change(other, HttpMethod.Delete, $"/sessions/{bookingId}", new { })).StatusCode);
        Assert.Equal(setup.Slot, (await owner.GetFromJsonAsync<JsonElement>($"/sessions/{bookingId}")).GetProperty("slotId").GetGuid());
        Assert.Empty(await db.Completions.ToListAsync()); Assert.Empty(await db.Notes.ToListAsync());
    }

    // Traces to: L2-017–018, L2-021, L2-023, L2-027. Given open mentor times,
    // when booked and cancelled, then slot state, next-session details and the
    // derived allowance agree; a module without prompts has no empty prompt list.
    [Fact]
    public async Task Given_published_times_when_booking_then_availability_curriculum_and_preparation_agree()
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        await db.Prompts.ExecuteDeleteAsync();
        var slot = await db.Availability.SingleAsync(x => x.Id == setup.Slot);
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(slot.StartsAt, "America/Toronto").DateTime);
        var path = $"/sessions/availability?day={day:yyyy-MM-dd}";
        var available = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(7, available.GetProperty("days").GetArrayLength());
        Assert.Equal("Open", available.GetProperty("slots")[0].GetProperty("state").GetString());
        var created = await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot });
        var booking = await created.Content.ReadFromJsonAsync<JsonElement>(); var id = booking.GetProperty("id").GetGuid();
        var curriculum = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        foreach (var field in new[] { "startsAt", "durationMinutes", "mentorName" }) Assert.Equal(booking.GetProperty(field).ToString(), curriculum.GetProperty("nextSession").GetProperty(field).ToString());
        var preparation = await client.GetFromJsonAsync<JsonElement>($"/sessions/{id}/preparation");
        Assert.Empty(preparation.GetProperty("prompts").EnumerateArray());
        available = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("Taken", available.GetProperty("slots")[0].GetProperty("state").GetString());
        Assert.Equal(1, available.GetProperty("bookedCount").GetInt32()); Assert.Equal(6, available.GetProperty("allowance").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await Change(client, HttpMethod.Delete, $"/sessions/{id}", new { })).StatusCode);
        available = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("Open", available.GetProperty("slots")[0].GetProperty("state").GetString());
        Assert.Equal(0, available.GetProperty("bookedCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, available.GetProperty("nextSession").ValueKind);
    }

    // Traces to: L2-025–028, L2-039. Given many long notes, when traversing
    // the notes destination or an attachment, then pages stay bounded and every
    // complete body is retrievable once, newest first, including timestamp ties.
    [Fact]
    public async Task Given_many_long_notes_when_read_in_pages_then_all_text_remains_reachable()
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var module = await client.GetFromJsonAsync<JsonElement>("/modules/current"); var moduleId = module.GetProperty("id").GetGuid();
        var ids = new HashSet<Guid>();
        for (var i = 0; i < 24; i++)
        {
            var response = await ApiFixture.Post(client, "/notes", new { ModuleId = moduleId, Body = $"Note {i:00}: " + new string((char)('a' + i), 9900) });
            response.EnsureSuccessStatusCode(); ids.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        }
        var seen = new HashSet<Guid>(); string? cursor = null; var pages = 0;
        do
        {
            var result = await client.GetFromJsonAsync<JsonElement>($"/notes?moduleId={moduleId}" + (cursor == null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
            var notes = result.GetProperty("notes");
            Assert.InRange(notes.GetArrayLength(), 1, 2);
            foreach (var note in notes.EnumerateArray())
            {
                Assert.True(seen.Add(note.GetProperty("id").GetGuid()));
                Assert.Equal(9909, note.GetProperty("body").GetString()!.Length);
                Assert.Equal(moduleId, note.GetProperty("moduleId").GetGuid());
            }
            cursor = result.GetProperty("nextCursor").GetString();
            Assert.True(++pages <= 24);
        } while (cursor != null);
        Assert.True(ids.SetEquals(seen));
        module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.InRange(module.GetProperty("notes").GetArrayLength(), 1, 2);
        Assert.NotNull(module.GetProperty("notesCursor").GetString());
        var malformed = await client.GetAsync("/notes?cursor=invalid");
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Contains("cursor", await malformed.Content.ReadAsStringAsync());
    }

    // Traces to: L2-025–028. Given full-length Unicode notes on a session,
    // when reading each page, then no body is shortened and the attachment stays scoped.
    [Fact]
    public async Task Given_long_unicode_session_notes_when_paged_then_every_body_remains_whole()
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        using var created = await ApiFixture.Post(client, "/sessions", new { SlotId = setup.Slot });
        created.EnsureSuccessStatusCode();
        var sessionId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var body = new string('\u754c', 10000);
        for (var i = 0; i < 3; i++)
        {
            using var saved = await ApiFixture.Post(client, "/notes", new { SessionId = sessionId, Body = body });
            saved.EnsureSuccessStatusCode();
        }
        var preparation = await client.GetFromJsonAsync<JsonElement>($"/sessions/{sessionId}/preparation");
        Assert.Single(preparation.GetProperty("notes").EnumerateArray());
        var note = preparation.GetProperty("notes")[0];
        Assert.Equal(body, note.GetProperty("body").GetString());
        var seen = new HashSet<Guid> { note.GetProperty("id").GetGuid() };
        var cursor = preparation.GetProperty("notesCursor").GetString();
        while (cursor != null)
        {
            var page = await client.GetFromJsonAsync<JsonElement>($"/notes?sessionId={sessionId}&generalOnly=true&cursor={Uri.EscapeDataString(cursor)}");
            Assert.Single(page.GetProperty("notes").EnumerateArray());
            note = page.GetProperty("notes")[0];
            Assert.Equal(body, note.GetProperty("body").GetString());
            Assert.Equal(sessionId, note.GetProperty("sessionId").GetGuid());
            Assert.True(seen.Add(note.GetProperty("id").GetGuid()));
            Assert.InRange(seen.Count, 1, 3);
            cursor = page.GetProperty("nextCursor").GetString();
        }
        Assert.Equal(3, seen.Count);
    }

    // Traces to: L2-006, L2-017. Local calendar weeks and DST labels use the cohort zone.
    [Fact]
    public async Task Given_a_cohort_zone_when_utc_has_advanced_a_day_then_the_local_week_is_used()
    {
        await BookingSetup(); fixture.Clock.UtcNow = new DateTimeOffset(2026, 9, 8, 1, 0, 0, TimeSpan.Zero);
        using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var cohort = await db.Cohorts.SingleAsync(); cohort.StartDate = new DateOnly(2026, 9, 1); await db.SaveChangesAsync();
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var response = await client.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(1, response.GetProperty("cohort").GetProperty("currentWeek").GetInt32());
        Assert.Equal("America/Toronto", response.GetProperty("cohort").GetProperty("timeZone").GetString());
    }

    // Traces to: L2-036 AC1. Given a malformed required route identifier, when
    // a programme endpoint is called, then it names the invalid field with 400
    // instead of serving the application document, and no state changes.
    [Fact]
    public async Task Given_malformed_route_identifiers_when_requested_then_the_invalid_field_is_rejected()
    {
        var setup = await BookingSetup(); using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        foreach (var (method, path, field) in new[]
        {
            (HttpMethod.Get, "/modules/not-a-number", "ordinal"),
            (HttpMethod.Get, "/notes/not-a-guid", "id"),
            (HttpMethod.Get, "/sessions/not-a-guid", "id"),
            (HttpMethod.Get, "/sessions/not-a-guid/preparation", "id"),
            (HttpMethod.Post, "/sections/not-a-guid/completion", "id"),
            (HttpMethod.Put, "/sessions/not-a-guid/slot", "id"),
            (HttpMethod.Delete, "/sessions/not-a-guid", "id")
        })
        {
            using var response = method == HttpMethod.Get ? await client.GetAsync(path) : await Change(client, method, path, new { SlotId = setup.Slot });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        }
        using var scope = fixture.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Empty(await db.Completions.ToListAsync()); Assert.Empty(await db.Bookings.ToListAsync()); Assert.Empty(await db.Notes.ToListAsync());
    }

    // Traces to: L2-004, L2-035. Every programme endpoint requires authentication.
    [Theory]
    [InlineData("/curriculum")]
    [InlineData("/modules/current")]
    [InlineData("/sessions/availability")]
    [InlineData("/sessions/history")]
    [InlineData("/notes")]
    public async Task Given_no_identity_when_programme_is_requested_then_access_is_refused(string path)
    {
        using var client = fixture.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }
}
