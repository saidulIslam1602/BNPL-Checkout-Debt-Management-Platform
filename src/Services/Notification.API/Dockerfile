# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/Notification.API/Notification.API.csproj", "src/Services/Notification.API/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Events/Events.csproj", "src/Shared/Events/"]
COPY ["src/Shared/Infrastructure/Infrastructure.csproj", "src/Shared/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Notification.API/Notification.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/Notification.API"
RUN dotnet build "Notification.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r notificationapi && useradd -r -g notificationapi notificationapi

# Copy published application
COPY --from=publish /app/publish .

# Create directories for logs and secrets
RUN mkdir -p /app/logs /app/secrets && \
    chown -R notificationapi:notificationapi /app

# Switch to non-root user
USER notificationapi

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Notification.API.dll"]