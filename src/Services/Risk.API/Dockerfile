# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/Risk.API/Risk.API.csproj", "src/Services/Risk.API/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Events/Events.csproj", "src/Shared/Events/"]
COPY ["src/Shared/Infrastructure/Infrastructure.csproj", "src/Shared/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Risk.API/Risk.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/Risk.API"
RUN dotnet build "Risk.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Risk.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - create the runtime image
FROM base AS final
WORKDIR /app

# Create directory for ML models
RUN mkdir -p /app/models

# Create directory for logs
RUN mkdir -p /app/logs

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "RivertyBNPL.Risk.API.dll"]