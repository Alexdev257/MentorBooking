FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/MBP.ApiGateway.ServiceDefaults.csproj MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY MBP.ApiGateway/MBP.ApiGateway.ApiService/MBP.ApiGateway.ApiService.csproj MBP.ApiGateway/MBP.ApiGateway.ApiService/
RUN dotnet restore MBP.ApiGateway/MBP.ApiGateway.ApiService/MBP.ApiGateway.ApiService.csproj

COPY MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/ MBP.ApiGateway/MBP.ApiGateway.ServiceDefaults/
COPY MBP.ApiGateway/MBP.ApiGateway.ApiService/ MBP.ApiGateway/MBP.ApiGateway.ApiService/
RUN dotnet publish MBP.ApiGateway/MBP.ApiGateway.ApiService/MBP.ApiGateway.ApiService.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MBP.ApiGateway.ApiService.dll"]
