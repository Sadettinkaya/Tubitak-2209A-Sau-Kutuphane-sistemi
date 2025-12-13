# Reservation Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy Shared.Events project first
COPY ["Shared.Events/Shared.Events.csproj", "Shared.Events/"]
COPY ["ReservationService/ReservationService.csproj", "ReservationService/"]

# Restore both projects
RUN dotnet restore "ReservationService/ReservationService.csproj"

# Copy all source files
COPY ["Shared.Events/", "Shared.Events/"]
COPY ["ReservationService/", "ReservationService/"]

# Build
WORKDIR /src/ReservationService
RUN dotnet build "ReservationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReservationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReservationService.dll"]
