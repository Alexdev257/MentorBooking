using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Infrastructure.Swagger
{
    public static class SwaggerExtensions
    {
        public static void AddSharedSwaggerGen(this IServiceCollection services, string apiTitle, string apiVersion = "v1")
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(apiVersion, new OpenApiInfo
                {
                    Title = apiTitle,
                    Version = apiVersion
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập token JWT vào bên dưới (Không cần gõ 'Bearer '):",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });

                c.CustomSchemaIds(type => type.FullName ?? type.Name ?? "Schema_" + type.Name);
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                // IFormFile / file upload: tránh lỗi "FromForm attribute used with IFormFile"
                c.OperationFilter<FormFileOperationFilter>();
            });
            services.AddSingleton<IApiDescriptionProvider, FormFileApiDescriptionProvider>();
        }
    }

    /// <summary>
    /// Xóa tham số IFormFile khỏi ApiDescription để SwaggerGen không ném khi generate (sau đó FormFileOperationFilter thêm RequestBody).
    /// </summary>
    public class FormFileApiDescriptionProvider : IApiDescriptionProvider
    {
        public int Order => 1000;

        public void OnProvidersExecuting(ApiDescriptionProviderContext context) { }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            foreach (var description in context.Results)
            {
                for (var i = description.ParameterDescriptions.Count - 1; i >= 0; i--)
                {
                    var p = description.ParameterDescriptions[i];
                    if (p.Type == typeof(IFormFile) || p.Type == typeof(IFormFileCollection))
                        description.ParameterDescriptions.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Document multipart/form-data cho action có IFormFile (tránh SwaggerGeneratorException).
    /// </summary>
    public class FormFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
        {
            var hasFormFile = context.MethodInfo?.GetParameters()
                .Any(pi => pi.ParameterType == typeof(IFormFile) || pi.ParameterType == typeof(IFormFileCollection)) ?? false;
            if (!hasFormFile) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema { Type = "string", Format = "binary", Description = "Audio/Video file" },
                                ["title"] = new OpenApiSchema { Type = "string", Nullable = true, Description = "Optional title" },
                                ["sourceType"] = new OpenApiSchema { Type = "integer", Default = new Microsoft.OpenApi.Any.OpenApiInteger(1), Description = "Source type (default 1)" }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
            operation.Parameters?.Clear();
        }
    }
}
