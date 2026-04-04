FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared.Kernel/Shared.Kernel.csproj Shared.Kernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY MeetingService.Domain/MeetingService.Domain.csproj MeetingService.Domain/
COPY MeetingService.Application/MeetingService.Application.csproj MeetingService.Application/
COPY MeetingService.Infrastructure/MeetingService.Infrastructure.csproj MeetingService.Infrastructure/
COPY MeetingService.Api/MeetingService.Api.csproj MeetingService.Api/
RUN dotnet restore MeetingService.Api/MeetingService.Api.csproj

COPY Shared.Kernel/ Shared.Kernel/
COPY Shared.Contracts/ Shared.Contracts/
COPY Shared.Infrastructure/ Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY MeetingService.Domain/ MeetingService.Domain/
COPY MeetingService.Application/ MeetingService.Application/
COPY MeetingService.Infrastructure/ MeetingService.Infrastructure/
COPY MeetingService.Api/ MeetingService.Api/
RUN dotnet publish MeetingService.Api/MeetingService.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MeetingService.Api.dll"]
