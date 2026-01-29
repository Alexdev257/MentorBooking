var builder = DistributedApplication.CreateBuilder(args);

var apiGateway = builder.AddProject<Projects.MBP_ApiGateway_ApiService>("apiservice");

var auth = builder.AddProject<Projects.AuthService_Api>("authservice-api");
var email = builder.AddProject<Projects.EmailService_Api>("emailservice-api");

apiGateway.WithReference(auth)
          .WithReference(email);

builder.AddProject<Projects.MBP_ApiGateway_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();
