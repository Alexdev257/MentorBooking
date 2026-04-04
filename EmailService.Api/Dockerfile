FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared.Kernel/Shared.Kernel.csproj Shared.Kernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY EmailService.Infrastructure/EmailService.Infrastructure.csproj EmailService.Infrastructure/
COPY EmailService.Api/EmailService.Api.csproj EmailService.Api/
RUN dotnet restore EmailService.Api/EmailService.Api.csproj

COPY Shared.Kernel/ Shared.Kernel/
COPY Shared.Contracts/ Shared.Contracts/
COPY Shared.Infrastructure/ Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY EmailService.Infrastructure/ EmailService.Infrastructure/
COPY EmailService.Api/ EmailService.Api/
RUN dotnet publish EmailService.Api/EmailService.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmailService.Api.dll"]
