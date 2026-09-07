using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class AuthoringAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;
    private static Task<HttpResponseMessage> Put(HttpClient client, string url, object body) => ApiFixture.Send(client, HttpMethod.Put, url, body);
    private static Task<HttpResponseMessage> Delete(HttpClient client, string url) => ApiFixture.Send(client, HttpMethod.Delete, url, null);
    private static async Task<JsonElement> Body(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();
    private async Task<int> ProgrammeCount(HttpClient administrator) => (await administrator.GetFromJsonAsync<JsonElement>("/administration/curricula")).GetProperty("programmes").GetArrayLength();

    // Traces to: L2-044 AC1–AC2, L2-051 AC1–AC2, L2-061 AC4. Given two programmes, when a key is reused, then the
    // creation is refused naming the key and nothing is stored; two simultaneous creations of one key leave exactly
    // one; an empty title is refused naming the field; and every creation is audited with its actor and request.
    [Fact]
    public async Task Given_two_programmes_when_a_key_is_reused_then_the_creation_is_refused_and_nothing_is_stored()
    {
        using var administrator = await fixture.Administrator();
        var created = await ApiFixture.Post(administrator, "/administration/curricula", new { key = "eight", title = "Eight weeks" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await Body(created)).GetProperty("id").GetGuid();
        var listed = Assert.Single((await administrator.GetFromJsonAsync<JsonElement>("/administration/curricula")).GetProperty("programmes").EnumerateArray());
        Assert.Equal(id, listed.GetProperty("id").GetGuid()); Assert.Equal("Draft", listed.GetProperty("state").GetString()); Assert.Equal(0, listed.GetProperty("moduleCount").GetInt32());
        var refused = await ApiFixture.Post(administrator, "/administration/curricula", new { key = "eight", title = "Eight weeks again" });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("The key eight is already used by another programme. Choose a different key.", (await Body(refused)).GetProperty("title").GetString());
        Assert.Equal(1, await ProgrammeCount(administrator));
        using var second = await fixture.Administrator();
        var race = await Task.WhenAll(ApiFixture.Post(administrator, "/administration/curricula", new { key = "nine", title = "Nine" }), ApiFixture.Post(second, "/administration/curricula", new { key = "nine", title = "Nine again" }));
        Assert.Equal(1, race.Count(x => x.StatusCode == HttpStatusCode.OK)); Assert.Equal(1, race.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(2, await ProgrammeCount(administrator));
        var empty = await ApiFixture.Post(administrator, "/administration/curricula", new { key = "ten", title = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("A title is required.", (await Body(empty)).GetProperty("errors").GetProperty("title")[0].GetString());
        Assert.Equal(2, await ProgrammeCount(administrator));
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var actor = await db.Participants.SingleAsync(x => x.EmailAddress == ApiFixture.AdministratorEmail);
        var audit = await db.CurriculumAudits.SingleAsync(x => x.TargetId == id);
        Assert.Equal("CurriculumCreated", audit.Action); Assert.Equal(actor.Id, audit.ActorId); Assert.NotEmpty(audit.CorrelationId); Assert.Equal(fixture.Clock.UtcNow, audit.At);
        Assert.Equal(2, await db.CurriculumAudits.CountAsync());
    }

    // Traces to: L2-044 AC4, L2-051 AC5, L2-052 AC1. Given a programme, when it is renamed, then the new title is
    // read back, its draft state is untouched even when the body claims otherwise, and the rename is audited.
    [Fact]
    public async Task Given_a_programme_when_it_is_renamed_then_the_title_changes_and_nothing_else_does()
    {
        var id = await fixture.SeedProgramme(modules: 2, key: "spare", publish: false);
        using var administrator = await fixture.Administrator();
        var renamed = await Put(administrator, $"/administration/curricula/{id}", new { title = "Spare, revised", state = "Published", isAdministrator = true });
        Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);
        var draft = await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{id}");
        Assert.Equal("Spare, revised", draft.GetProperty("title").GetString()); Assert.Equal("Draft", draft.GetProperty("state").GetString());
        Assert.Equal("spare", draft.GetProperty("key").GetString()); Assert.Equal(2, draft.GetProperty("modules").GetArrayLength());
        Assert.True(draft.GetProperty("canChangeKey").GetBoolean()); Assert.True(draft.GetProperty("canRemove").GetBoolean());
        Assert.True(draft.GetProperty("readiness").GetProperty("canPublish").GetBoolean()); Assert.Equal(120, draft.GetProperty("limits").GetProperty("title").GetInt32());
        var module = draft.GetProperty("modules")[0];
        Assert.Equal(1, module.GetProperty("ordinal").GetInt32()); Assert.Equal(5, module.GetProperty("sectionCount").GetInt32()); Assert.Equal(3, module.GetProperty("stepCount").GetInt32()); Assert.Equal(1, module.GetProperty("promptCount").GetInt32());
        using var scope = fixture.Services.CreateScope();
        Assert.Equal("CurriculumRenamed", (await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.SingleAsync(x => x.TargetId == id)).Action);
    }

    // Traces to: L2-044 AC5–AC9, L2-061 AC4. Given a programme no cohort follows and one a cohort follows, when each
    // is re-keyed and removed, then the free one takes a new key, refuses a taken one, and is removed with its
    // modules, sections and prompts, while the followed one refuses both and names the cohort.
    [Fact]
    public async Task Given_a_followed_and_an_unfollowed_programme_when_rekeyed_and_removed_then_only_the_unfollowed_one_changes()
    {
        var free = await fixture.SeedProgramme(modules: 2, key: "spare", publish: false);
        var followed = await fixture.SeedProgramme(modules: 3, key: "starter");
        await fixture.SeedCohort(followed);
        using var administrator = await fixture.Administrator();
        var taken = await Put(administrator, $"/administration/curricula/{free}/key", new { key = "starter" });
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode); Assert.Contains("starter", (await Body(taken)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Put(administrator, $"/administration/curricula/{free}/key", new { key = "spare-2" })).StatusCode);
        Assert.Equal("spare-2", (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{free}")).GetProperty("key").GetString());
        // The new key is how a cohort names the programme from now on: it resolves, and only publication stands in the way.
        using (var naming = fixture.Services.CreateScope())
        {
            var byNewKey = await Assert.ThrowsAsync<QuinntyneBrownStewardship.Application.Common.ProgrammeException>(() => naming.ServiceProvider.GetRequiredService<ISender>().Send(new CreateCohortCommand(Guid.NewGuid(), DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime), "mentor@example.com", "spare-2", 12, 2)));
            Assert.Equal(409, byNewKey.StatusCode);
        }
        var locked = await Put(administrator, $"/administration/curricula/{followed}/key", new { key = "starter-2" });
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        Assert.Equal("1 cohort follows this programme. The key cannot be changed while it does.", (await Body(locked)).GetProperty("title").GetString());
        var kept = await Delete(administrator, $"/administration/curricula/{followed}");
        Assert.Equal(HttpStatusCode.Conflict, kept.StatusCode);
        Assert.Equal("1 cohort follows this programme. It cannot be removed while it does.", (await Body(kept)).GetProperty("title").GetString());
        var followedDraft = await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{followed}");
        Assert.Equal("starter", followedDraft.GetProperty("key").GetString()); Assert.False(followedDraft.GetProperty("canChangeKey").GetBoolean()); Assert.False(followedDraft.GetProperty("canRemove").GetBoolean());
        Assert.Equal(1, followedDraft.GetProperty("cohortCount").GetInt32()); Assert.Equal(1, followedDraft.GetProperty("readiness").GetProperty("cohorts").GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(administrator, $"/administration/curricula/{free}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await administrator.GetAsync($"/administration/curricula/{free}")).StatusCode);
        Assert.Equal(1, await ProgrammeCount(administrator));
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(3, await db.Modules.CountAsync()); Assert.Equal(15, await db.Sections.CountAsync()); Assert.Equal(3, await db.Prompts.CountAsync());
        Assert.Equal(["CurriculumRekeyed", "CurriculumRemoved"], (await db.CurriculumAudits.Where(x => x.TargetId == free).OrderBy(x => x.At).ThenBy(x => x.Action).Select(x => x.Action).ToListAsync()));
        var gone = await Assert.ThrowsAsync<QuinntyneBrownStewardship.Application.Common.ProgrammeException>(() => scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateCohortCommand(Guid.NewGuid(), DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime), "mentor@example.com", "spare-2", 12, 2)));
        Assert.Equal(404, gone.StatusCode);
    }

    // Traces to: L2-051 AC4, AC8–AC9, L2-061 AC2. Given the programme routes, when an unknown identifier, a body
    // failing several rules, or a write without its request token arrives, then each is refused as stated and
    // nothing changes.
    [Fact]
    public async Task Given_the_programme_routes_when_bad_requests_arrive_then_each_is_refused_and_nothing_changes()
    {
        var id = await fixture.SeedProgramme(modules: 1, key: "spare", publish: false);
        using var administrator = await fixture.Administrator();
        var unknown = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await administrator.GetAsync($"/administration/curricula/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/curricula/{unknown}", new { title = "Nobody" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/curricula/{unknown}/key", new { key = "nobody" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(administrator, $"/administration/curricula/{unknown}")).StatusCode);
        var invalid = await ApiFixture.Post(administrator, "/administration/curricula", new { key = "Not A Key", title = new string('t', 121) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var errors = (await Body(invalid)).GetProperty("errors");
        Assert.Equal("A key uses lowercase letters, digits and hyphens only.", errors.GetProperty("key")[0].GetString());
        Assert.Equal("A title may be at most 120 characters. Shorten it by 1.", errors.GetProperty("title")[0].GetString());
        var overlong = await ApiFixture.Post(administrator, "/administration/curricula", new { key = new string('k', 101), title = "" });
        var overlongErrors = (await Body(overlong)).GetProperty("errors");
        Assert.Equal("A key may be at most 100 characters. Shorten it by 1.", overlongErrors.GetProperty("key")[0].GetString());
        Assert.Equal("A title is required.", overlongErrors.GetProperty("title")[0].GetString());
        using var unsigned = await fixture.Administrator();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/administration/curricula/{id}") { Content = JsonContent.Create(new { title = "Forged" }) };
        Assert.Equal(HttpStatusCode.BadRequest, (await unsigned.SendAsync(request)).StatusCode);
        Assert.Equal("spare", (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{id}")).GetProperty("title").GetString());
        Assert.Equal(1, await ProgrammeCount(administrator));
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.CountAsync());
    }

    // Traces to: L2-054 AC1–AC2, L2-045 AC4. Given a programme with no modules and one whose module has no
    // sections, when their drafts are read, then each states why it cannot be published and names the module.
    [Fact]
    public async Task Given_incomplete_programmes_when_their_drafts_are_read_then_readiness_names_what_stops_publication()
    {
        using var administrator = await fixture.Administrator();
        var bare = (await Body(await ApiFixture.Post(administrator, "/administration/curricula", new { key = "alumni", title = "Stewardship — alumni" }))).GetProperty("id").GetGuid();
        var readiness = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{bare}")).GetProperty("readiness");
        Assert.False(readiness.GetProperty("canPublish").GetBoolean());
        Assert.Equal("This programme cannot be published. It has no modules, so a participant would have nothing to read.", readiness.GetProperty("reason").GetString());
        var id = await fixture.SeedProgramme(modules: 4, key: "foundations", publish: false);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            var third = await db.Modules.SingleAsync(x => x.CurriculumId == id && x.Ordinal == 3);
            third.Title = "Choose enough";
            await db.Sections.Where(x => x.ModuleId == third.Id).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
        }
        readiness = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{id}")).GetProperty("readiness");
        Assert.False(readiness.GetProperty("canPublish").GetBoolean());
        Assert.Equal("1 module carries no section: 03 Choose enough. Every module needs at least one before this programme can be published.", readiness.GetProperty("reason").GetString());
        Assert.Equal("03 Choose enough", Assert.Single(readiness.GetProperty("emptyModuleTitles").EnumerateArray()).GetString());
        Assert.Equal(4, readiness.GetProperty("unpublishedModuleCount").GetInt32()); Assert.Equal(0, readiness.GetProperty("activeCohortCount").GetInt32());
    }

    private async Task<JsonElement> ModuleDraft(HttpClient administrator, Guid id) => await administrator.GetFromJsonAsync<JsonElement>($"/administration/modules/{id}");
    private static object Revision(JsonElement draft, string? title = null, string? summary = null, string? effort = null, string[]? steps = null)
        => new { title = title ?? draft.GetProperty("title").GetString(), summary = summary ?? draft.GetProperty("summary").GetString(), effortEstimate = effort ?? draft.GetProperty("effortEstimate").GetString(), practiceSteps = steps ?? draft.GetProperty("practiceSteps").EnumerateArray().Select(x => x.GetString()!).ToArray(), revision = draft.GetProperty("revision").GetGuid() };
    private static async Task CompleteModule(HttpClient participant, int ordinal)
    {
        var module = await participant.GetFromJsonAsync<JsonElement>($"/modules/{ordinal}");
        foreach (var section in module.GetProperty("sections").EnumerateArray())
            Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, $"/sections/{section.GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
    }

    // Traces to: L2-047 AC1–AC4, L2-050 AC3, L2-061 AC4. Given a prompt a participant has answered, when its text is
    // revised, then the answer stays attached; an answered prompt cannot be removed; added and removed prompts keep a
    // contiguous order; and a module left with no prompts reaches the participant with none.
    [Fact]
    public async Task Given_a_prompt_a_participant_has_answered_when_its_text_is_revised_then_the_answer_stays_attached()
    {
        var curriculum = await fixture.SeedProgramme(modules: 3);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        var first = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        var moduleId = first.GetProperty("id").GetGuid(); var promptId = first.GetProperty("preparationPrompts")[0].GetProperty("id").GetGuid();
        var answer = await (await ApiFixture.Post(participant, "/notes", new { body = "My answer.", moduleId, sessionId = (Guid?)null, promptId })).Content.ReadFromJsonAsync<JsonElement>();
        using var administrator = await fixture.Administrator();
        Assert.Equal(HttpStatusCode.NoContent, (await Put(administrator, $"/administration/prompts/{promptId}", new { text = "Whose experience changed your decision, and how?" })).StatusCode);
        var prompt = (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("preparationPrompts")[0];
        Assert.Equal("Whose experience changed your decision, and how?", prompt.GetProperty("text").GetString());
        Assert.Equal(answer.GetProperty("id").GetGuid(), prompt.GetProperty("answer").GetProperty("id").GetGuid());
        var refused = await Delete(administrator, $"/administration/prompts/{promptId}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("1 participant has answered this prompt. It cannot be removed while that answer stands.", (await Body(refused)).GetProperty("title").GetString());
        var second = (await Body(await ApiFixture.Post(administrator, $"/administration/modules/{moduleId}/prompts", new { text = "What did you decide not to build?" }))).GetProperty("id").GetGuid();
        var third = (await Body(await ApiFixture.Post(administrator, $"/administration/modules/{moduleId}/prompts", new { text = "Who will you ask next?" }))).GetProperty("id").GetGuid();
        var draft = await ModuleDraft(administrator, moduleId);
        Assert.Equal([1, 2, 3], draft.GetProperty("prompts").EnumerateArray().Select(p => p.GetProperty("ordinal").GetInt32()).ToList());
        Assert.False(draft.GetProperty("prompts")[0].GetProperty("canRemove").GetBoolean()); Assert.Equal(1, draft.GetProperty("prompts")[0].GetProperty("answerCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(administrator, $"/administration/prompts/{second}")).StatusCode);
        draft = await ModuleDraft(administrator, moduleId);
        Assert.Equal([promptId, third], draft.GetProperty("prompts").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList());
        Assert.Equal([1, 2], draft.GetProperty("prompts").EnumerateArray().Select(p => p.GetProperty("ordinal").GetInt32()).ToList());
        Assert.Equal(2, (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("preparationPrompts").GetArrayLength());
        // Module two loses its only prompt; when the participant reaches it, it carries none.
        var secondModule = await ModuleDraft(administrator, (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules")[1].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(administrator, $"/administration/prompts/{secondModule.GetProperty("prompts")[0].GetProperty("id").GetGuid()}")).StatusCode);
        await CompleteModule(participant, 1);
        var reached = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(2, reached.GetProperty("ordinal").GetInt32()); Assert.Equal(0, reached.GetProperty("preparationPrompts").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/prompts/{Guid.NewGuid()}", new { text = "Nobody" })).StatusCode);
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(["PromptAdded", "PromptAdded", "PromptRemoved", "PromptRemoved", "PromptRevised"], (await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.Select(x => x.Action).ToListAsync()).Order().ToList());
    }

    // Traces to: L2-045 AC1–AC3, L2-046 AC1–AC4, L2-052 AC2, L2-061 AC4. Given a published programme, when a module is
    // added, then it takes the last position in draft and stays invisible; when a module is revised, then the participant
    // reads the revision at once, with the steps exactly as authored, or none; when a module nothing depends on is removed,
    // then the remaining modules close the gap.
    [Fact]
    public async Task Given_a_programme_when_a_module_is_added_revised_and_removed_then_the_participant_reads_each_change_that_is_published()
    {
        var curriculum = await fixture.SeedProgramme(modules: 3);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var administrator = await fixture.Administrator();
        var added = (await Body(await ApiFixture.Post(administrator, $"/administration/curricula/{curriculum}/modules", new { title = "Handing the practice on", summary = "Leave it in good hands." }))).GetProperty("id").GetGuid();
        var draft = await ModuleDraft(administrator, added);
        Assert.Equal(4, draft.GetProperty("ordinal").GetInt32()); Assert.Equal(4, draft.GetProperty("moduleCount").GetInt32()); Assert.Equal("Draft", draft.GetProperty("state").GetString());
        Assert.Equal("starter", draft.GetProperty("curriculumKey").GetString()); Assert.Equal(0, draft.GetProperty("sections").GetArrayLength()); Assert.True(draft.GetProperty("canRemove").GetBoolean());
        Assert.Equal(400, draft.GetProperty("limits").GetProperty("summary").GetInt32());
        Assert.Equal(4, (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules").GetArrayLength());
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        Assert.Equal(3, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules").GetArrayLength());
        var firstId = (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("id").GetGuid();
        var first = await ModuleDraft(administrator, firstId);
        var steps = new[] { "Audit one default.", "Compare two alternatives.", "Propose a reversible change." };
        var revised = await Put(administrator, $"/administration/modules/{firstId}", Revision(first, title: "Begin again", summary: "A revised summary.", effort: "45 minutes", steps: steps));
        Assert.Equal(HttpStatusCode.OK, revised.StatusCode);
        var revision = (await Body(revised)).GetProperty("revision").GetGuid(); Assert.NotEqual(first.GetProperty("revision").GetGuid(), revision);
        var read = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal("Begin again", read.GetProperty("title").GetString()); Assert.Equal("45 minutes", read.GetProperty("effortEstimate").GetString());
        Assert.Equal(steps, read.GetProperty("practiceSteps").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.Equal("Begin again", (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules")[0].GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Put(administrator, $"/administration/modules/{firstId}", Revision(await ModuleDraft(administrator, firstId), steps: []))).StatusCode);
        Assert.Equal(0, (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("practiceSteps").GetArrayLength());
        // Removing the third module, which nothing depends on, closes the gap; the draft fourth becomes third.
        var thirdId = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules")[2].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(administrator, $"/administration/modules/{thirdId}")).StatusCode);
        var modules = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules");
        Assert.Equal([1, 2, 3], modules.EnumerateArray().Select(m => m.GetProperty("ordinal").GetInt32()).ToList()); Assert.Equal(added, modules[2].GetProperty("id").GetGuid());
        Assert.Equal(2, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await administrator.GetAsync($"/administration/modules/{thirdId}")).StatusCode);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(10, await db.Sections.CountAsync()); Assert.Equal(2, await db.Prompts.CountAsync());
        Assert.Equal(["ModuleAdded", "ModuleRemoved", "ModuleRevised", "ModuleRevised"], (await db.CurriculumAudits.Select(x => x.Action).ToListAsync()).Order().ToList());
    }

    // Traces to: L2-065 AC1–AC4. Given two administrators holding the same module, when both save, then the second is
    // refused and the first revision stands; a reload lets the second apply their change; and two simultaneous saves
    // leave exactly one stored.
    [Fact]
    public async Task Given_two_administrators_holding_the_same_module_when_both_save_then_the_second_is_refused_until_they_reload()
    {
        var curriculum = await fixture.SeedProgramme(modules: 1, publish: false);
        using var first = await fixture.Administrator(); using var second = await fixture.Administrator();
        var moduleId = (await first.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules")[0].GetProperty("id").GetGuid();
        var opened = await ModuleDraft(first, moduleId); var openedElsewhere = await ModuleDraft(second, moduleId);
        Assert.Equal(HttpStatusCode.OK, (await Put(first, $"/administration/modules/{moduleId}", Revision(opened, title: "First to save"))).StatusCode);
        var refused = await Put(second, $"/administration/modules/{moduleId}", Revision(openedElsewhere, title: "Second to save"));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("This module changed since it was opened. Reload it to see the current content.", (await Body(refused)).GetProperty("title").GetString());
        Assert.Equal("First to save", (await ModuleDraft(second, moduleId)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Put(second, $"/administration/modules/{moduleId}", Revision(await ModuleDraft(second, moduleId), title: "Second to save, after reloading"))).StatusCode);
        Assert.Equal("Second to save, after reloading", (await ModuleDraft(first, moduleId)).GetProperty("title").GetString());
        var current = await ModuleDraft(first, moduleId);
        var race = await Task.WhenAll(Put(first, $"/administration/modules/{moduleId}", Revision(current, title: "Raced by the first")), Put(second, $"/administration/modules/{moduleId}", Revision(current, title: "Raced by the second")));
        Assert.Equal(1, race.Count(x => x.StatusCode == HttpStatusCode.OK)); Assert.Equal(1, race.Count(x => x.StatusCode == HttpStatusCode.Conflict));
    }

    // Traces to: L2-051 AC2, AC4, AC8–AC9. Given a module revision failing several rules at once, when it is saved,
    // then every rule is reported by field, with its maximum and overage, and nothing is stored.
    [Fact]
    public async Task Given_a_module_revision_failing_several_rules_when_saved_then_every_rule_is_reported_with_its_overage()
    {
        var curriculum = await fixture.SeedProgramme(modules: 1, publish: false);
        using var administrator = await fixture.Administrator();
        var moduleId = (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules")[0].GetProperty("id").GetGuid();
        var draft = await ModuleDraft(administrator, moduleId);
        var refused = await Put(administrator, $"/administration/modules/{moduleId}", Revision(draft, title: "", summary: new string('s', 442), effort: new string('e', 61), steps: ["Keep this step.", "", new string('p', 401)]));
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var errors = (await Body(refused)).GetProperty("errors");
        Assert.Equal("A title is required.", errors.GetProperty("title")[0].GetString());
        Assert.Equal("A summary may be at most 400 characters. Shorten it by 42.", errors.GetProperty("summary")[0].GetString());
        Assert.Equal("An effort estimate may be at most 60 characters. Shorten it by 1.", errors.GetProperty("effortEstimate")[0].GetString());
        Assert.Equal("A practice step is required.", errors.GetProperty("practiceSteps[1]")[0].GetString());
        Assert.Equal("A practice step may be at most 400 characters. Shorten it by 1.", errors.GetProperty("practiceSteps[2]")[0].GetString());
        Assert.False(errors.TryGetProperty("practiceSteps[0]", out _));
        var unchanged = await ModuleDraft(administrator, moduleId);
        Assert.Equal("Module 1", unchanged.GetProperty("title").GetString()); Assert.Equal(draft.GetProperty("revision").GetGuid(), unchanged.GetProperty("revision").GetGuid());
        var unknown = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await administrator.GetAsync($"/administration/modules/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/modules/{unknown}", Revision(draft))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(administrator, $"/administration/modules/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(administrator, $"/administration/modules/{unknown}/prompts", new { text = "Nobody" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(administrator, $"/administration/curricula/{unknown}/modules", new { title = "Nobody", summary = "Nowhere" })).StatusCode);
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.CountAsync());
    }

    private async Task<JsonElement> SectionDraft(HttpClient administrator, Guid id) => await administrator.GetFromJsonAsync<JsonElement>($"/administration/sections/{id}");

    // Traces to: L2-050 AC1–AC2, AC4–AC7. Given a section a participant completed, a module with a note attached, and a
    // module with an answered prompt, when their removal is attempted, then each is refused with the reason named, no
    // constraint fails, and the participant's progress is exactly as it was.
    [Fact]
    public async Task Given_a_section_a_participant_completed_when_removal_is_attempted_then_409_names_the_completion_and_progress_is_unchanged()
    {
        var curriculum = await fixture.SeedProgramme(modules: 3);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        var first = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        var moduleId = first.GetProperty("id").GetGuid(); var sectionId = first.GetProperty("sections")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, $"/sections/{sectionId}/completion", new { })).StatusCode);
        using var administrator = await fixture.Administrator();
        var refused = await Delete(administrator, $"/administration/sections/{sectionId}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("1 completion is recorded against this section. It cannot be removed while that record stands.", (await Body(refused)).GetProperty("title").GetString());
        var module = await Delete(administrator, $"/administration/modules/{moduleId}");
        Assert.Equal(HttpStatusCode.Conflict, module.StatusCode);
        Assert.Equal("1 completion is recorded against the sections of this module. It cannot be removed while those records stand.", (await Body(module)).GetProperty("title").GetString());
        var draft = await ModuleDraft(administrator, moduleId);
        Assert.False(draft.GetProperty("canRemove").GetBoolean()); Assert.Equal(1, draft.GetProperty("completionCount").GetInt32());
        Assert.False(draft.GetProperty("sections")[0].GetProperty("canRemove").GetBoolean()); Assert.Equal(1, draft.GetProperty("sections")[0].GetProperty("completionCount").GetInt32());
        Assert.True(draft.GetProperty("sections")[1].GetProperty("canRemove").GetBoolean());
        var section = await SectionDraft(administrator, sectionId);
        Assert.Equal(1, section.GetProperty("completionCount").GetInt32()); Assert.False(section.GetProperty("canRemove").GetBoolean());
        // A note attached to the second module, and an answer to the third module's prompt, each keep their module in place.
        await CompleteModule(participant, 1);
        var second = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, "/notes", new { body = "A module note.", moduleId = second.GetProperty("id").GetGuid(), sessionId = (Guid?)null })).StatusCode);
        var noted = await Delete(administrator, $"/administration/modules/{second.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.Conflict, noted.StatusCode); Assert.Equal("1 note is attached to this module. It cannot be removed while those records stand.", (await Body(noted)).GetProperty("title").GetString());
        await CompleteModule(participant, 2);
        var third = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, "/notes", new { body = "An answer.", moduleId = third.GetProperty("id").GetGuid(), sessionId = (Guid?)null, promptId = third.GetProperty("preparationPrompts")[0].GetProperty("id").GetGuid() })).StatusCode);
        var answered = await Delete(administrator, $"/administration/modules/{third.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.Conflict, answered.StatusCode); Assert.Equal("1 participant has answered a prompt in this module. It cannot be removed while those records stand.", (await Body(answered)).GetProperty("title").GetString());
        var progress = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(2, progress.GetProperty("completed").GetInt32()); Assert.Equal(3, progress.GetProperty("currentOrdinal").GetInt32()); Assert.Equal(3, progress.GetProperty("modules").GetArrayLength());
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(15, await db.Sections.CountAsync()); Assert.Equal(10, await db.Completions.CountAsync()); Assert.Equal(0, await db.CurriculumAudits.CountAsync());
    }

    // Traces to: L2-048 AC1–AC3, L2-016 AC3, L2-061 AC4. Given a module a participant has completed, when a section is
    // added, then it is placed last, dated by the clock, and reopens the module without rewriting the history; a
    // revised section is read next time; a removed one closes the gap.
    [Fact]
    public async Task Given_a_completed_module_when_a_section_is_added_through_the_api_then_completion_reopens_without_rewriting_history()
    {
        var curriculum = await fixture.SeedProgramme(modules: 2);
        await fixture.Enroll(await fixture.SeedCohort(curriculum));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        var moduleId = (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("id").GetGuid();
        await CompleteModule(participant, 1);
        Assert.Equal(2, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("currentOrdinal").GetInt32());
        fixture.Clock.UtcNow += TimeSpan.FromDays(1);
        using var administrator = await fixture.Administrator();
        var added = (await Body(await ApiFixture.Post(administrator, $"/administration/modules/{moduleId}/sections", new { title = "A sixth section", reading = "New reading.\n\nSecond paragraph." }))).GetProperty("id").GetGuid();
        var section = await SectionDraft(administrator, added);
        Assert.Equal(6, section.GetProperty("ordinal").GetInt32()); Assert.Equal(6, section.GetProperty("sectionCount").GetInt32()); Assert.Equal(fixture.Clock.UtcNow, section.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal("Module 1", section.GetProperty("moduleTitle").GetString()); Assert.Equal(6, section.GetProperty("siblings").GetArrayLength()); Assert.True(section.GetProperty("canRemove").GetBoolean());
        Assert.Equal(12000, section.GetProperty("limits").GetProperty("reading").GetInt32());
        var reopened = await participant.GetFromJsonAsync<JsonElement>("/curriculum");
        Assert.Equal(1, reopened.GetProperty("currentOrdinal").GetInt32()); Assert.Equal(0, reopened.GetProperty("completed").GetInt32());
        var module = await participant.GetFromJsonAsync<JsonElement>("/modules/1");
        Assert.Equal(5, module.GetProperty("completedSections").GetInt32()); Assert.Equal(6, module.GetProperty("sections").GetArrayLength()); Assert.Equal(added, module.GetProperty("resumeSectionId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await Put(administrator, $"/administration/sections/{added}", new { title = "A sixth section, revised", reading = "<script>literal</script>\n\nRevised.", revision = section.GetProperty("revision").GetGuid() })).StatusCode);
        var read = (await participant.GetFromJsonAsync<JsonElement>("/modules/1")).GetProperty("sections")[5];
        Assert.Equal("A sixth section, revised", read.GetProperty("title").GetString()); Assert.Equal("<script>literal</script>\n\nRevised.", read.GetProperty("reading").GetString());
        var fifth = (await ModuleDraft(administrator, moduleId)).GetProperty("sections")[4].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await Delete(administrator, $"/administration/sections/{fifth}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(administrator, $"/administration/sections/{added}")).StatusCode);
        Assert.Equal([1, 2, 3, 4, 5], (await ModuleDraft(administrator, moduleId)).GetProperty("sections").EnumerateArray().Select(s => s.GetProperty("ordinal").GetInt32()).ToList());
        Assert.Equal(2, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("currentOrdinal").GetInt32());
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(5, await db.Completions.CountAsync()); Assert.All(await db.Completions.ToListAsync(), c => Assert.True(c.CompletedAt < fixture.Clock.UtcNow));
        Assert.Equal(["SectionAdded", "SectionRemoved", "SectionRevised"], (await db.CurriculumAudits.Select(x => x.Action).ToListAsync()).Order().ToList());
    }

    // Traces to: L2-051 AC3, AC6–AC7, AC9, L2-065 AC1. Given the reading maximum, when exactly that many four-byte
    // characters are saved, then the save succeeds; one more is refused naming the field and the overage; markup is
    // stored and returned literally; and a stale revision is refused.
    [Fact]
    public async Task Given_the_reading_maximum_when_exactly_that_many_four_byte_characters_are_saved_then_it_succeeds_and_one_more_is_refused()
    {
        var curriculum = await fixture.SeedProgramme(modules: 1, publish: false);
        using var administrator = await fixture.Administrator();
        var sectionId = (await ModuleDraft(administrator, (await administrator.GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculum}")).GetProperty("modules")[0].GetProperty("id").GetGuid())).GetProperty("sections")[0].GetProperty("id").GetGuid();
        var draft = await SectionDraft(administrator, sectionId);
        var exact = string.Concat(Enumerable.Repeat("\U0001F600", 12000));
        var saved = await Put(administrator, $"/administration/sections/{sectionId}", new { title = "At the maximum", reading = exact, revision = draft.GetProperty("revision").GetGuid() });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var revision = (await Body(saved)).GetProperty("revision").GetGuid();
        Assert.Equal(exact, (await SectionDraft(administrator, sectionId)).GetProperty("reading").GetString());
        var over = await Put(administrator, $"/administration/sections/{sectionId}", new { title = "Over the maximum", reading = exact + "\U0001F600", revision });
        Assert.Equal(HttpStatusCode.BadRequest, over.StatusCode);
        Assert.Equal("A reading may be at most 12000 characters. Shorten it by 1.", (await Body(over)).GetProperty("errors").GetProperty("reading")[0].GetString());
        var stale = await Put(administrator, $"/administration/sections/{sectionId}", new { title = "Stale", reading = "Stale.", revision = draft.GetProperty("revision").GetGuid() });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode); Assert.Equal("This section changed since it was opened. Reload it to see the current content.", (await Body(stale)).GetProperty("title").GetString());
        Assert.Equal("At the maximum", (await SectionDraft(administrator, sectionId)).GetProperty("title").GetString());
        var unknown = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await administrator.GetAsync($"/administration/sections/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/sections/{unknown}", new { title = "Nobody", reading = "Nowhere", revision })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(administrator, $"/administration/sections/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ApiFixture.Post(administrator, $"/administration/modules/{unknown}/sections", new { title = "Nobody", reading = "Nowhere" })).StatusCode);
    }

    private static async Task<List<int>> Ordinals(HttpClient administrator, string url, string list) => (await administrator.GetFromJsonAsync<JsonElement>(url)).GetProperty(list).EnumerateArray().Select(x => x.GetProperty("ordinal").GetInt32()).ToList();
    private static async Task<List<Guid>> Ids(HttpClient administrator, string url, string list) => (await administrator.GetFromJsonAsync<JsonElement>(url)).GetProperty(list).EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToList();

    // Traces to: L2-049 AC1, AC3–AC4, L2-061 AC4. Given four modules, when the fourth moves to second, then the positions
    // are contiguous and unique; a list that is not a permutation is refused and the stored order stands.
    [Fact]
    public async Task Given_four_modules_when_the_fourth_moves_to_second_then_positions_are_contiguous_and_unique()
    {
        var curriculum = await fixture.SeedProgramme(modules: 4);
        using var administrator = await fixture.Administrator();
        var ids = await Ids(administrator, $"/administration/curricula/{curriculum}", "modules");
        var order = new[] { ids[0], ids[3], ids[1], ids[2] };
        Assert.Equal(HttpStatusCode.NoContent, (await Put(administrator, $"/administration/curricula/{curriculum}/modules/order", new { order })).StatusCode);
        Assert.Equal(order, await Ids(administrator, $"/administration/curricula/{curriculum}", "modules"));
        Assert.Equal([1, 2, 3, 4], await Ordinals(administrator, $"/administration/curricula/{curriculum}", "modules"));
        foreach (var bad in new[] { new[] { ids[0], ids[3], ids[1] }, new[] { ids[0], ids[3], ids[1], ids[2], Guid.NewGuid() }, new[] { ids[0], ids[0], ids[1], ids[2] }, new[] { ids[0], ids[3], ids[1], Guid.NewGuid() } })
        {
            var refused = await Put(administrator, $"/administration/curricula/{curriculum}/modules/order", new { order = bad });
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal("The submitted order does not match the modules of this programme. Reload and try again.", (await Body(refused)).GetProperty("title").GetString());
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await Put(administrator, $"/administration/curricula/{curriculum}/modules/order", new { order = Array.Empty<Guid>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/curricula/{Guid.NewGuid()}/modules/order", new { order })).StatusCode);
        Assert.Equal(order, await Ids(administrator, $"/administration/curricula/{curriculum}", "modules"));
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal([1, 2, 3, 4], (await db.Modules.Where(x => x.CurriculumId == curriculum).Select(x => x.Ordinal).ToListAsync()).Order().ToList());
        Assert.Equal("ModulesReordered", (await db.CurriculumAudits.SingleAsync()).Action);
    }

    // Traces to: L2-049 AC2, L2-047 AC5, L2-050 AC4. Given a module's sections and prompts, when each is reordered, then a
    // participant reads the sections in the new order with a completed one still complete, and reads the prompts in the
    // new order against the session that follows the module.
    [Fact]
    public async Task Given_sections_and_prompts_when_reordered_then_the_participant_reads_the_new_order_and_keeps_their_completion()
    {
        var curriculum = await fixture.SeedProgramme(modules: 2, prompts: 3);
        var cohort = await fixture.SeedCohort(curriculum);
        var enrollment = await fixture.Enroll(cohort);
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        var module = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        var moduleId = module.GetProperty("id").GetGuid();
        var sections = module.GetProperty("sections").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, $"/sections/{sections[0]}/completion", new { })).StatusCode);
        using var administrator = await fixture.Administrator();
        var sectionOrder = new[] { sections[1], sections[2], sections[3], sections[4], sections[0] };
        Assert.Equal(HttpStatusCode.NoContent, (await Put(administrator, $"/administration/modules/{moduleId}/sections/order", new { order = sectionOrder })).StatusCode);
        var read = (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("sections").EnumerateArray().ToList();
        Assert.Equal(sectionOrder, read.Select(x => x.GetProperty("id").GetGuid()));
        Assert.Equal([1, 2, 3, 4, 5], read.Select(x => x.GetProperty("ordinal").GetInt32()));
        Assert.True(read[4].GetProperty("isComplete").GetBoolean()); Assert.Equal(sections[1], (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("resumeSectionId").GetGuid());
        var prompts = await Ids(administrator, $"/administration/modules/{moduleId}", "prompts");
        var promptOrder = new[] { prompts[2], prompts[0], prompts[1] };
        Assert.Equal(HttpStatusCode.NoContent, (await Put(administrator, $"/administration/modules/{moduleId}/prompts/order", new { order = promptOrder })).StatusCode);
        Assert.Equal(promptOrder, (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("preparationPrompts").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        Guid session;
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
            var mentor = (await db.Cohorts.SingleAsync(x => x.Id == cohort)).MentorId!.Value;
            var booking = new QuinntyneBrownStewardship.Domain.Scheduling.Booking { EnrollmentId = enrollment, CreatedAt = fixture.Clock.UtcNow, Slot = new() { MentorId = mentor, StartsAt = fixture.Clock.UtcNow.AddDays(3) } };
            db.Bookings.Add(booking); await db.SaveChangesAsync(); session = booking.Id;
        }
        var preparation = await participant.GetFromJsonAsync<JsonElement>($"/sessions/{session}/preparation");
        Assert.Equal(promptOrder, preparation.GetProperty("prompts").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        var refused = await Put(administrator, $"/administration/modules/{moduleId}/prompts/order", new { order = new[] { prompts[0], prompts[1] } });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode); Assert.Contains("prompts of this module", (await Body(refused)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await Put(administrator, $"/administration/modules/{Guid.NewGuid()}/sections/order", new { order = sectionOrder })).StatusCode);
    }

    // Traces to: L2-049 AC3. Given a reorder whose second pass fails after the staging pass, when the stored order is
    // inspected, then it is the order that held before the reorder began.
    [Fact]
    public async Task Given_a_reorder_that_fails_after_staging_when_the_order_is_inspected_then_it_is_the_order_that_held_before()
    {
        var curriculum = await fixture.SeedProgramme(modules: 3);
        using var administrator = await fixture.Administrator();
        var before = await Ids(administrator, $"/administration/curricula/{curriculum}", "modules");
        using var failing = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => { services.AddScoped<CurriculumStore>(); services.RemoveAll<ICurriculumStore>(); services.AddScoped<ICurriculumStore, FailingCurriculumStore>(); }));
        using var client = failing.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.AdministratorEmail, ApiFixture.Password })).StatusCode);
        var failed = await Put(client, $"/administration/curricula/{curriculum}/modules/order", new { order = new[] { before[2], before[0], before[1] } });
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.DoesNotContain("private-database", await failed.Content.ReadAsStringAsync());
        Assert.Equal(before, await Ids(administrator, $"/administration/curricula/{curriculum}", "modules"));
        Assert.Equal([1, 2, 3], await Ordinals(administrator, $"/administration/curricula/{curriculum}", "modules"));
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().CurriculumAudits.CountAsync());
    }
}
