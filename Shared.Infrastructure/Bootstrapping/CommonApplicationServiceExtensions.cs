using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Middleware;
using Shared.Infrastructure.Swagger;
using System.Text.Json.Serialization;

namespace Shared.Infrastructure.Bootstrapping;

/// <summary>
/// Common service registration and pipeline (CORS, JWT, Swagger, GlobalException).
/// Use in each API: builder.AddCommonService("API Title"); app.UseCommonService();
/// </summary>
public static class CommonApplicationServiceExtensions
{
    public static IHostApplicationBuilder AddCommonService(this IHostApplicationBuilder builder, string swaggerApiTitle = "API")
    {
        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        builder.AddJwtAuthentication();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSharedSwaggerGen(swaggerApiTitle);

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return builder;
    }

    public static WebApplication UseCommonService(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DocumentTitle = $"{app.Environment.ApplicationName} - API";
                c.DefaultModelsExpandDepth(1);
                c.DisplayRequestDuration();
            });
        }

        app.UseCors("AllowAll");
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
