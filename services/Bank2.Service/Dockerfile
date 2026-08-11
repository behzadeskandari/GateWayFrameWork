FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/Bank2.Service/Bank2.Service.csproj", "services/Bank2.Service/"]
COPY ["services/Bank2.Service.Application/Bank2.Service.Application.csproj", "services/Bank2.Service.Application/"]
COPY ["services/Bank2.Service.Domain/Bank2.Service.Domain.csproj", "services/Bank2.Service.Domain/"]
COPY ["services/Bank2.Service.Contracts/Bank2.Service.Contracts.csproj", "services/Bank2.Service.Contracts/"]
COPY ["services/Bank2.Service.Infrastructure/Bank2.Service.Infrastructure.csproj", "services/Bank2.Service.Infrastructure/"]
COPY ["services/Shared/Banking.Service.Audit.Abstractions/Banking.Service.Audit.Abstractions.csproj", "services/Shared/Banking.Service.Audit.Abstractions/"]
RUN dotnet restore "services/Bank2.Service/Bank2.Service.csproj"
COPY . .
WORKDIR "/src/services/Bank2.Service"
RUN dotnet publish "Bank2.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Bank2.Service.dll"]
