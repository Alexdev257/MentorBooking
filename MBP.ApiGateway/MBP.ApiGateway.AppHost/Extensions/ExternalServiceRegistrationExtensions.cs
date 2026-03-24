using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace MBP.ApiGateway.AppHost.Extensions;
public static class ExternalServiceRegistrationExtensions
{
    public static IDistributedApplicationBuilder AddMentorBookingServices(this IDistributedApplicationBuilder builder)
    {
        var postgresPassword = builder.AddParameter("postgres-password", "12345");
        var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 15432)
            .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(15050), "PgAdmin"); // pgAdmin: http://localhost:15050 (tránh conflict 5050)

        var authDb = postgres.AddDatabase("auth-db", "auth_db");
        var bookingDb = postgres.AddDatabase("booking-db", "booking_db");
        var meetingDb = postgres.AddDatabase("meeting-db", "meeting_db");
        var aiDb = postgres.AddDatabase("ai-db", "ai_db");

        var authService = builder.AddProject<Projects.AuthService_Api>("authservice-api")
            .WithReference(authDb)
            .WaitFor(postgres);

        var bookingService = builder.AddProject<Projects.BookingService_Api>("bookingservice-api")
            .WithReference(bookingDb)
            .WithReference(authService)
            .WaitFor(postgres);

        var meetingService = builder.AddProject<Projects.MeetingService_Api>("meetingservice-api")
            .WithReference(meetingDb)
            .WaitFor(postgres);

        var aiService = builder.AddProject<Projects.AIService_Api>("aiservice-api")
            .WithReference(aiDb)
            .WaitFor(postgres);

        var emailService = builder.AddProject<Projects.EmailService_Api>("emailservice-api");

        // HTTP 5000 / HTTPS 5001: cố định để FE/dev tools gọi ổn định (tránh port random mỗi lần chạy Aspire)
        var apiGateway = builder.AddProject<Projects.MBP_ApiGateway_ApiService>("apiservice")
            .WithHttpEndpoint(port: 5000)
            .WithHttpsEndpoint(port: 5001)
            .WithReference(authService)
            .WithReference(bookingService)
            .WithReference(meetingService)
            .WithReference(aiService)
            .WithReference(emailService);

        builder.AddProject<Projects.MBP_ApiGateway_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(apiGateway)
            .WaitFor(apiGateway);

        return builder;
    }
}
