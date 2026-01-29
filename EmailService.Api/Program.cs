
using EmailService.Infrastructure.Consumers;
using EmailService.Infrastructure.Services;
using MassTransit;

namespace EmailService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddScoped<EmailSender>();

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<SendOtpRegisterConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h => {
                    h.Username(builder.Configuration["RabbitMQ:Username"]!);
                    h.Password(builder.Configuration["RabbitMQ:Password"]!);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        app.MapGet("/", () => "Email Service is running...");

        app.Run();
    }
}
