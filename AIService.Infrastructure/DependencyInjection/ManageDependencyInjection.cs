using AIService.Application.Common;
using AIService.Application.Configuration;
using AIService.Application.Interfaces;
using AIService.Application.Interfaces.Services;
using AIService.Application.Services;
using AIService.Infrastructure.Implements.Repositories;
using AIService.Infrastructure.Services;
using AIService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Bus;
using System.Reflection;

namespace AIService.Infrastructure.DependencyInjection
{
    public static class ManageDependencyInjection
    {
        public static IServiceCollection AddAIServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
            services.AddHttpClient<ITranscriptSummarizationService, GeminiTranscriptSummarizationService>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromMinutes(3);
            });

            services.AddDatabase(configuration);
            services.AddScopedInterface();
            services.AddAutoMapper(typeof(AIServiceMappingProfile));
            services.AddMediatRInfrastructure(configuration);
            services.AddMessageBus(configuration);
            return services;
        }

        private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("ai-db") ??
                configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Missing connection string. Expected 'ai-db' (Aspire AppHost) or 'DefaultConnection' (local appsettings).");

            services.AddDbContext<AIService.Infrastructure.Persistence.AIApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.AddScoped<DbContext>(provider => provider.GetService<AIService.Infrastructure.Persistence.AIApplicationDbContext>()!);
        }

        private static void AddScopedInterface(this IServiceCollection service)
        {
            service.AddScoped<IAIUnitOfWork, UnitOfWork>();
            service.AddScoped<IFileStorageService, LocalFileStorageService>();
            service.AddScoped<IMediaProcessingService, FfmpegMediaProcessingService>();
            service.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
            service.AddScoped<ITranscriptService, TranscriptService>();
        }

        private static void AddMediatRInfrastructure(this IServiceCollection service, IConfiguration config)
        {
            var applicationAssembly = Assembly.Load("AIService.Application");

            service.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(applicationAssembly);
            });
        }
    }
}
