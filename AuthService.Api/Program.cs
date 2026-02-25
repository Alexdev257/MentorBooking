using AuthService.Infrastructure.DependencyInjection;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Shared.Infrastructure;
using Shared.Infrastructure.Swagger;

namespace AuthService.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập JWT (lấy từ POST /api/auth/login). Ví dụ: Bearer {token}"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddSharedInfrastructure(builder.Configuration);
        builder.Services.AddAuthServiceInfrastructure(builder.Configuration);

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var conn = db.Database.GetConnectionString();
            Console.WriteLine($"?? Connection string: {conn}");

            var pending = db.Database.GetPendingMigrations().ToList();
            Console.WriteLine($"?? Pending migrations: {pending.Count}");

            if (pending.Any())
            {
                Console.WriteLine("?? Running database migrations...");
                db.Database.Migrate();
                Console.WriteLine("? Migration completed.");
            }
            else
            {
                Console.WriteLine("? No pending migrations.");
            }
        }

        await AuthService.Infrastructure.Persistence.DefaultAdminSeeder.SeedAsync(app.Services);

        app.UseSharedInfrastructure();
        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();

        await app.RunAsync();
    }
}
