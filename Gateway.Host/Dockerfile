FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Gateway.Host/Gateway.Host.csproj", "Gateway.Host/"]
COPY ["Gateway.Framework.Core/Gateway.Framework.Core.csproj", "Gateway.Framework.Core/"]
COPY ["Gateway.Framework.Shared/Gateway.Framework.Shared.csproj", "Gateway.Framework.Shared/"]
COPY ["Gateway.Framework.Infrastructure/Gateway.Framework.Infrastructure.csproj", "Gateway.Framework.Infrastructure/"]
COPY ["Gateway.Framework.Security/Gateway.Framework.Security.csproj", "Gateway.Framework.Security/"]
COPY ["Gateway.Framework.Logging/Gateway.Framework.Logging.csproj", "Gateway.Framework.Logging/"]
COPY ["Gateway.Framework.Monitoring/Gateway.Framework.Monitoring.csproj", "Gateway.Framework.Monitoring/"]
COPY ["Gateway.Framework.Resilience/Gateway.Framework.Resilience.csproj", "Gateway.Framework.Resilience/"]
COPY ["Gateway.Framework.Gateway/Gateway.Framework.Gateway.csproj", "Gateway.Framework.Gateway/"]
RUN dotnet restore "Gateway.Host/Gateway.Host.csproj"
COPY . .
WORKDIR "/src/Gateway.Host"
RUN dotnet publish "Gateway.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Gateway.Host.dll"]
