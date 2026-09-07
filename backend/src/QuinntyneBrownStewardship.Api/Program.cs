using Microsoft.AspNetCore.Authentication;
using QuinntyneBrownStewardship.Application;
using QuinntyneBrownStewardship.Infrastructure;
using QuinntyneBrownStewardship.Infrastructure.Access;
using QuinntyneBrownStewardship.Api.Http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthentication(SessionCookie.Scheme).AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionCookie.Scheme, _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(AdministrationPolicy.Name, policy => policy.RequireRole(AdministrationPolicy.Role)));
        builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-StewardshipCsrf"; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Strict; });
        builder.Services.AddControllersWithViews();
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpsRedirection(options => options.HttpsPort = builder.Configuration.GetValue<int?>("HttpsPort") ?? 7240);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
        builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
        builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
        builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", timeout: TimeSpan.FromSeconds(5));
        var app = builder.Build();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        app.UseHttpsRedirection();
        app.UseResponseCompression();
        // Browser document navigations share screen paths with JSON endpoints.
        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.Request.Method) && context.Request.GetTypedHeaders().Accept?.Any(x => x.MediaType == "text/html") == true
                && new[] { "/curriculum", "/modules", "/sessions", "/notes", "/admin" }.Any(x => context.Request.Path.StartsWithSegments(x)))
                context.Request.Path = "/index.html";
            await next();
        });
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(x => x.Key, x => x.Value.Status.ToString())
            })
        });
        app.MapFallbackToFile("index.html");
        app.Run();
    }
}
