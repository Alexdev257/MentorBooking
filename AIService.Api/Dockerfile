FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/* \
    && ffmpeg -version

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared.Kernel/Shared.Kernel.csproj Shared.Kernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY AIService.Domain/AIService.Domain.csproj AIService.Domain/
COPY AIService.Application/AIService.Application.csproj AIService.Application/
COPY AIService.Infrastructure/AIService.Infrastructure.csproj AIService.Infrastructure/
COPY AIService.Api/AIService.Api.csproj AIService.Api/

RUN dotnet restore AIService.Api/AIService.Api.csproj

COPY Shared.Kernel/ Shared.Kernel/
COPY Shared.Contracts/ Shared.Contracts/
COPY Shared.Infrastructure/ Shared.Infrastructure/
COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY AIService.Domain/ AIService.Domain/
COPY AIService.Application/ AIService.Application/
COPY AIService.Infrastructure/ AIService.Infrastructure/
COPY AIService.Api/ AIService.Api/

RUN dotnet publish AIService.Api/AIService.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIService.Api.dll"]