using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class AuthoringAccessAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;

    // Traces to: L2-041 AC1–AC4, L2-043 AC1–AC3, L2-061 AC1. Given no identity, a participant, and an
    // administrator, when the session and the programme index are requested, then authority comes from
    // the session alone: the visitor is refused 401, the participant 403 even with a supplied flag, and
    // only the administrator reads the index.
    [Fact]
    public async Task Given_no_identity_a_participant_and_an_administrator_when_the_programme_index_is_requested_then_only_the_administrator_is_admitted()
    {
        await fixture.SeedProgramme(publish: false);
        using var anonymous = fixture.Browser();
        var refused = await anonymous.GetAsync("/administration/curricula");
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode); Assert.Null(refused.Headers.Location);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/authentication/session")).StatusCode);
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        Assert.False((await participant.GetFromJsonAsync<JsonElement>("/authentication/session")).GetProperty("isAdministrator").GetBoolean());
        participant.DefaultRequestHeaders.Add("X-Administrator", "true");
        var forbidden = await participant.GetAsync("/administration/curricula?isAdministrator=true");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode); Assert.Null(forbidden.Headers.Location);
        Assert.DoesNotContain("starter", await forbidden.Content.ReadAsStringAsync());
        using var administrator = await fixture.Administrator();
        Assert.True((await administrator.GetFromJsonAsync<JsonElement>("/authentication/session")).GetProperty("isAdministrator").GetBoolean());
        var index = await administrator.GetFromJsonAsync<JsonElement>("/administration/curricula");
        var programme = Assert.Single(index.GetProperty("programmes").EnumerateArray());
        Assert.Equal("starter", programme.GetProperty("key").GetString()); Assert.Equal("Draft", programme.GetProperty("state").GetString());
        Assert.Equal(12, programme.GetProperty("moduleCount").GetInt32()); Assert.Equal(0, programme.GetProperty("cohortCount").GetInt32());
        Assert.Equal(120, index.GetProperty("limits").GetProperty("title").GetInt32());
    }

    // Traces to: L2-043 AC4. Given an administrator whose session has expired, when an authoring
    // endpoint is called, then the API responds 401.
    [Fact]
    public async Task Given_an_expired_administrator_session_when_an_authoring_endpoint_is_called_then_it_is_refused()
    {
        using var administrator = await fixture.Administrator();
        Assert.Equal(HttpStatusCode.OK, (await administrator.GetAsync("/administration/curricula")).StatusCode);
        fixture.Clock.UtcNow += TimeSpan.FromDays(31);
        Assert.Equal(HttpStatusCode.Unauthorized, (await administrator.GetAsync("/administration/curricula")).StatusCode);
    }

    // Traces to: L2-064 AC1–AC4. Given an operator, when authority is provisioned, granted, and
    // withdrawn, then the next request reflects it, an unknown address changes nothing, and an
    // enrolled participant keeps their enrollment and recorded progress.
    [Fact]
    public async Task Given_an_operator_when_authority_is_provisioned_granted_and_withdrawn_then_the_next_request_reflects_it()
    {
        await fixture.Enroll(await fixture.SeedCohort(await fixture.SeedProgramme()));
        using var scope = fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Assert.False(await sender.Send(new SetAdministratorAuthorityCommand("nobody@example.com", true)));
        Assert.True(await sender.Send(new ProvisionAdministratorCommand("operator@example.com", ApiFixture.Password)));
        Assert.False(await sender.Send(new ProvisionAdministratorCommand("operator@example.com", ApiFixture.Password)));
        using var participant = fixture.Browser(); await ApiFixture.SignIn(participant);
        var module = await participant.GetFromJsonAsync<JsonElement>("/modules/current");
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(participant, $"/sections/{module.GetProperty("sections")[0].GetProperty("id").GetGuid()}/completion", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/administration/curricula")).StatusCode);
        Assert.True(await sender.Send(new SetAdministratorAuthorityCommand(ApiFixture.Email, true)));
        // The claim is issued from the account row on every request, so the grant reaches the same session.
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/administration/curricula")).StatusCode);
        Assert.True((await participant.GetFromJsonAsync<JsonElement>("/authentication/session")).GetProperty("isAdministrator").GetBoolean());
        Assert.Equal(12, (await participant.GetFromJsonAsync<JsonElement>("/curriculum")).GetProperty("modules").GetArrayLength());
        Assert.Equal(1, (await participant.GetFromJsonAsync<JsonElement>("/modules/current")).GetProperty("completedSections").GetInt32());
        Assert.True(await sender.Send(new SetAdministratorAuthorityCommand(ApiFixture.Email, false)));
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/administration/curricula")).StatusCode);
        using var operatorClient = fixture.Browser();
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(operatorClient, "/authentication/sign-in", new { EmailAddress = "operator@example.com", ApiFixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await operatorClient.GetAsync("/administration/curricula")).StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Assert.Equal(1, await db.Enrollments.CountAsync()); Assert.Equal(1, await db.Completions.CountAsync());
    }
}
