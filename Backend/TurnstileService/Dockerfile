# Turnstile Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TurnstileService.csproj", "."]
RUN dotnet restore "TurnstileService.csproj"
COPY . .
RUN dotnet build "TurnstileService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TurnstileService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TurnstileService.dll"]
