# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Services/Settlement.API/Settlement.API.csproj", "src/Services/Settlement.API/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Events/Events.csproj", "src/Shared/Events/"]
COPY ["src/Shared/Infrastructure/Infrastructure.csproj", "src/Shared/Infrastructure/"]

RUN dotnet restore "src/Services/Settlement.API/Settlement.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Services/Settlement.API"
RUN dotnet build "Settlement.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Settlement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Settlement.API.dll"]