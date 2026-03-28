using BookingService.Infrastructure.DependencyInjection;
using BookingService.Infrastructure.Persistence;
using Google;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddBookingServiceInfrastructure(builder.Configuration);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BookingApplicationDbContext>();
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

app.Run();
