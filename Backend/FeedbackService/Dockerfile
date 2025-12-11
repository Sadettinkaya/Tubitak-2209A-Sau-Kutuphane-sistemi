# Feedback Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FeedbackService.csproj", "."]
RUN dotnet restore "FeedbackService.csproj"
COPY . .
RUN dotnet build "FeedbackService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FeedbackService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create App_Data directory for feedback storage
RUN mkdir -p /app/App_Data

ENTRYPOINT ["dotnet", "FeedbackService.dll"]
