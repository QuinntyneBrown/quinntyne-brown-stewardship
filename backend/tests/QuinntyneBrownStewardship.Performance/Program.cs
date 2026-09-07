using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Api.Tests;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
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
            if (args.Contains("--payload-only"))
            {
                Directory.CreateDirectory(".local");
                await PayloadScenario.Capture(fixture, content, ".local/production-responses.json");
                return 0;
            }
            var slots = Enumerable.Range(0, 10).Select(i => new SlotImport(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2).AddHours(i))).ToList();
            using (var scope = fixture.Services.CreateScope())
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ImportCurriculumCommand(content));
                await fixture.PublishCurriculum(await fixture.CurriculumId(content.Key));
                await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Performance mentor"));
                var cohort = Guid.NewGuid();
                await sender.Send(new CreateCohortCommand(cohort, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-7), "mentor@example.com", content.Key, 12, 2));
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
            if (args.Contains("--sign-in-only"))
            {
                await ClearSignInHistory();
                var signIn = await Measure("POST /authentication/sign-in", 500, (client, _) => clients[client].PostAsJsonAsync("/authentication/sign-in", new { EmailAddress = $"performance-{client}@example.com", ApiFixture.Password }), afterWarmup: ClearSignInHistory);
                Console.WriteLine(JsonSerializer.Serialize(signIn, json));
                return signIn.Passed ? 0 : 1;
            }
            // Traces to: L2-039 AC1–2. Given five active participants, when each
            // programme operation receives 100 requests after warm-up, then its
            // p95 stays within the read/write budget. Setup is outside timing.
            foreach (var path in new[] { "/authentication/session", "/authentication/csrf", "/enrollment", "/health", "/curriculum", "/modules/current", "/modules/1", "/sessions/availability", "/sessions/history", "/notes" })
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
            async Task ClearSignInHistory()
            {
                using var scope = fixture.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().SignInAttempts.ExecuteDeleteAsync();
            }
            // A warm-up is separate from the 100 successful authentication samples.
            // Reset only the isolated fixture's attempt history between these phases;
            // production rate limits and their concurrent refusal tests stay intact.
            await ClearSignInHistory();
            results.Add(await Measure("POST /authentication/sign-in", 500, (client, _) => clients[client].PostAsJsonAsync("/authentication/sign-in", new { EmailAddress = $"performance-{client}@example.com", ApiFixture.Password }), afterWarmup: ClearSignInHistory));
            await ClearSignInHistory();
            results.Add(await Measure("POST /authentication/sign-out", 500, (client, _) => clients[client].PostAsJsonAsync("/authentication/sign-out", new { }), async (client, _) =>
            {
                using var signedIn = await ApiFixture.Post(clients[client], "/authentication/sign-in", new { EmailAddress = $"performance-{client}@example.com", ApiFixture.Password });
                signedIn.EnsureSuccessStatusCode();
                var csrf = await clients[client].GetFromJsonAsync<CsrfResponse>("/authentication/csrf");
                clients[client].DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                clients[client].DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
            }, ClearSignInHistory));
            // Traces to: L2-062 AC1–AC3. Five administrators read and revise the bundled programme, reorder it, and
            // publish it again; reads stay within 300 ms, writes within 500 ms, and a publication within 2,000 ms.
            var administrators = new List<HttpClient>();
            // The measured sign-ins above spend the origin allowance, and these five are setup rather than samples.
            await ClearSignInHistory();
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    var client = await fixture.Administrator(); administrators.Add(client);
                    var csrf = await client.GetFromJsonAsync<CsrfResponse>("/authentication/csrf");
                    client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
                }
                var curriculumId = await fixture.CurriculumId(content.Key);
                var programme = (await administrators[0].GetFromJsonAsync<JsonElement>($"/administration/curricula/{curriculumId}")).GetProperty("modules").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
                // Each administrator works on a module of their own; the first section of each holds a reading at the maximum.
                var moduleDrafts = new JsonElement[5]; var sectionDrafts = new JsonElement[5];
                for (var i = 0; i < 5; i++)
                {
                    moduleDrafts[i] = (await administrators[i].GetFromJsonAsync<JsonElement>($"/administration/modules/{programme[i]}"));
                    var sectionId = moduleDrafts[i].GetProperty("sections")[0].GetProperty("id").GetGuid();
                    var draft = await administrators[i].GetFromJsonAsync<JsonElement>($"/administration/sections/{sectionId}");
                    using var filled = await administrators[i].PutAsJsonAsync($"/administration/sections/{sectionId}", new { title = draft.GetProperty("title").GetString(), reading = new string('r', 12000), revision = draft.GetProperty("revision").GetGuid() });
                    filled.EnsureSuccessStatusCode();
                    sectionDrafts[i] = await administrators[i].GetFromJsonAsync<JsonElement>($"/administration/sections/{sectionId}");
                }
                results.Add(await Measure("GET /administration/curricula", 300, (client, _) => administrators[client].GetAsync("/administration/curricula")));
                results.Add(await Measure("GET /administration/curricula/{id}", 300, (client, _) => administrators[client].GetAsync($"/administration/curricula/{curriculumId}")));
                results.Add(await Measure("GET /administration/modules/{id}", 300, (client, _) => administrators[client].GetAsync($"/administration/modules/{programme[client]}")));
                results.Add(await Measure("GET /administration/sections/{id} (12,000-character reading)", 300, (client, _) => administrators[client].GetAsync($"/administration/sections/{sectionDrafts[client].GetProperty("id").GetGuid()}")));
                results.Add(await Measure("PUT /administration/sections/{id}", 500, async (client, iteration) =>
                {
                    var draft = sectionDrafts[client];
                    var response = await administrators[client].PutAsJsonAsync($"/administration/sections/{draft.GetProperty("id").GetGuid()}", new { title = $"Revised {iteration}", reading = new string('r', 12000), revision = draft.GetProperty("revision").GetGuid() });
                    if (response.IsSuccessStatusCode) sectionDrafts[client] = await administrators[client].GetFromJsonAsync<JsonElement>($"/administration/sections/{draft.GetProperty("id").GetGuid()}");
                    return response;
                }));
                results.Add(await Measure("PUT /administration/modules/{id}", 500, async (client, iteration) =>
                {
                    var draft = moduleDrafts[client];
                    var response = await administrators[client].PutAsJsonAsync($"/administration/modules/{draft.GetProperty("id").GetGuid()}", new { title = $"Module revised {iteration}", summary = draft.GetProperty("summary").GetString(), effortEstimate = draft.GetProperty("effortEstimate").GetString(), practiceSteps = draft.GetProperty("practiceSteps").EnumerateArray().Select(x => x.GetString()).ToArray(), revision = draft.GetProperty("revision").GetGuid() });
                    if (response.IsSuccessStatusCode) moduleDrafts[client] = await administrators[client].GetFromJsonAsync<JsonElement>($"/administration/modules/{draft.GetProperty("id").GetGuid()}");
                    return response;
                }));
                // Any rotation of every module is a permutation, whatever arrangement the other administrators left.
                results.Add(await Measure("PUT /administration/curricula/{id}/modules/order", 500, (client, iteration) => administrators[client].PutAsJsonAsync($"/administration/curricula/{curriculumId}/modules/order", new { order = programme.Skip((client + iteration + 20) % programme.Length).Concat(programme.Take((client + iteration + 20) % programme.Length)).ToArray() })));
                results.Add(await Measure("POST /administration/curricula/{id}/publication", 2000, (client, _) => administrators[client].PostAsJsonAsync($"/administration/curricula/{curriculumId}/publication", new { })));
            }
            finally { foreach (var client in administrators) client.Dispose(); }
            var output = args.FirstOrDefault() ?? ".local/api-performance.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            results.AddRange(await PayloadScenario.Capture(fixture, content, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "production-responses.json"), measureReads: true));
            var report = new { measuredAt = DateTimeOffset.UtcNow, runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(), processors = Environment.ProcessorCount, configuration = "Release; real SQL Server; in-process ASP.NET HTTP host; five independently authenticated participants; 15 warm-up requests per operation; four additional compressed reads from five devices of the participant with full-length Unicode notes", results, payloadFixture = "production-responses.json" };
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

    internal static async Task<Measurement> Measure(string operation, int budget, Func<int, int, Task<HttpResponseMessage>> request, Func<int, int, Task>? prepare = null, Func<Task>? afterWarmup = null)
    {
        for (var i = 0; i < 15; i++) { if (prepare != null) await prepare(i % 5, -i); using var response = await request(i % 5, -i); response.EnsureSuccessStatusCode(); await response.Content.LoadIntoBufferAsync(); }
        if (afterWarmup != null) await afterWarmup();
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
