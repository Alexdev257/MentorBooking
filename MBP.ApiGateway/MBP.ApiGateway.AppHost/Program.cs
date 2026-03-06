var builder = DistributedApplication.CreateBuilder(args);

//var postgresPassword = builder.AddParameter("postgres-password", "12345");
//var postgres = builder.AddPostgres("postgres", port: 5432, password: postgresPassword)
//    .WithDataVolume();

//var authDb = postgres.AddDatabase("auth-db", "auth_db");
//var bookingDb = postgres.AddDatabase("booking-db", "booking_db");
//var meetingDb = postgres.AddDatabase("meeting-db", "meeting_db");
//var aiDb = postgres.AddDatabase("ai-db", "ai_db");

//var apiGateway = builder.AddProject<Projects.MBP_ApiGateway_ApiService>("apiservice");

//var auth = builder.AddProject<Projects.AuthService_Api>("authservice-api")
//    .WithReference(authDb);


var authDb = builder.AddConnectionString("auth-db");
var bookingDb = builder.AddConnectionString("booking-db");
var meetingDb = builder.AddConnectionString("meeting-db");
var aiDb = builder.AddConnectionString("ai-db");

var apiGateway = builder.AddProject<Projects.MBP_ApiGateway_ApiService>("apiservice");

var auth = builder.AddProject<Projects.AuthService_Api>("authservice-api")
    .WithReference(authDb);

var email = builder.AddProject<Projects.EmailService_Api>("emailservice-api");

apiGateway.WithReference(auth)
          .WithReference(email);

builder.AddProject<Projects.MBP_ApiGateway_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.AddProject<Projects.BookingService_Api>("bookingservice-api")
    .WithReference(bookingDb);

builder.AddProject<Projects.MeetingService_Api>("meetingservice-api")
    .WithReference(meetingDb);

builder.AddProject<Projects.AIService_Api>("aiservice-api")
    .WithReference(aiDb);

builder.Build().Run();
