using System.Reflection;
using AuthService.Application.DTOs.Request.Admin;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuthService.Api.Swagger;

/// <summary>
/// Bổ sung field <c>Avatar</c> (binary) vào requestBody multipart khi Swashbuckle không sinh từ <see cref="IFormFile"/>.
/// </summary>
public sealed class AdminMultipartAvatarOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var body = operation.RequestBody;
        if (body?.Content == null || !body.Content.TryGetValue("multipart/form-data", out var media))
            return;

        var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        var method = descriptor?.MethodInfo ?? context.MethodInfo;

        var register = false;
        var update = false;
        foreach (var prm in method.GetParameters())
        {
            var t = prm.ParameterType;
            if (t == typeof(RegisterTeacherByAdminRequest) || t == typeof(RegisterStudentByAdminRequest))
            {
                register = true;
                break;
            }
            if (t == typeof(UpdateTeacherByAdminRequest) || t == typeof(UpdateStudentByAdminRequest))
            {
                update = true;
                break;
            }
        }

        if (!register && !update)
            return;

        var schema = media.Schema;
        OpenApiSchema? target = schema;

        if (schema?.Reference != null)
        {
            var id = schema.Reference.Id;
            if (context.SchemaRepository.Schemas.TryGetValue(id, out var comp))
                target = comp;
        }

        if (target?.Properties == null)
            return;

        if (!target.Properties.ContainsKey("Avatar"))
        {
            target.Properties["Avatar"] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
                Description = "Ảnh đại diện (file upload). Tên part: Avatar.",
            };
        }

        if (register)
        {
            target.Required ??= new HashSet<string>();
            target.Required.Add("Avatar");
        }
    }
}
