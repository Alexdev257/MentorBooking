using AIService.Infrastructure.DependencyInjection;
using AIService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure;
using Shared.Infrastructure.Bootstrapping;

namespace AIService.Api.Bootstrapping;

/// <summary>
/// Centralized registration and pipeline for AIService (Verendar-style flow).
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>Aligned with <c>TranscriptController</c> upload limit (500 MB).</summary>
    private const long MaxMultipartBytes = 524_288_000;

    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Do not use AddServiceDefaults(): AddStandardResilienceHandler defaults to 30s total timeout and cancels Groq transcribe.
        builder.AddServiceDefaultsWithoutStandardHttpResilience();
        builder.AddCommonService("AI Service API");

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = MaxMultipartBytes;
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxMultipartBytes;
        });

        builder.Services.AddSharedInfrastructure(builder.Configuration);
        builder.Services.AddAIServiceInfrastructure(builder.Configuration);
        builder.Services.AddControllers();

        return builder;
    }

    public static WebApplication UseApplicationServices(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        RunMigrationsIfNeeded(app);

        app.UseCommonService();
        app.UseHttpsRedirection();
        app.MapControllers();

        return app;
    }

    private static void RunMigrationsIfNeeded(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AIApplicationDbContext>();
            var conn = db.Database.GetConnectionString();
            Console.WriteLine($"Connection string: {conn}");

            var pending = db.Database.GetPendingMigrations().ToList();
            Console.WriteLine($"Pending migrations: {pending.Count}");

            if (pending.Count > 0)
            {
                Console.WriteLine("Running database migrations...");
                db.Database.Migrate();
                Console.WriteLine("Migration completed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Migration failed, app will start anyway: {ex.Message}");
        }
    }
}
