using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Bus
{
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration, params System.Reflection.Assembly[] consumerAssemblies)
        {
            services.AddMassTransit(x =>
            {
                if (consumerAssemblies != null && consumerAssemblies.Length > 0)
                {
                    x.AddConsumers(consumerAssemblies);
                }

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"]!);
                        h.Password(configuration["RabbitMQ:Password"]!);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddScoped<IMessageProducer, MassTransitProducer>();
            return services;
        }
    }
}
