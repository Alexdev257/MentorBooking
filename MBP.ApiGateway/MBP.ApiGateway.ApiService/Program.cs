using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Khi chạy qua Aspire Dashboard, dùng URL do AppHost inject thay vì port cố định trong appsettings
// Aspire 9 inject service URL theo format: services__{name}__{scheme}__0
string? GetAspireServiceUrl(string name) =>
    builder.Configuration[$"services__{name}__https__0"] ??
    builder.Configuration[$"services__{name}__http__0"];

var clusterOverrides = new Dictionary<string, string?>();
var aiUrl      = GetAspireServiceUrl("aiservice-api");
var authUrl    = GetAspireServiceUrl("authservice-api");
var bookingUrl = GetAspireServiceUrl("bookingservice-api");
var meetingUrl = GetAspireServiceUrl("meetingservice-api");
var emailUrl   = GetAspireServiceUrl("emailservice-api");
if (!string.IsNullOrEmpty(aiUrl))      clusterOverrides["ReverseProxy:Clusters:ai-cluster:Destinations:destination1:Address"]      = aiUrl;
if (!string.IsNullOrEmpty(authUrl))    clusterOverrides["ReverseProxy:Clusters:auth-cluster:Destinations:destination1:Address"]    = authUrl;
if (!string.IsNullOrEmpty(bookingUrl)) clusterOverrides["ReverseProxy:Clusters:booking-cluster:Destinations:destination1:Address"] = bookingUrl;
if (!string.IsNullOrEmpty(meetingUrl)) clusterOverrides["ReverseProxy:Clusters:meeting-cluster:Destinations:destination1:Address"] = meetingUrl;
if (!string.IsNullOrEmpty(emailUrl))   clusterOverrides["ReverseProxy:Clusters:email-cluster:Destinations:destination1:Address"]   = emailUrl;
if (clusterOverrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(clusterOverrides);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    // --- KHAI BÁO CÁC DOCUMENT ---
    c.SwaggerDoc("auth", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });
    c.SwaggerDoc("ai", new OpenApiInfo { Title = "AI Service API", Version = "v1" });
    c.SwaggerDoc("booking", new OpenApiInfo { Title = "Booking Service API", Version = "v1" });
    c.SwaggerDoc("meeting", new OpenApiInfo { Title = "Meeting Service API", Version = "v1" });

    // --- C?U HÌNH SECURITY (AUTHORIZE BUTTON) ---
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nh?p token theo ??nh d?ng: Bearer {token}",
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

    c.CustomSchemaIds(type => type.FullName ?? type.Name ?? "Schema");
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});


//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", options =>
//    {
//        options.Authority = "https://authservice-api";
//        options.RequireHttpsMetadata = false;
//        options.TokenValidationParameters.ValidateAudience = false;
//    });

//builder.Services.AddAuthorization();

var app = builder.Build();

// Zoom recording/transcript webhooks can have larger JSON payloads (many recording_files).
// Ensure the reverse proxy doesn't reject the request body before it reaches downstream services.
app.Use(async (context, next) =>
{
    var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (feature is { IsReadOnly: false })
    {
        // 50 MB should be plenty for Zoom webhook JSON while still bounded.
        feature.MaxRequestBodySize = 50 * 1024 * 1024;
    }

    // Help debug "Zoom recording.* webhook not arriving":
    // log only for Zoom webhook endpoint so we can see if requests reach the gateway and what status is returned.
    var isZoomWebhook = context.Request.Path.StartsWithSegments("/api/zoom/wh", StringComparison.OrdinalIgnoreCase);
    if (isZoomWebhook)
    {
        Console.WriteLine($"[ApiGateway] Incoming Zoom webhook: {context.Request.Method} {context.Request.Path}");
    }

    await next();

    if (isZoomWebhook)
    {
        Console.WriteLine($"[ApiGateway] Zoom webhook response: HTTP {context.Response.StatusCode}");
    }
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/auth-service/swagger/v1/swagger.json", "Auth Service API");
    options.SwaggerEndpoint("/ai-service/swagger/v1/swagger.json", "AI Service API");
    options.SwaggerEndpoint("/booking-service/swagger/v1/swagger.json", "Booking Service API");
    options.SwaggerEndpoint("/meeting-service/swagger/v1/swagger.json", "Meeting Service API");

    options.ConfigObject.AdditionalItems["syntaxHighlight"] = false;
});


//app.UseAuthentication();
//app.UseAuthorization();
app.UseCors("AllowAll");
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
