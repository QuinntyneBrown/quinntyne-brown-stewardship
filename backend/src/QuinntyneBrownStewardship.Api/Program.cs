using Microsoft.AspNetCore.Authentication;
using QuinntyneBrownStewardship.Application;
using QuinntyneBrownStewardship.Infrastructure;
using QuinntyneBrownStewardship.Infrastructure.Access;
using QuinntyneBrownStewardship.Api.Http;
namespace QuinntyneBrownStewardship.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthentication(SessionCookie.Scheme).AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionCookie.Scheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-StewardshipCsrf"; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Strict; });
        builder.Services.AddControllersWithViews();
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpsRedirection(options => options.HttpsPort = builder.Configuration.GetValue<int?>("HttpsPort") ?? 7240);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16384);
        var app = builder.Build();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
        app.Run();
    }
}
