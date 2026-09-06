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
namespace QuinntyneBrownStewardship.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("migrate" or "provision") || (args[0] == "provision" && args.Length != 2))
        {
            Console.Error.WriteLine("Usage: migrate | provision <email-address> (password prompted; at least 12 characters)");
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
            Console.Write("Password: ");
            var password = PasswordPrompt.Read();
            Console.Write("Confirm password: ");
            if (password != PasswordPrompt.Read()) { Console.Error.WriteLine("Passwords do not match."); return 2; }
            var created = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ProvisionParticipantCommand(args[1], password));
            Console.WriteLine(created ? "Participant created without an enrollment." : "That email address is already registered.");
            return created ? 0 : 1;
        }
        catch (ValidationException exception) { foreach (var error in exception.Errors) Console.Error.WriteLine(error.ErrorMessage); return 2; }
        catch (Exception) { Console.Error.WriteLine("The operation failed. Check database connectivity and configuration."); return 1; }
    }
}
