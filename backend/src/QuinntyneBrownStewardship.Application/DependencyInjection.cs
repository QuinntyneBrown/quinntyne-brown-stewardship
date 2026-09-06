using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config => { config.RegisterServicesFromAssemblyContaining<SignInCommand>(); config.AddOpenBehavior(typeof(ValidationBehavior<,>)); });
        services.AddValidatorsFromAssemblyContaining<SignInCommandValidator>();
        services.AddScoped<Programme.ProgrammeReader>();
        services.AddScoped<Scheduling.BookingOperations>();
        return services;
    }
}
