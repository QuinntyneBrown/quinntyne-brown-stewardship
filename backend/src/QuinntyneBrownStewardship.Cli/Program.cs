using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuinntyneBrownStewardship.Application;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Infrastructure;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Common;
using System.Text.Json;
namespace QuinntyneBrownStewardship.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var expected = args.FirstOrDefault() switch { "migrate" => 1, "provision" or "import-curriculum" or "create-cohort" or "publish-availability" => 2, "provision-mentor" or "enroll" => 3, _ => 0 };
        if (expected == 0 || args.Length != expected)
        {
            Console.Error.WriteLine("Usage: migrate | provision <email> | provision-mentor <email> <display-name> | import-curriculum <json-file> | create-cohort <json-file> | enroll <email> <cohort-id> | publish-availability <json-file>");
            return 2;
        }
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        try
        {
            if (args[0] == "migrate")
            {
                await scope.ServiceProvider.GetRequiredService<StewardshipDbContext>().Database.MigrateAsync();
                Console.WriteLine("Database migrations applied.");
                return 0;
            }
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            switch (args[0])
            {
                case "import-curriculum":
                    Console.WriteLine($"Imported {await sender.Send(new ImportCurriculumCommand(await Read<CurriculumImport>(args[1])))} modules.");
                    return 0;
                case "create-cohort":
                    Console.WriteLine($"Cohort: {await sender.Send(await Read<CreateCohortCommand>(args[1]))}");
                    return 0;
                case "enroll":
                    if (!Guid.TryParse(args[2], out var cohortId)) { Console.Error.WriteLine("Supply a valid cohort identifier."); return 2; }
                    Console.WriteLine($"Enrollment: {await sender.Send(new EnrollParticipantCommand(args[1], cohortId))}");
                    return 0;
                case "publish-availability":
                    Console.WriteLine($"Published {await sender.Send(new PublishAvailabilityCommand(await Read<AvailabilityImport>(args[1])))} slots.");
                    return 0;
            }
            Console.Write("Password: ");
            var password = PasswordPrompt.Read();
            Console.Write("Confirm password: ");
            if (password != PasswordPrompt.Read()) { Console.Error.WriteLine("Passwords do not match."); return 2; }
            var created = args[0] == "provision-mentor"
                ? await sender.Send(new ProvisionMentorCommand(args[1], password, args[2]))
                : await sender.Send(new ProvisionParticipantCommand(args[1], password));
            Console.WriteLine(created ? args[0] == "provision-mentor" ? "Mentor created." : "Participant created without an enrollment." : "That email address is already registered.");
            return created ? 0 : 1;
        }
        catch (ValidationException exception) { foreach (var error in exception.Errors) Console.Error.WriteLine(error.ErrorMessage); return 2; }
        catch (ProgrammeException exception) { Console.Error.WriteLine(exception.Message); return 2; }
        catch (JsonException) { Console.Error.WriteLine("Invalid JSON document. Check the documented input format and field types."); return 2; }
        catch (IOException) { Console.Error.WriteLine("The input document could not be read. Check its path and permissions."); return 2; }
        catch (Exception) { Console.Error.WriteLine("The operation failed. Check database connectivity and configuration."); return 1; }
    }
    private static async Task<T> Read<T>(string path) => JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new JsonException("Empty document.");
}
