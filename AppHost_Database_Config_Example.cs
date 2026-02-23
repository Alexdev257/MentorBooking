// File này chỉ là VÍ DỤ - KHÔNG CẦN thiết lập với setup hiện tại
// Nếu muốn dùng .NET Aspire quản lý database, có thể tham khảo code này

using Aspire.Hosting.PostgreSQL;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume()
    .AddDatabase("auth_db")
    .AddDatabase("booking_db")
    .AddDatabase("meeting_db")
    .AddDatabase("ai_db");

var apiGateway = builder.AddProject<Projects.MBP_ApiGateway_ApiService>("apiservice");

var auth = builder.AddProject<Projects.AuthService_Api>("authservice-api")
    .WithReference(postgres.GetDatabase("auth_db")); 
var email = builder.AddProject<Projects.EmailService_Api>("emailservice-api");

apiGateway.WithReference(auth)
          .WithReference(email);

builder.AddProject<Projects.MBP_ApiGateway_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.AddProject<Projects.BookingService_Api>("bookingservice-api")
    .WithReference(postgres.GetDatabase("booking_db"));

builder.AddProject<Projects.MeetingService_Api>("meetingservice-api")
    .WithReference(postgres.GetDatabase("meeting_db"));

builder.AddProject<Projects.AIService_Api>("aiservice-api")
    .WithReference(postgres.GetDatabase("ai_db"));

builder.Build().Run();


