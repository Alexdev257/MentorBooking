using MBP.ApiGateway.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddMentorBookingServices();

builder.Build().Run();
