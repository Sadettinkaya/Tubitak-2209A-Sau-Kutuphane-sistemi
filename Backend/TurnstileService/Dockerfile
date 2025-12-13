# Turnstile Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy Shared.Events project first
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]
COPY ["TurnstileService/TurnstileService.csproj", "TurnstileService/"]

# Restore both projects
RUN dotnet restore "TurnstileService/TurnstileService.csproj"

# Copy all source files
COPY ["Shared.Events/", "Shared.Events/"]
COPY ["TurnstileService/", "TurnstileService/"]

# Build
WORKDIR /src/TurnstileService
RUN dotnet build "TurnstileService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TurnstileService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TurnstileService.dll"]
