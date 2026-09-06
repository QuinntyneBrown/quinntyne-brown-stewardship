using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Infrastructure.Access;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Enrollment;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class AccessAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.Reset();
    public Task DisposeAsync() => Task.CompletedTask;

    // Traces to: L2-007 absence is based on persisted active enrollment, not a constant.
    [Fact]
    public async Task Given_an_active_enrollment_when_resolved_then_the_participants_actual_cohort_is_returned()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var participant = await db.Participants.SingleAsync();
        var cohort = new QuinntyneBrownStewardship.Domain.Enrollment.Cohort
        {
            MentorName = "Assigned mentor",
            StartDate = DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-14)
        };
        db.Enrollments.Add(new() { ParticipantId = participant.Id, Cohort = cohort });
        await db.SaveChangesAsync();
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        var enrollment = await client.GetFromJsonAsync<EnrollmentResponse>("/enrollment");
        Assert.True(enrollment!.IsEnrolled);
        Assert.Equal(cohort.Id, enrollment.CohortId);
        Assert.Equal("Assigned mentor", enrollment.MentorName);
        Assert.Equal(3, enrollment.CurrentWeek);
        Assert.Equal(6, enrollment.SessionAllowance);
    }

    // Traces to: L2-001 AC3; both failed credentials invoke the same real hasher.
    [Fact]
    public async Task Given_unknown_and_registered_addresses_when_passwords_fail_then_both_pay_the_hash_verification_cost()
    {
        await using var host = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPasswordHasher>();
            services.AddSingleton<PasswordHasher>();
            services.AddSingleton<CountingPasswordHasher>();
            services.AddSingleton<IPasswordHasher>(provider => provider.GetRequiredService<CountingPasswordHasher>());
        }));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var hasher = host.Services.GetRequiredService<CountingPasswordHasher>();
        await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, Password = "wrong" });
        await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = "unknown@example.com", Password = "wrong" });
        Assert.Equal(2, hasher.VerifiedHashes.Count);
        Assert.NotEqual(hasher.DummyHash, hasher.VerifiedHashes[0]);
        Assert.Equal(hasher.DummyHash, hasher.VerifiedHashes[1]);
    }

    // Traces to: L2-037 AC4, L2-040 AC3.
    [Fact]
    public async Task Given_an_unhandled_dependency_error_when_enrollment_is_requested_then_only_a_safe_error_and_logged_identifier_escape()
    {
        using var logs = new CapturedLogs();
        await using var host = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(logs);
            services.AddScoped<AccessStore>();
            services.RemoveAll<IAccessStore>();
            services.AddScoped<IAccessStore, FailingEnrollmentStore>();
        }));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await ApiFixture.SignIn(client);
        var response = await client.GetAsync("/enrollment");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private-", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        var correlation = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.Contains(correlation, body);
        Assert.Contains(logs.Messages, message => message.Contains(correlation));
    }

    // Traces to: L2-004 AC2, L2-007 prerequisite.
    [Fact]
    public async Task Given_an_unidentified_visitor_when_enrollment_is_requested_then_access_is_refused()
    {
        using var client = fixture.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/enrollment")).StatusCode);
    }

    // Traces to: L2-007 AC1–2, L2-001 AC1, L2-035 AC4.
    [Fact]
    public async Task Given_a_provisioned_participant_when_signed_in_then_enrollment_is_explicitly_absent()
    {
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        var response = await client.GetAsync("/enrollment?participantId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        Assert.Equal(new EnrollmentResponse(false), enrollment);
    }

    // Traces to: L2-001 AC2–3.
    [Fact]
    public async Task Given_wrong_or_unknown_credentials_when_submitted_then_failures_are_identical()
    {
        using var client = fixture.Browser();
        var wrong = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, Password = "wrong" });
        var unknown = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = "unknown@example.com", Password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Contains("Email address or password is incorrect", await wrong.Content.ReadAsStringAsync());
    }

    // Traces to: L2-001 AC4, L2-036 AC1.
    [Theory]
    [InlineData("", "", "emailAddress")]
    [InlineData("bad", "password", "emailAddress")]
    [InlineData(ApiFixture.Email, "", "password")]
    public async Task Given_invalid_fields_when_submitted_then_the_invalid_field_is_named(string email, string password, string field)
    {
        using var client = fixture.Browser();
        var response = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = email, Password = password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, await response.Content.ReadAsStringAsync());
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().SignInAttempts.CountAsync());
    }

    // Traces to: L2-036 AC4, L2-035 AC4.
    [Fact]
    public async Task Given_extra_identity_fields_when_signing_in_then_only_credentials_determine_identity()
    {
        using var client = fixture.Browser();
        Assert.Equal(HttpStatusCode.OK, (await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, ApiFixture.Password, ParticipantId = Guid.NewGuid(), IsAdmin = true })).StatusCode);
        Assert.Equal(ApiFixture.Email, (await client.GetFromJsonAsync<SessionResponse>("/authentication/session"))!.EmailAddress);
    }

    // Traces to: L2-037 AC2; provisioning acceptance.
    [Fact]
    public async Task Given_an_existing_account_when_provisioned_again_then_the_original_hash_is_preserved()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var original = await db.Participants.AsNoTracking().SingleAsync();
        Assert.DoesNotContain(ApiFixture.Password, original.PasswordHash);
        Assert.False(await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ProvisionParticipantCommand("PARTICIPANT@example.com", "Another private password!")));
        Assert.Equal(original.PasswordHash, (await db.Participants.AsNoTracking().SingleAsync()).PasswordHash);
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
    }

    // Traces to: L2-037 AC1,3; mutation CSRF protection.
    [Fact]
    public async Task Given_sign_in_when_inspected_then_https_csrf_and_secure_cookie_are_enforced()
    {
        using var client = fixture.Browser();
        var noToken = await client.PostAsJsonAsync("/authentication/sign-in", new { EmailAddress = ApiFixture.Email, ApiFixture.Password });
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var response = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, ApiFixture.Password });
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-Stewardship="));
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("expires=", cookie);
        using var http = fixture.CreateClient(new() { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.TemporaryRedirect, (await http.GetAsync("/enrollment")).StatusCode);
    }

    // Traces to: L2-002 AC1–3.
    [Fact]
    public async Task Given_a_persisted_session_when_returning_then_activity_refreshes_the_idle_deadline()
    {
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        fixture.Clock.UtcNow += TimeSpan.FromDays(14);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/enrollment")).StatusCode);
        fixture.Clock.UtcNow += TimeSpan.FromDays(29);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/enrollment")).StatusCode);
        fixture.Clock.UtcNow += TimeSpan.FromDays(30);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/enrollment")).StatusCode);
    }

    // Traces to: L2-003 AC1–3.
    [Fact]
    public async Task Given_two_devices_when_one_signs_out_then_only_that_session_is_revoked()
    {
        using var first = fixture.Browser(); using var second = fixture.Browser();
        await ApiFixture.SignIn(first); await ApiFixture.SignIn(second);
        Assert.Equal(HttpStatusCode.NoContent, (await ApiFixture.Post(first, "/authentication/sign-out", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/enrollment")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/enrollment")).StatusCode);
        using var scope = fixture.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Sessions.CountAsync(x => x.RevokedAt != null));
    }

    // Traces to: L2-038 AC1–4.
    [Fact]
    public async Task Given_ten_failures_when_more_credentials_are_submitted_then_cooling_off_applies_to_both_passwords()
    {
        using var client = fixture.Browser();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, Password = "wrong" })).StatusCode);
        var correct = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, ApiFixture.Password });
        var wrong = await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, Password = "wrong" });
        Assert.Equal(HttpStatusCode.TooManyRequests, correct.StatusCode);
        Assert.Equal(await correct.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
        using var scope = fixture.Services.CreateScope();
        var attempts = await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().SignInAttempts.ToListAsync();
        Assert.Equal(12, attempts.Count);
        Assert.All(attempts, x => { Assert.NotEmpty(x.Origin); Assert.Equal(fixture.Clock.UtcNow, x.At); });
        fixture.Clock.UtcNow += TimeSpan.FromMinutes(15);
        await ApiFixture.SignIn(client);
    }

    // Traces to: L2-038 origin throttling.
    [Fact]
    public async Task Given_an_exhausted_origin_when_a_different_account_signs_in_then_it_is_throttled()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        db.SignInAttempts.AddRange(Enumerable.Range(0, 100).Select(i => new QuinntyneBrownStewardship.Domain.Access.SignInAttempt { NormalizedEmail = $"OTHER{i}@EXAMPLE.COM", Origin = "local", At = fixture.Clock.UtcNow }));
        await db.SaveChangesAsync();
        using var client = fixture.Browser();
        Assert.Equal(HttpStatusCode.TooManyRequests, (await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = ApiFixture.Email, ApiFixture.Password })).StatusCode);
    }

    // Traces to: L2-007, L2-005 AC2 (enrollment lookup only).
    [Fact]
    public async Task Given_another_participants_cohort_when_enrollment_is_requested_then_it_is_never_returned()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ProvisionParticipantCommand("another@example.com", ApiFixture.Password));
        var other = await db.Participants.SingleAsync(x => x.EmailAddress == "another@example.com");
        var cohort = new QuinntyneBrownStewardship.Domain.Enrollment.Cohort { MentorName = "Another mentor", StartDate = new DateOnly(2026, 9, 1) };
        db.Enrollments.Add(new() { ParticipantId = other.Id, Cohort = cohort });
        await db.SaveChangesAsync();
        using var client = fixture.Browser();
        await ApiFixture.SignIn(client);
        Assert.False((await client.GetFromJsonAsync<EnrollmentResponse>("/enrollment?participantId=" + other.Id))!.IsEnrolled);
    }
}
