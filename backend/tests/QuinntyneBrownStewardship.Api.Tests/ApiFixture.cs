using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuinntyneBrownStewardship.Api;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Email = "participant@example.com";
    public const string Password = "A private testing phrase 42!";
    public const string AdministratorEmail = "administrator@example.com";
    public TestClock Clock { get; } = new();
    public string ConnectionString { get; } = MakeConnectionString();
    private static string MakeConnectionString()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("STEWARDSHIP_TEST_SQL") ?? "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True");
        connection.InitialCatalog = "StewardshipTests_" + Guid.NewGuid().ToString("N");
        return connection.ConnectionString;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/QuinntyneBrownStewardship.Api")));
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<StewardshipDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<StewardshipDbContext>>();
            services.AddDbContext<StewardshipDbContext>(options => options.UseSqlServer(ConnectionString));
            services.RemoveAll<ISystemClock>();
            services.AddSingleton<ISystemClock>(Clock);
        });
    }
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Database.MigrateAsync();
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Database.EnsureDeletedAsync();
        await DisposeAsync();
    }
    public HttpClient Browser() => CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    public async Task Reset()
    {
        Clock.UtcNow = DateTimeOffset.UtcNow;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        await db.Notes.ExecuteDeleteAsync();
        await db.BookingAudits.ExecuteDeleteAsync();
        await db.Bookings.ExecuteDeleteAsync();
        await db.Availability.ExecuteDeleteAsync();
        await db.Completions.ExecuteDeleteAsync();
        await db.Prompts.ExecuteDeleteAsync();
        await db.Sections.ExecuteDeleteAsync();
        await db.Modules.ExecuteDeleteAsync();
        await db.Enrollments.ExecuteDeleteAsync();
        await db.Cohorts.ExecuteDeleteAsync();
        await db.CurriculumAudits.ExecuteDeleteAsync();
        await db.Curricula.ExecuteDeleteAsync();
        await db.Sessions.ExecuteDeleteAsync();
        await db.SignInAttempts.ExecuteDeleteAsync();
        await db.Participants.ExecuteDeleteAsync();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Assert.True(await sender.Send(new ProvisionParticipantCommand(Email, Password)));
        Assert.True(await sender.Send(new ProvisionAdministratorCommand(AdministratorEmail, Password)));
    }
    // The administrator signed in on a fresh browser.
    public async Task<HttpClient> Administrator()
    {
        var client = Browser();
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "/authentication/sign-in", new { EmailAddress = AdministratorEmail, Password })).StatusCode);
        return client;
    }
    public static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, object? body)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/authentication/csrf");
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body ?? new { }) };
        request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        return await client.SendAsync(request);
    }
    public static Task<HttpResponseMessage> Post(HttpClient client, string url, object body) => Send(client, HttpMethod.Post, url, body);
    public static async Task SignIn(HttpClient client)
    {
        var response = await Post(client, "/authentication/sign-in", new { EmailAddress = Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    // Provisions the account when it does not exist yet, then signs it in on a fresh browser.
    public async Task<HttpClient> Participant(string email)
    {
        using (var scope = Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ProvisionParticipantCommand(email, Password));
        var client = Browser();
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "/authentication/sign-in", new { EmailAddress = email, Password })).StatusCode);
        return client;
    }
    public async Task<Guid> SeedProgramme(int modules = 12, int sectionsPerModule = 5, int prompts = 1, string key = "starter", bool publish = true, DateTimeOffset? sectionsCreatedAt = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var state = publish ? PublicationState.Published : PublicationState.Draft;
        var curriculum = new Curriculum { Key = key, Title = key, State = state, CreatedAt = Clock.UtcNow, PublishedAt = publish ? Clock.UtcNow : null };
        curriculum.Modules = Enumerable.Range(1, modules).Select(i => new CurriculumModule
        {
            CurriculumId = curriculum.Id, Ordinal = i, Title = $"Module {i}", Summary = "Practice stewardship", EffortEstimate = "30 minutes", PracticeSteps = ["Listen", "Reflect", "Revise"], State = state,
            Sections = Enumerable.Range(1, sectionsPerModule).Select(s => new ModuleSection { Ordinal = s, Title = $"Section {s}", Reading = "Consider who benefits and who bears the cost.", CreatedAt = sectionsCreatedAt ?? Clock.UtcNow.AddDays(-90) }).ToList(),
            PreparationPrompts = Enumerable.Range(1, prompts).Select(p => new PreparationPrompt { Ordinal = p, Text = "Whose experience changed your decision?" }).ToList()
        }).ToList();
        db.Curricula.Add(curriculum);
        await db.SaveChangesAsync();
        return curriculum.Id;
    }
    public async Task<Guid> SeedCohort(Guid curriculumId, int durationWeeks = 12, int cadenceWeeks = 2, DateOnly? start = null, string? mentorEmail = "mentor@example.com", string mentorName = "Assigned mentor")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        Guid? mentorId = null;
        if (mentorEmail != null)
        {
            var mentor = await db.Participants.SingleOrDefaultAsync(x => x.EmailAddress == mentorEmail);
            if (mentor == null)
            {
                // The mentor shares the fixture password so a test can sign in as the mentor too.
                var hash = (await db.Participants.SingleAsync(x => x.EmailAddress == Email)).PasswordHash;
                mentor = new Participant { EmailAddress = mentorEmail, NormalizedEmail = mentorEmail.ToUpperInvariant(), PasswordHash = hash, IsMentor = true, DisplayName = mentorName };
                db.Participants.Add(mentor);
            }
            mentorId = mentor.Id;
        }
        var cohort = new Cohort { CurriculumId = curriculumId, StartDate = start ?? DateOnly.FromDateTime(Clock.UtcNow.UtcDateTime).AddDays(-7), MentorId = mentorId, MentorName = mentorName, DurationWeeks = durationWeeks, SessionCadenceWeeks = cadenceWeeks };
        db.Cohorts.Add(cohort);
        await db.SaveChangesAsync();
        return cohort.Id;
    }
    public async Task<Guid> Enroll(Guid cohortId, string email = Email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardshipDbContext>();
        var participant = await db.Participants.SingleAsync(x => x.EmailAddress == email);
        var enrollment = new Enrollment { ParticipantId = participant.Id, CohortId = cohortId };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        return enrollment.Id;
    }
    public async Task<Guid> CurriculumId(string key = "starter")
    {
        using var scope = Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Curricula.SingleAsync(x => x.Key == key)).Id;
    }
    // Publication goes through the administrator's own request, so every caller exercises the real path.
    public async Task PublishCurriculum(Guid id)
    {
        using var administrator = await Administrator();
        var response = await Post(administrator, $"/administration/curricula/{id}/publication", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
