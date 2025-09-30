# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/Gateway/API.Gateway/API.Gateway.csproj", "src/Gateway/API.Gateway/"]
RUN dotnet restore "src/Gateway/API.Gateway/API.Gateway.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Gateway/API.Gateway"
RUN dotnet build "API.Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "API.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "API.Gateway.dll"]