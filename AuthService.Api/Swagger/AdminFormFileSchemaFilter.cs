using AuthService.Application.DTOs.Request.Admin;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuthService.Api.Swagger;

/// <summary>
/// Swashbuckle thường bỏ qua <see cref="IFormFile"/> trong DTO <c>[FromForm]</c>, nên Swagger UI không hiện ô upload.
/// Filter này thêm property <c>Avatar</c> (string/binary) vào schema tương ứng.
/// </summary>
public sealed class AdminFormFileSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RegisterTeacherByAdminRequest) ||
            context.Type == typeof(RegisterStudentByAdminRequest))
        {
            EnsureAvatar(schema, required: true);
        }
        else if (context.Type == typeof(UpdateTeacherByAdminRequest) ||
                 context.Type == typeof(UpdateStudentByAdminRequest))
        {
            EnsureAvatar(schema, required: false);
        }
    }

    private static void EnsureAvatar(OpenApiSchema schema, bool required)
    {
        schema.Properties ??= new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        schema.Properties["Avatar"] = new OpenApiSchema
        {
            Type = "string",
            Format = "binary",
            Description = "Ảnh đại diện (multipart file). Tên field: Avatar.",
        };

        if (!required)
            return;

        schema.Required ??= new HashSet<string>();
        schema.Required.Add("Avatar");
    }
}
