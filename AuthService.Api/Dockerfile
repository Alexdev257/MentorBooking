FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared.Kernel/Shared.Kernel.csproj Shared.Kernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY AuthService.Domain/AuthService.Domain.csproj AuthService.Domain/
COPY AuthService.Application/AuthService.Application.csproj AuthService.Application/
COPY AuthService.Infrastructure/AuthService.Infrastructure.csproj AuthService.Infrastructure/
COPY AuthService.Api/AuthService.Api.csproj AuthService.Api/
RUN dotnet restore AuthService.Api/AuthService.Api.csproj

COPY Shared.Kernel/ Shared.Kernel/
COPY Shared.Contracts/ Shared.Contracts/
COPY Shared.Infrastructure/ Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY AuthService.Domain/ AuthService.Domain/
COPY AuthService.Application/ AuthService.Application/
COPY AuthService.Infrastructure/ AuthService.Infrastructure/
COPY AuthService.Api/ AuthService.Api/
RUN dotnet publish AuthService.Api/AuthService.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
