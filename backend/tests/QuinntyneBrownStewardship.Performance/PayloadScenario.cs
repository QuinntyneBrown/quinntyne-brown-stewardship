using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Api.Tests;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Programme;
using Xunit;

namespace QuinntyneBrownStewardship.Performance;

public static class PayloadScenario
{
    public static async Task<List<Measurement>> Capture(ApiFixture fixture, CurriculumImport curriculum, string output, bool measureReads = false)
    {
        await fixture.Reset();
        // The five three-day booking cycles finish at the real current time, so
        // held sessions remain in the past in both API and browser fixtures.
        fixture.Clock.UtcNow -= TimeSpan.FromDays(15);
        using var scope = fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ImportCurriculumCommand(curriculum));
        await fixture.PublishCurriculum(await fixture.CurriculumId(curriculum.Key));
        await sender.Send(new ProvisionMentorCommand("mentor@example.com", ApiFixture.Password, "Performance mentor"));
        var cohort = Guid.NewGuid();
        await sender.Send(new CreateCohortCommand(cohort, DateOnly.FromDateTime(fixture.Clock.UtcNow.UtcDateTime).AddDays(-7), "mentor@example.com", curriculum.Key, 12, 2));
        await sender.Send(new EnrollParticipantCommand(ApiFixture.Email, cohort));
        using var client = fixture.Browser();
        BookingResponse? booking = null;
        // Five past sessions and one future session exercise the complete allowance.
        for (var i = 0; i < 6; i++)
        {
            await ApiFixture.SignIn(client);
            var slot = new SlotImport(Guid.NewGuid(), fixture.Clock.UtcNow.AddDays(2));
            await sender.Send(new PublishAvailabilityCommand(new("mentor@example.com", [slot])));
            using var created = await ApiFixture.Post(client, "/sessions", new { SlotId = slot.Id });
            created.EnsureSuccessStatusCode(); booking = await created.Content.ReadFromJsonAsync<BookingResponse>();
            if (i < 5) fixture.Clock.UtcNow = slot.StartsAt.AddDays(1);
        }
        var module = curriculum.Modules.OrderBy(x => x.Ordinal).First();
        Guid noteId = default;
        // Random text prevents repeated fixture strings from hiding transfer costs.
        for (var i = 0; i < 20; i++)
        {
            foreach (var attachment in new[] { new { ModuleId = (Guid?)module.Id, SessionId = (Guid?)null }, new { ModuleId = (Guid?)null, SessionId = (Guid?)booking!.Id } })
            {
                using var saved = await ApiFixture.Post(client, "/notes", new { attachment.ModuleId, attachment.SessionId, Body = LongBody() });
                saved.EnsureSuccessStatusCode(); noteId = (await saved.Content.ReadFromJsonAsync<NoteResponse>())!.Id;
            }
        }
        foreach (var prompt in module.PreparationPrompts)
        {
            using var saved = await ApiFixture.Post(client, "/notes", new { ModuleId = module.Id, PromptId = prompt.Id, Body = LongBody() });
            saved.EnsureSuccessStatusCode();
        }
        var responses = new Dictionary<string, object>();
        using var administrator = await fixture.Administrator();
        var curriculumId = await fixture.CurriculumId(curriculum.Key);
        var firstModule = curriculum.Modules.OrderBy(x => x.Ordinal).First();
        var firstSection = firstModule.Sections.OrderBy(x => x.Ordinal).First();
        foreach (var (path, reader) in new[] { ("/authentication/session (administrator)", "/authentication/session"), ("/administration/curricula", "/administration/curricula"), ($"/administration/curricula/{curriculumId}", $"/administration/curricula/{curriculumId}"), ($"/administration/modules/{firstModule.Id}", $"/administration/modules/{firstModule.Id}"), ($"/administration/sections/{firstSection.Id}", $"/administration/sections/{firstSection.Id}") })
            responses.Add(path, await Captured(administrator, reader));
        foreach (var path in new[] { "/authentication/session", "/enrollment", "/curriculum", "/modules/current", "/sessions/availability", "/sessions/history", "/notes", $"/notes/{noteId}", $"/sessions/{booking!.Id}", $"/sessions/{booking.Id}/preparation" })
            responses.Add(path, await Captured(client, path));
        var report = new { measuredAt = DateTimeOffset.UtcNow, scenario = "20 distinct 10,000-character Unicode module notes; 20 Unicode session notes; all three preparation answers at 10,000 Unicode characters; five past sessions and one future session; the administrator's session and the four administration reads of the bundled programme; responses compressed by the production middleware", noteId, sessionId = booking!.Id, programmeId = curriculumId, moduleId = firstModule.Id, sectionId = firstSection.Id, responses };
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        var measurements = new List<Measurement>();
        if (!measureReads) return measurements;
        var readers = new List<HttpClient> { client };
        try
        {
            for (var i = 1; i < 5; i++)
            {
                var reader = fixture.Browser(); readers.Add(reader); await ApiFixture.SignIn(reader);
            }
            foreach (var reader in readers) reader.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br");
            // Traces to: L2-039 AC1. Five independently authenticated devices read
            // the populated collection, including production response compression.
            foreach (var path in new[] { "/modules/current", "/notes", $"/notes/{noteId}", $"/sessions/{booking.Id}/preparation" })
                measurements.Add(await Program.Measure("GET " + path + " (long Unicode notes)", 300, (reader, _) => readers[reader].GetAsync(path)));
        }
        finally { foreach (var reader in readers.Skip(1)) reader.Dispose(); }
        return measurements;
    }

    // One response as the browser receives it: the body, its compressed and uncompressed sizes, the headers with a transport reserve, and the server time.
    private static async Task<object> Captured(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.AcceptEncoding.ParseAdd("br");
        var start = Stopwatch.GetTimestamp();
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var compressed = await response.Content.ReadAsByteArrayAsync();
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        Assert.Contains("br", response.Content.Headers.ContentEncoding);
        using var decoder = new BrotliStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var body = new MemoryStream(); await decoder.CopyToAsync(body);
        var bytes = body.ToArray();
        return new { body = JsonSerializer.Deserialize<JsonElement>(bytes), uncompressedBytes = bytes.Length, compressedBytes = compressed.Length,
            // Reserve transport headers in addition to the observed application headers.
            headerBytes = Encoding.UTF8.GetByteCount(response.Headers + response.Content.Headers.ToString()) + 1000,
            serverMilliseconds = Math.Round(elapsed, 2) };
    }
    private static string LongBody() => new(Enumerable.Range(0, 10000).Select(_ => (char)RandomNumberGenerator.GetInt32(0x4e00, 0xa000)).ToArray());
}
