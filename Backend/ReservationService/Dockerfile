# Reservation Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ReservationService.csproj", "."]
RUN dotnet restore "ReservationService.csproj"
COPY . .
RUN dotnet build "ReservationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReservationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReservationService.dll"]
