
using EmailService.Infrastructure.Consumers;
using EmailService.Infrastructure.Services;
using MassTransit;
using Shared.Infrastructure.Bus;

namespace EmailService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddHttpClient<EmailSender>((_, http) =>
        {
            http.Timeout = TimeSpan.FromMinutes(2);
        });

        builder.Services.AddMessageBus(builder.Configuration, typeof(SendOtpRegisterConsumer).Assembly);

        var app = builder.Build();

        app.MapDefaultEndpoints();

        app.MapGet("/", () => "Email Service is running...");

        app.Run();
    }
}
