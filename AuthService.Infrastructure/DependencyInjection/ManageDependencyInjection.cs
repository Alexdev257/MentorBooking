using AuthService.Application.Common;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Application.Services;
using AuthService.Infrastructure.Implements.Helpers;
using AuthService.Infrastructure.Implements.Repositories;
using AuthService.Infrastructure.Implements.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Common.Wrappers;
using Shared.Contracts.Interfaces;
using Shared.Infrastructure.Bus;
using Shared.Infrastructure.Persistence.Interceptors;
using Shared.Infrastructure.Persistence.Repositories;
using Shared.Infrastructure.Swagger;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.DependencyInjection
{
    public static class ManageDependencyInjection
    {

        public static IServiceCollection AddAuthServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDatabase(configuration);
            services.AddScopedInterface();
            services.AddMediatRInfrastructure(configuration);
            services.AddAutoMapper(typeof(AuthServiceMappingProfile));
            services.AddCorsExtentions();
            services.AddJwtAuthentication(configuration);
            services.AddAuthorizationRole();
            services.AddSharedSwaggerGen("Auth Service API");

            AddMessageBusWhenConfigured(services, configuration);
            return services;
        }

       private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString =
        configuration.GetConnectionString("auth-db") ??
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Missing connection string. Expected 'auth-db' (Aspire) or 'DefaultConnection' (local).");

    connectionString = Shared.Infrastructure.Persistence.ConnectionStringHelper.Normalize(connectionString);

    services.AddDbContext<AuthService.Infrastructure.Persistence.ApplicationDbContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(connectionString);
        options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
    });

}


        private static void AddMessageBusWhenConfigured(IServiceCollection services, IConfiguration configuration)
        {
            var rabbitEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", true);
            var rabbitHost = configuration["RabbitMQ:Host"];
            if (rabbitEnabled && !string.IsNullOrWhiteSpace(rabbitHost))
            {
                services.AddMessageBus(configuration);
            }
            else
            {
                services.AddScoped<Shared.Contracts.Interfaces.IMessageProducer, Shared.Infrastructure.Bus.NoOpMessageProducer>();
            }
        }

        private static void AddScopedInterface(this IServiceCollection service)
        {
            service.AddScoped<IAuthUnitOfWork, UnitOfWork>();
            service.AddScoped<IJwtHelper, JwtHelper>();
            service.AddScoped<IBcryptHelper, BcryptHelper>();
            service.AddScoped<IQueryablePager, QueryablePager>();
            service.AddScoped<IAdminAuthService, AdminAuthService>();
            service.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();
            service.AddScoped<IReviewService, ReviewService>();
            service.AddSingleton<IStorageService, FirebaseService>();
        }

        private static void AddMediatRInfrastructure(this IServiceCollection service, IConfiguration config)
        {
            var applicationAssembly = Assembly.Load("AuthService.Application");

            service.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(applicationAssembly);
            });
        }

        private static void AddCorsExtentions(this IServiceCollection service)
        {
            service.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
        }

        private static void AddJwtAuthentication(this IServiceCollection service, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

            service.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSettings["Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                                context.Response.Headers["Token-Expired"] = "true";
                            return Task.CompletedTask;
                        },
                        // 1. X? lý khi ch?a ??ng nh?p ho?c Token sai (401 Unauthorized)
                        OnChallenge = context =>
                        {
                            // Ng?n ch?n hành vi m?c ??nh (tr? v? r?ng)
                            context.HandleResponse();

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            string errorMessage = "B?n ch?a ??ng nh?p. Vui lòng cung c?p Token h?p l?.";
                            string errorCode = "UNAUTHORIZED";

                            // 2. Phân tích chi ti?t nguyên nhân l?i
                            if (context.AuthenticateFailure != null)
                            {
                                if (context.AuthenticateFailure is SecurityTokenExpiredException)
                                {
                                    errorMessage = "Phiên ??ng nh?p ?ã h?t h?n. Vui lòng ??ng nh?p l?i ho?c làm m?i Token.";
                                    errorCode = "TOKEN_EXPIRED";
                                }
                                else if (context.AuthenticateFailure is SecurityTokenInvalidSignatureException)
                                {
                                    errorMessage = "Token không h?p l? (Ch? ký b? sai).";
                                    errorCode = "INVALID_SIGNATURE";
                                }
                                else
                                {
                                    errorMessage = "Token không h?p l?. Vui lòng ??ng nh?p l?i.";
                                    errorCode = "INVALID_TOKEN";
                                }
                            }
                            // Tr??ng h?p không có header Authorization
                            else if (!context.Request.Headers.ContainsKey("Authorization"))
                            {
                                errorMessage = "Không tìm th?y thông tin xác th?c (Missing Authorization Header).";
                                errorCode = "MISSING_TOKEN";
                            }

                            var response = new CommonResponse<object>
                            {
                                IsSuccess = false,
                                Message = errorMessage,
                                Data = new { ErrorCode = errorCode } // G?i kèm mã l?i ?? Frontend d? b?t
                            };

                            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        },

                        // 2. X? lý khi ?ã ??ng nh?p nh?ng không ?? quy?n (403 Forbidden)
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            var response = new CommonResponse<object>
                            {
                                IsSuccess = false,
                                Message = "You are not allowed to access this endpoint.",
                                Data = null,
                            };

                            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        }
                    };
                });
        }

        private static void AddAuthorizationRole(this IServiceCollection service)
        {
            ////1 admin
            ////2 teacher
            ////3 sttudent
            //service.AddAuthorization(options =>
            //{
            //    options.AddPolicy("AdminOnly", policy => { policy.RequireClaim("RoleId", "1".ToLower()); });

            //    options.AddPolicy("TeacherOnly", policy => { policy.RequireClaim("RoleId", "2".ToLower()); });

            //    options.AddPolicy("StudentOnly", policy => { policy.RequireClaim("RoleId", "3".ToLower()); });

            //    options.AddPolicy("AdminOrTeacher", policy =>
            //        policy.RequireAssertion(context =>
            //        {
            //            var roleClaim = context.User.FindFirst(c => c.Type == "RoleId")?.Value;
            //            //return roleClaim != "User";
            //            return roleClaim == "1".ToLower() || roleClaim == "2".ToLower();
            //        }));

            //    options.AddPolicy("AdminOrStudent", policy =>
            //        policy.RequireAssertion(context =>
            //        {
            //            var roleClaim = context.User.FindFirst(c => c.Type == "RoleId")?.Value;
            //            //return roleClaim != "User";
            //            return roleClaim == "1".ToLower() || roleClaim == "3".ToLower();
            //        }));

            //    options.AddPolicy("TeacherOrStudent", policy =>
            //        policy.RequireAssertion(context =>
            //        {
            //            var roleClaim = context.User.FindFirst(c => c.Type == "RoleId")?.Value;
            //            //return roleClaim != "User";
            //            return roleClaim == "2".ToLower() || roleClaim == "3".ToLower();
            //        }));

            //    options.AddPolicy("AllRole", policy =>
            //        policy.RequireAssertion(context =>
            //        {
            //            var roleClaim = context.User.FindFirst(c => c.Type == "RoleId")?.Value;
            //            //return roleClaim != "Admin";
            //            return roleClaim == "1".ToLower() || roleClaim == "2".ToLower() || roleClaim == "3".ToLower();
            //        }));
            //});
        }
    }
}
