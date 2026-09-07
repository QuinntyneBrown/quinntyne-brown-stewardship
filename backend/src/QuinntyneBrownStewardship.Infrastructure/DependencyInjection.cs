using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Infrastructure.Access;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StewardshipDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Stewardship") ?? "Server=(localdb)\\MSSQLLocalDB;Database=Stewardship;Trusted_Connection=True;TrustServerCertificate=True"));
        services.AddOptions<SessionLifetimeOptions>().Bind(configuration.GetSection("SessionLifetime")).Validate(x => x.IdleTimeout > TimeSpan.Zero).ValidateOnStart();
        services.AddOptions<SignInOptions>().Bind(configuration.GetSection("SignIn")).Validate(x => x.AccountLimit > 0 && x.OriginLimit > 0 && x.Window > TimeSpan.Zero).ValidateOnStart();
        services.AddOptions<PasswordHashingOptions>().Bind(configuration.GetSection("PasswordHashing")).Validate(x => x.IterationCount >= 100000).ValidateOnStart();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAccessStore, AccessStore>();
        services.AddScoped<IProgrammeStore, ProgrammeStore>();
        services.AddScoped<ICurriculumStore, CurriculumStore>();
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddOptions<QuinntyneBrownStewardship.Application.Notes.NoteOptions>().Bind(configuration.GetSection("Notes")).Validate(x => x.MaxLength > 0 && x.MaxLength <= 10000).ValidateOnStart();
        services.AddOptions<QuinntyneBrownStewardship.Application.Administration.CurriculumOptions>().Bind(configuration.GetSection("Curriculum"))
            // The longest authored field in four-byte characters plus the rest of the body stays under the request body limit, so the field message is what an author reads.
            .Validate(x => x.KeyMaxLength is > 0 and <= 100 && x.TitleMaxLength > 0 && x.SummaryMaxLength > 0 && x.EffortEstimateMaxLength > 0 && x.StepMaxLength > 0 && x.PromptMaxLength > 0 && x.ReadingMaxLength > 0 && x.ReadingMaxLength * 4 + 8192 <= 65536).ValidateOnStart();
        services.AddScoped<ISignInGate, SignInGate>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentParticipant, CurrentParticipant>();
        services.AddScoped<IRequestOrigin, RequestOrigin>();
        services.AddScoped<IDeviceSession, DeviceSession>();
        services.AddScoped<ICsrfToken, CsrfToken>();
        return services;
    }
}
