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
using Npgsql;
using Shared.Infrastructure.Bus;
using System.Reflection;

namespace AIService.Infrastructure.DependencyInjection
{
    public static class ManageDependencyInjection
    {
        /// <summary>EF/Npgsql default is 30s — large transcripts (text + many segments) fail SaveChanges on slow DB (e.g. Render).</summary>
        private const int AiDbCommandTimeoutSeconds = 1800;

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
            services.AddGroqTranscription();
            services.AddAutoMapper(typeof(AIServiceMappingProfile));
            services.AddMediatRInfrastructure(configuration);
            services.AddMessageBus(configuration);
            services.AddHttpClient("url-downloader", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(15);
            });

            services.AddHostedService<TranscriptProcessingService>();
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

            connectionString = Shared.Infrastructure.Persistence.ConnectionStringHelper.Normalize(connectionString);
            var npgsqlBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                CommandTimeout = AiDbCommandTimeoutSeconds
            };
            connectionString = npgsqlBuilder.ConnectionString;

            services.AddDbContext<AIService.Infrastructure.Persistence.AIApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.CommandTimeout(AiDbCommandTimeoutSeconds);
                });
            });

            services.AddScoped<DbContext>(provider => provider.GetService<AIService.Infrastructure.Persistence.AIApplicationDbContext>()!);
        }

        private static void AddScopedInterface(this IServiceCollection service)
        {
            service.AddScoped<IAIUnitOfWork, UnitOfWork>();
            service.AddScoped<IFileStorageService, LocalFileStorageService>();
            service.AddScoped<IMediaProcessingService, FfmpegMediaProcessingService>();
            service.AddScoped<ITranscriptService, TranscriptService>();
        }

        private static void AddGroqTranscription(this IServiceCollection service)
        {
            service.AddHttpClient<ITranscriptionService, GroqTranscriptionService>(client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/");
                // Long media can keep Groq busy longer than 10 minutes.
                client.Timeout = TimeSpan.FromMinutes(45);
            });
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
