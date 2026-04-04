FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared.Kernel/Shared.Kernel.csproj Shared.Kernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY BookingService.Domain/BookingService.Domain.csproj BookingService.Domain/
COPY BookingService.Application/BookingService.Application.csproj BookingService.Application/
COPY BookingService.Infrastructure/BookingService.Infrastructure.csproj BookingService.Infrastructure/
COPY BookingService.Api/BookingService.Api.csproj BookingService.Api/
RUN dotnet restore BookingService.Api/BookingService.Api.csproj

COPY Shared.Kernel/ Shared.Kernel/
COPY Shared.Contracts/ Shared.Contracts/
COPY Shared.Infrastructure/ Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY BookingService.Domain/ BookingService.Domain/
COPY BookingService.Application/ BookingService.Application/
COPY BookingService.Infrastructure/ BookingService.Infrastructure/
COPY BookingService.Api/ BookingService.Api/
RUN dotnet publish BookingService.Api/BookingService.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookingService.Api.dll"]
