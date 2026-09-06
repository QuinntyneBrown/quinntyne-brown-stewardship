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
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Email = "participant@example.com";
    public const string Password = "A private testing phrase 42!";
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
        await db.Enrollments.ExecuteDeleteAsync();
        await db.Cohorts.ExecuteDeleteAsync();
        await db.Sessions.ExecuteDeleteAsync();
        await db.SignInAttempts.ExecuteDeleteAsync();
        await db.Participants.ExecuteDeleteAsync();
        Assert.True(await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ProvisionParticipantCommand(Email, Password)));
    }
    public static async Task<HttpResponseMessage> Post(HttpClient client, string url, object body)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/authentication/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        return await client.SendAsync(request);
    }
    public static async Task SignIn(HttpClient client)
    {
        var response = await Post(client, "/authentication/sign-in", new { EmailAddress = Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
