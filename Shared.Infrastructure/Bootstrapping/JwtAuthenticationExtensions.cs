using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Common.Wrappers;
using Shared.Infrastructure.Services;
using System.Text.Json;

namespace Shared.Infrastructure.Bootstrapping;

public static class JwtAuthenticationExtensions
{
    public static IHostApplicationBuilder AddJwtAuthentication(this IHostApplicationBuilder builder)
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtBearerConfigurationOptions.SectionName)
            .Get<JwtBearerConfigurationOptions>();

        if (jwtOptions == null || string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException(
                $"JWT configuration section '{JwtBearerConfigurationOptions.SectionName}' with SecretKey is required in appsettings.");

        builder.Services.Configure<JwtBearerConfigurationOptions>(
            builder.Configuration.GetSection(JwtBearerConfigurationOptions.SectionName));

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddHttpContextAccessor();

        var key = Encoding.ASCII.GetBytes(jwtOptions.SecretKey);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = jwtOptions.ValidateIssuer,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = jwtOptions.ValidateAudience,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = jwtOptions.ValidateLifetime,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                        context.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var response = new CommonResponse<object>
                    {
                        IsSuccess = false,
                        Message = "You are not authorized to access this resource.",
                        Data = new { ErrorCode = "UNAUTHORIZED" }
                    };
                    return context.Response.WriteAsync(JsonSerializer.Serialize(response));
                },
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

        builder.Services.AddAuthorization();
        return builder;
    }
}
