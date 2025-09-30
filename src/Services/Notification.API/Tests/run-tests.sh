#!/bin/bash

# Notification API Test Runner
echo "🧪 Running Notification API Tests..."

# Set test environment
export ASPNETCORE_ENVIRONMENT=Testing

# Run unit tests
echo "📋 Running Unit Tests..."
dotnet test --filter "Category=Unit" --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"

# Run integration tests (requires Docker for test containers)
echo "🔗 Running Integration Tests..."
if command -v docker &> /dev/null; then
    dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
else
    echo "⚠️  Docker not found. Skipping integration tests."
fi

# Generate test report
echo "📊 Generating Test Report..."
if command -v reportgenerator &> /dev/null; then
    reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
    echo "📈 Coverage report generated at TestResults/CoverageReport/index.html"
else
    echo "⚠️  ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
fi

echo "✅ Test run completed!"