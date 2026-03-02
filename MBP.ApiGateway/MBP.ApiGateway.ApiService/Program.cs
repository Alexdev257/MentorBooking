
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // URL Frontend
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // B?t bu?c cho SignalR
    });
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

    c.CustomSchemaIds(type => type.FullName);
});


//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", options =>
//    {
//        options.Authority = "https://authservice-api";
//        options.RequireHttpsMetadata = false;
//        options.TokenValidationParameters.ValidateAudience = false;
//    });

//builder.Services.AddAuthorization();

//builder.Services.AddCors(o =>
//{
//    o.AddDefaultPolicy(p =>
//        p.AllowAnyOrigin()
//         .AllowAnyMethod()
//         .AllowAnyHeader());
//});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Khai báo Endpoint ?? UI bi?t load file JSON nào
        options.SwaggerEndpoint("/auth-service/swagger/v1/swagger.json", "Auth Service API");
        options.SwaggerEndpoint("/ai-service/swagger/v1/swagger.json", "AI Service API");
        options.SwaggerEndpoint("/booking-service/swagger/v1/swagger.json", "Booking Service API");
        options.SwaggerEndpoint("/meeting-service/swagger/v1/swagger.json", "Meeting Service API");

        options.ConfigObject.AdditionalItems["syntaxHighlight"] = false;
    });
}


//app.UseAuthentication();
//app.UseAuthorization();
app.UseCors("AllowAll");
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
