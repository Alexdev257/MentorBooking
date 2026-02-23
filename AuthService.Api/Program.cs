
using AuthService.Infrastructure.DependencyInjection;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure;
using Shared.Infrastructure.Swagger;

namespace AuthService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //builder.Services.AddSharedSwagger();

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

        app.Run();
    }
}
