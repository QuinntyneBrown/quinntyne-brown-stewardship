using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using Xunit;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class OperationsAcceptanceTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    // Traces to: L2-040. Given a reachable database, when health is requested,
    // then the service reports application and database readiness.
    [Fact]
    public async Task Given_a_reachable_database_when_health_is_requested_then_readiness_is_reported()
    {
        using var client = fixture.Browser();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.Equal("Healthy", health.GetProperty("checks").GetProperty("database").GetString());
    }

    // Traces to: L2-040. Given an unreachable database, when health is requested,
    // then 503 names the failed dependency without exposing a connection string.
    [Fact]
    public async Task Given_an_unreachable_database_when_health_is_requested_then_database_failure_is_safe()
    {
        await using var host = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<StewardshipDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<StewardshipDbContext>>();
            services.AddDbContext<StewardshipDbContext>(options => options.UseSqlServer("Server=tcp:127.0.0.1,1;Database=unavailable;User Id=health;Password=not-a-real-secret;Connect Timeout=1;TrustServerCertificate=True"));
        }));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("database", text);
        Assert.DoesNotContain("not-a-real-secret", text);
        Assert.DoesNotContain("127.0.0.1", text);
    }
}
