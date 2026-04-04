using AIService.Api.Bootstrapping;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();

var app = builder.Build();

app.UseApplicationServices();

app.Run();
