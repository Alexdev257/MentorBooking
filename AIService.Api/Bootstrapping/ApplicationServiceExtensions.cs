using AIService.Infrastructure.DependencyInjection;
using AIService.Infrastructure.Persistence;
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
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddCommonService("AI Service API");

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
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AIApplicationDbContext>();
        var pending = db.Database.GetPendingMigrations().ToList();
        if (pending.Count > 0)
            db.Database.Migrate();
    }
}
