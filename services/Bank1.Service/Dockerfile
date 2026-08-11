FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/Bank1.Service/Bank1.Service.csproj", "services/Bank1.Service/"]
COPY ["services/Bank1.Service.Application/Bank1.Service.Application.csproj", "services/Bank1.Service.Application/"]
COPY ["services/Bank1.Service.Infrastructure/Bank1.Service.Infrastructure.csproj", "services/Bank1.Service.Infrastructure/"]
COPY ["services/Bank1.Service.Domain/Bank1.Service.Domain.csproj", "services/Bank1.Service.Domain/"]
COPY ["services/Bank1.Service.Contracts/Bank1.Service.Contracts.csproj", "services/Bank1.Service.Contracts/"]
COPY ["services/Shared/Banking.Service.Audit.Abstractions/Banking.Service.Audit.Abstractions.csproj", "services/Shared/Banking.Service.Audit.Abstractions/"]
RUN dotnet restore "services/Bank1.Service/Bank1.Service.csproj"
COPY . .
WORKDIR "/src/services/Bank1.Service"
RUN dotnet publish "Bank1.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Bank1.Service.dll"]
