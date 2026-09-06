using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class AdministrationAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;
    private static async Task<CurriculumImport> Content() => JsonSerializer.Deserialize<CurriculumImport>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "starter-curriculum.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    // Traces to: L2-005, L2-008, L2-016. Given the full starter curriculum,
    // when imported repeatedly and extended, then identifiers and progress survive,
    // and adding a section reopens the module without rewriting completion history.
    [Fact]
    public async Task Given_the_starter_curriculum_when_imported_and_extended_then_progress_is_preserved_and_rederived()
    {
        var content = await Content();
        using var scope = fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Assert.Equal(12, await sender.Send(new ImportCurriculumCommand(content)));
        // Each command has the same scoped lifetime as a separate CLI invocation.
        using (var repeat = fixture.Services.CreateScope()) Assert.Equal(12, await repeat.ServiceProvider.GetRequiredService<ISender>().Send(new ImportCurriculumCommand(await Content())));
        Assert.True(await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Assigned mentor")));
        var cohortId = Guid.NewGuid();
        await sender.Send(new CreateCohortCommand(cohortId, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime), "mentor@example.com"));
        var enrollment = await sender.Send(new EnrollParticipantCommand(ApiFixture.Email, cohortId));
        Assert.Equal(enrollment, await sender.Send(new EnrollParticipantCommand(ApiFixture.Email, cohortId)));
        using var client = fixture.Browser(); await ApiFixture.SignIn(client);
        var module = await client.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(content.Modules[0].Sections[0].Reading, module.GetProperty("sections")[0].GetProperty("reading").GetString());
        foreach (var section in module.GetProperty("sections").EnumerateArray())
            Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, $"/sections/{section.GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("completed").GetInt32());
        content.Modules[0].Sections.Add(new() { Id = Guid.NewGuid(), Ordinal = 6, Title = "Revisit the responsibility", Reading = "Return to the original promise and record the effect of the change." });
        using (var update = fixture.Services.CreateScope()) await update.ServiceProvider.GetRequiredService<ISender>().Send(new ImportCurriculumCommand(content));
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("completed").GetInt32());
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(5, await db.Completions.CountAsync());
        Assert.Equal(61, await db.Sections.CountAsync());
    }

    // Traces to: L2-005–006, L2-017. Invalid operations leave persisted programme
    // state unchanged: no second active cohort and no overlapping mentor slots.
    [Fact]
    public async Task Given_programme_setup_when_enrollment_or_slots_conflict_then_the_operation_is_rejected()
    {
        using var scope = fixture.Services.CreateScope(); var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ImportCurriculumCommand(await Content()));
        await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Assigned mentor"));
        var cohort = Guid.NewGuid(); var other = Guid.NewGuid(); var start = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime);
        await sender.Send(new CreateCohortCommand(cohort, start, "mentor@example.com"));
        await sender.Send(new CreateCohortCommand(other, start, "mentor@example.com"));
        await sender.Send(new EnrollParticipantCommand(ApiFixture.Email, cohort));
        await Assert.ThrowsAsync<ProgrammeException>(() => sender.Send(new EnrollParticipantCommand(ApiFixture.Email, other)));
        using (var publish = fixture.Services.CreateScope())
            await Assert.ThrowsAsync<ProgrammeException>(() => publish.ServiceProvider.GetRequiredService<ISender>().Send(new PublishAvailabilityCommand(new("mentor@example.com", [new(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2)), new(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2).AddMinutes(30))]))));
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(1, await db.Enrollments.CountAsync()); Assert.Equal(0, await db.Availability.CountAsync());
    }
}
