#!/bin/bash

# Riverty BNPL Platform Build Script
# This script helps build and run the platform for development

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    local missing_deps=()
    
    if ! command_exists docker; then
        missing_deps+=("docker")
    fi
    
    if ! command_exists docker-compose; then
        missing_deps+=("docker-compose")
    fi
    
    if ! command_exists dotnet; then
        missing_deps+=("dotnet")
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        print_error "Missing dependencies: ${missing_deps[*]}"
        print_error "Please install the missing dependencies and try again."
        exit 1
    fi
    
    print_success "All prerequisites are installed"
}

# Function to build .NET solution
build_dotnet() {
    print_status "Building .NET solution..."
    
    if [ ! -f "RivertyBNPL.sln" ]; then
        print_error "Solution file not found. Make sure you're in the project root directory."
        exit 1
    fi
    
    dotnet restore RivertyBNPL.sln
    dotnet build RivertyBNPL.sln --configuration Release --no-restore
    
    print_success ".NET solution built successfully"
}

# Function to build Docker images
build_docker() {
    print_status "Building Docker images..."
    
    # Build only the services that have Dockerfiles
    docker-compose build payment-api
    
    print_success "Docker images built successfully"
}

# Function to start services
start_services() {
    print_status "Starting services with Docker Compose..."
    
    # Start infrastructure services first
    docker-compose up -d sqlserver redis
    
    # Wait for SQL Server to be ready
    print_status "Waiting for SQL Server to be ready..."
    sleep 30
    
    # Start application services
    docker-compose up -d payment-api
    
    print_success "Services started successfully"
    
    # Show service status
    docker-compose ps
}

# Function to stop services
stop_services() {
    print_status "Stopping services..."
    docker-compose down
    print_success "Services stopped successfully"
}

# Function to view logs
view_logs() {
    local service=${1:-""}
    
    if [ -n "$service" ]; then
        print_status "Viewing logs for service: $service"
        docker-compose logs -f "$service"
    else
        print_status "Viewing logs for all services"
        docker-compose logs -f
    fi
}

# Function to run database migrations
run_migrations() {
    print_status "Running database migrations..."
    
    # Check if SQL Server is running
    if ! docker-compose ps sqlserver | grep -q "Up"; then
        print_error "SQL Server is not running. Please start services first."
        exit 1
    fi
    
    # Run migrations for Payment API
    cd src/Services/Payment.API
    dotnet ef database update --connection "Server=localhost,1433;Database=RivertyBNPL_Payment;User Id=sa;Password=RivertyBNPL123!;TrustServerCertificate=true;MultipleActiveResultSets=true"
    cd ../../..
    
    print_success "Database migrations completed"
}

# Function to run tests
run_tests() {
    print_status "Running tests..."
    
    # Run unit tests
    dotnet test tests/Unit/ --configuration Release --no-build --verbosity normal
    
    # Run integration tests (if services are running)
    if docker-compose ps | grep -q "Up"; then
        dotnet test tests/Integration/ --configuration Release --no-build --verbosity normal
    else
        print_warning "Services are not running. Skipping integration tests."
    fi
    
    print_success "Tests completed"
}

# Function to clean up
cleanup() {
    print_status "Cleaning up..."
    
    # Stop services
    docker-compose down
    
    # Remove unused Docker resources
    docker system prune -f
    
    # Clean .NET build artifacts
    dotnet clean RivertyBNPL.sln
    
    print_success "Cleanup completed"
}

# Function to show help
show_help() {
    echo "Riverty BNPL Platform Build Script"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  check       Check prerequisites"
    echo "  build       Build .NET solution and Docker images"
    echo "  start       Start all services"
    echo "  stop        Stop all services"
    echo "  restart     Restart all services"
    echo "  logs        View logs for all services"
    echo "  logs <svc>  View logs for specific service"
    echo "  migrate     Run database migrations"
    echo "  test        Run tests"
    echo "  clean       Clean up resources"
    echo "  dev         Start development environment"
    echo "  help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 dev              # Start development environment"
    echo "  $0 logs payment-api # View Payment API logs"
    echo "  $0 test             # Run all tests"
}

# Function to start development environment
start_dev() {
    print_status "Starting development environment..."
    
    check_prerequisites
    build_dotnet
    build_docker
    start_services
    
    # Wait a bit for services to start
    sleep 10
    
    run_migrations
    
    print_success "Development environment is ready!"
    print_status "Access points:"
    echo "  - API Gateway: http://localhost:5000"
    echo "  - Payment API: http://localhost:5001"
    echo "  - Swagger UI: http://localhost:5001"
    echo "  - Seq Logs: http://localhost:5341"
    echo "  - Grafana: http://localhost:3000 (admin/admin)"
    echo ""
    print_status "To view logs: $0 logs"
    print_status "To stop services: $0 stop"
}

# Main script logic
case "${1:-help}" in
    check)
        check_prerequisites
        ;;
    build)
        check_prerequisites
        build_dotnet
        build_docker
        ;;
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        stop_services
        start_services
        ;;
    logs)
        view_logs "$2"
        ;;
    migrate)
        run_migrations
        ;;
    test)
        run_tests
        ;;
    clean)
        cleanup
        ;;
    dev)
        start_dev
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: $1"
        show_help
        exit 1
        ;;
esac