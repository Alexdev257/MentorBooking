using Microsoft.AspNetCore.Http;
using AuthService.Infrastructure.DependencyInjection;
using AuthService.Infrastructure.Persistence;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Npgsql;
using AuthService.Api.Swagger;
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
            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
            });
            options.SchemaFilter<AdminFormFileSchemaFilter>();
            options.OperationFilter<AdminMultipartAvatarOperationFilter>();
        });
        //builder.Services.AddSwaggerGen(options =>
        //{
        //    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });
        //    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        //    {
        //        Name = "Authorization",
        //        Type = SecuritySchemeType.Http,
        //        Scheme = "Bearer",
        //        BearerFormat = "JWT",
        //        In = ParameterLocation.Header,
        //        Description = "Nhập JWT (lấy từ POST /api/auth/login). Ví dụ: Bearer {token}"
        //    });
        //    options.AddSecurityRequirement(new OpenApiSecurityRequirement
        //    {
        //        {
        //            new OpenApiSecurityScheme
        //            {
        //                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        //            },
        //            Array.Empty<string>()
        //        }
        //    });
        //});

        builder.Services.AddSharedInfrastructure(builder.Configuration);
        builder.Services.AddAuthServiceInfrastructure(builder.Configuration);

        var firebaseCredJson = builder.Configuration["Firebase:CredentialJson"];
        var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"]
                               ?? "mentorbookingproject-firebase-adminsdk-fbsvc-a7290ef766.json";

        if (!string.IsNullOrWhiteSpace(firebaseCredJson))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseCredJson)
            });
            Console.WriteLine("Firebase Admin SDK initialized from environment variable.");
        }
        else if (File.Exists(firebaseCredPath))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseCredPath)
            });
            Console.WriteLine("Firebase Admin SDK initialized from file.");
        }
        else
        {
            Console.WriteLine($"[WARNING] Firebase credentials not found. Set Firebase__CredentialJson env var or place file at '{firebaseCredPath}'.");
        }

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var conn = db.Database.GetConnectionString();
            Console.WriteLine($"Connection string: {conn}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var pending = (await db.Database.GetPendingMigrationsAsync(cts.Token)).ToList();
            Console.WriteLine($"Pending migrations: {pending.Count}");

            if (pending.Any())
            {
                Console.WriteLine("Running database migrations...");
                await db.Database.MigrateAsync(cts.Token);
                Console.WriteLine("Migration completed.");
            }
            else
            {
                Console.WriteLine("No pending migrations.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Migration failed, app will start anyway: {ex.Message}");
        }

        try
        {
            await AuthService.Infrastructure.Persistence.DefaultAdminSeeder.SeedAsync(app.Services);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(
                "Database table does not exist. If migrations were never applied or tables were dropped, run: DELETE FROM \"__EFMigrationsHistory\"; then restart the application to apply migrations. Error: {Message}",
                ex.Message);
            throw;
        }

        app.UseSharedInfrastructure();
        app.MapDefaultEndpoints();

        app.UseSwagger();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsDevelopment())
            app.UseHttpsRedirection();
        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();

        await app.RunAsync();
    }
}
