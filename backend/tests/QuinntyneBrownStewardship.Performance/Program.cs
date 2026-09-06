using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Api.Tests;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Administration;
using Xunit;
namespace QuinntyneBrownStewardship.Performance;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
        var fixture = new ApiFixture();
        var clients = new List<HttpClient>();
        var initialized = false;
        try
        {
            await fixture.InitializeAsync(); initialized = true; await fixture.Reset();
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
            var content = JsonSerializer.Deserialize<CurriculumImport>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "starter-curriculum.json")), json)!;
            using (var scope = fixture.Services.CreateScope())
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ImportCurriculumCommand(content));
                await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Performance mentor"));
                var cohort = Guid.NewGuid();
                await sender.Send(new CreateCohortCommand(cohort, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-7), "mentor@example.com"));
                for (var i = 0; i < 5; i++)
                {
                    var email = $"performance-{i}@example.com";
                    await sender.Send(new ProvisionParticipantCommand(email, ApiFixture.Password));
                    await sender.Send(new EnrollParticipantCommand(email, cohort));
                    var client = fixture.Browser(); clients.Add(client);
                    (await ApiFixture.Post(client, "/authentication/sign-in", new { EmailAddress = email, ApiFixture.Password })).EnsureSuccessStatusCode();
                    var csrf = await client.GetFromJsonAsync<CsrfResponse>("/authentication/csrf");
                    client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
                }
            }
            var results = new List<Measurement>();
            foreach (var path in new[] { "/curriculum", "/modules/current", "/sessions/availability", "/sessions/history", "/notes" })
                results.Add(await Measure("GET " + path, 300, (client, _) => clients[client].GetAsync(path)));
            results.Add(await Measure("POST /notes", 500, (client, iteration) => clients[client].PostAsJsonAsync("/notes", new { ModuleId = content.Modules[0].Id, Body = $"Reflection {client}/{iteration}: attend to the person carrying the cost." })));
            var report = new { measuredAt = DateTimeOffset.UtcNow, runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(), processors = Environment.ProcessorCount, configuration = "Release; real SQL Server; in-process ASP.NET HTTP host; five independently authenticated participants; 15 warm-up requests per operation", results };
            var output = args.FirstOrDefault() ?? ".local/api-performance.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, json));
            foreach (var result in results) Console.WriteLine($"{result.Operation}: p95 {result.P95Milliseconds:F1} ms / {result.BudgetMilliseconds} ms — {(result.Passed ? "PASS" : "FAIL")}");
            return results.All(x => x.Passed) ? 0 : 1;
        }
        finally
        {
            foreach (var client in clients) client.Dispose();
            if (initialized) await ((IAsyncLifetime)fixture).DisposeAsync();
            else await fixture.DisposeAsync();
        }
    }

    private static async Task<Measurement> Measure(string operation, int budget, Func<int, int, Task<HttpResponseMessage>> request)
    {
        for (var i = 0; i < 15; i++) { using var response = await request(i % 5, -i); response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync(); }
        var times = new ConcurrentBag<double>();
        await Task.WhenAll(Enumerable.Range(0, 5).Select(async client =>
        {
            for (var i = 0; i < 20; i++)
            {
                var start = Stopwatch.GetTimestamp(); using var response = await request(client, i);
                response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync();
                times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
        }));
        var p95 = times.Order().ElementAt(94);
        return new(operation, 100, 5, Math.Round(p95, 2), budget, p95 <= budget);
    }
}
