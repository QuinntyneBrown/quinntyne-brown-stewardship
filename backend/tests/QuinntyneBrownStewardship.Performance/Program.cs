using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Api.Tests;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Programme;
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
            var slots = Enumerable.Range(0, 10).Select(i => new SlotImport(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2).AddHours(i))).ToList();
            using (var scope = fixture.Services.CreateScope())
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ImportCurriculumCommand(content));
                await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Performance mentor"));
                var cohort = Guid.NewGuid();
                await sender.Send(new CreateCohortCommand(cohort, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-7), "mentor@example.com"));
                await sender.Send(new PublishAvailabilityCommand(new("mentor@example.com", slots)));
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
            // Traces to: L2-039 AC1–2. Given five active participants, when each
            // programme operation receives 100 requests after warm-up, then its
            // p95 stays within the read/write budget. Setup is outside timing.
            foreach (var path in new[] { "/curriculum", "/modules/current", "/sessions/availability", "/sessions/history", "/notes" })
                results.Add(await Measure("GET " + path, 300, (client, _) => clients[client].GetAsync(path)));
            results.Add(await Measure("POST /notes", 500, (client, iteration) => clients[client].PostAsJsonAsync("/notes", new { ModuleId = content.Modules[0].Id, Body = $"Reflection {client}/{iteration}: attend to the person carrying the cost." })));
            var notes = new NoteResponse[5];
            for (var i = 0; i < 5; i++) notes[i] = (await clients[i].GetFromJsonAsync<NotesResponse>("/notes"))!.Notes[0];
            results.Add(await Measure("GET /notes/{id}", 300, (client, _) => clients[client].GetAsync($"/notes/{notes[client].Id}")));
            results.Add(await Measure("POST /notes (revision)", 500, async (client, iteration) =>
            {
                var note = notes[client];
                var response = await clients[client].PostAsJsonAsync("/notes", new { note.Id, note.ModuleId, note.Revision, Body = $"Revised reflection {iteration}" });
                response.EnsureSuccessStatusCode(); notes[client] = (await response.Content.ReadFromJsonAsync<NoteResponse>())!;
                return response;
            }));
            var sections = content.Modules.OrderBy(x => x.Ordinal).SelectMany(x => x.Sections.OrderBy(s => s.Ordinal)).ToArray();
            var nextSection = new int[5];
            results.Add(await Measure("POST /sections/{id}/completion", 500, (client, _) => clients[client].PostAsJsonAsync($"/sections/{sections[nextSection[client]++].Id}/completion", new { })));
            var bookings = new BookingResponse?[5];
            async Task<HttpResponseMessage> Book(int client)
            {
                var response = await clients[client].PostAsJsonAsync("/sessions", new { SlotId = slots[client * 2].Id });
                response.EnsureSuccessStatusCode(); bookings[client] = await response.Content.ReadFromJsonAsync<BookingResponse>();
                return response;
            }
            results.Add(await Measure("POST /sessions", 500, (client, _) => Book(client), async (client, _) =>
            {
                if (bookings[client] is not { } booking) return;
                using var response = await clients[client].DeleteAsync($"/sessions/{booking.Id}"); response.EnsureSuccessStatusCode();
            }));
            foreach (var suffix in new[] { "", "/preparation" })
                results.Add(await Measure("GET /sessions/{id}" + suffix, 300, (client, _) => clients[client].GetAsync($"/sessions/{bookings[client]!.Id}{suffix}")));
            results.Add(await Measure("PUT /sessions/{id}/slot", 500, async (client, _) =>
            {
                var booking = bookings[client]!;
                var target = slots[client * 2 + (booking.SlotId == slots[client * 2].Id ? 1 : 0)].Id;
                var response = await clients[client].PutAsJsonAsync($"/sessions/{booking.Id}/slot", new { SlotId = target });
                response.EnsureSuccessStatusCode(); bookings[client] = await response.Content.ReadFromJsonAsync<BookingResponse>();
                return response;
            }));
            results.Add(await Measure("DELETE /sessions/{id}", 500, async (client, _) =>
            {
                var response = await clients[client].DeleteAsync($"/sessions/{bookings[client]!.Id}"); bookings[client] = null; return response;
            }, async (client, _) => { if (bookings[client] == null) { using var response = await Book(client); } }));
            // Valid long notes must also fit the screen's transfer budget. Random
            // plain text avoids a misleadingly small compressed repeated fixture.
            for (var i = 0; i < 20; i++)
            {
                using var response = await clients[0].PostAsJsonAsync("/notes", new { ModuleId = content.Modules[0].Id, Body = Convert.ToBase64String(RandomNumberGenerator.GetBytes(7500)) });
                response.EnsureSuccessStatusCode();
            }
            var payloads = new List<object>();
            foreach (var path in new[] { "/curriculum", "/modules/1", "/sessions/availability", "/sessions/history", "/notes" })
            {
                using var response = await clients[0].GetAsync(path); response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                using var outputBody = new MemoryStream();
                using (var compressor = new BrotliStream(outputBody, CompressionLevel.Fastest, true)) await compressor.WriteAsync(bytes);
                payloads.Add(new { path, uncompressedBytes = bytes.Length, compressedBytes = outputBody.Length });
            }
            var report = new { measuredAt = DateTimeOffset.UtcNow, runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(), processors = Environment.ProcessorCount, configuration = "Release; real SQL Server; in-process ASP.NET HTTP host; five independently authenticated participants; 15 warm-up requests per operation", results, payloadScenario = "First participant has 20 additional distinct 10,000-character notes on module 1; Brotli fastest, excluding HTTP headers and frontend assets", payloads };
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

    private static async Task<Measurement> Measure(string operation, int budget, Func<int, int, Task<HttpResponseMessage>> request, Func<int, int, Task>? prepare = null)
    {
        for (var i = 0; i < 15; i++) { if (prepare != null) await prepare(i % 5, -i); using var response = await request(i % 5, -i); response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync(); }
        var times = new ConcurrentBag<double>();
        await Task.WhenAll(Enumerable.Range(0, 5).Select(async client =>
        {
            for (var i = 0; i < 20; i++)
            {
                if (prepare != null) await prepare(client, i);
                var start = Stopwatch.GetTimestamp(); using var response = await request(client, i);
                response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync();
                times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
        }));
        var p95 = times.Order().ElementAt(94);
        return new(operation, 100, 5, Math.Round(p95, 2), budget, p95 <= budget);
    }
}
