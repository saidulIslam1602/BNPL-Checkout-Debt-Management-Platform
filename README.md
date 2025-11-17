# BNPL Checkout Debt Management Platform

A comprehensive Buy Now Pay Later (BNPL) platform built with modern microservices architecture, featuring payment processing, risk assessment, and real-time notification systems.

## Overview

This platform provides a complete BNPL solution with the following components:

### Core Services
- **Payment Processing**: Multi-gateway payment handling (Stripe, Adyen, Nets, Vipps)
- **Risk Assessment**: Credit scoring and fraud detection
- **Settlement Management**: Automated merchant settlement processing
- **Notification System**: Multi-channel notifications (Email, SMS, Push)
- **Real-time Communication**: WebSocket-based event streaming

### Architecture
- Microservices-based .NET 8 backend
- Node.js real-time service with Socket.IO
- Angular 17 and Vue.js 3 frontend applications
- Azure Functions for background processing
- API Gateway with Ocelot
- Comprehensive monitoring and logging

## Technology Stack

### Backend Services
- **.NET 8.0**: Primary framework for all APIs
- **Entity Framework Core**: ORM for database operations
- **MediatR**: CQRS pattern implementation
- **FluentValidation**: Request validation
- **AutoMapper**: Object mapping
- **Serilog**: Structured logging

### Frontend Applications
- **Angular 17**: Consumer and Merchant portals
- **Vue.js 3**: Admin portal
- **Knockout.js**: Legacy portal (backward compatibility)
- **TypeScript**: Type-safe development
- **Bootstrap 5**: UI framework

### Real-time Services
- **Node.js 18+**: WebSocket server
- **Socket.IO**: Real-time bidirectional communication
- **Express**: HTTP server

### Background Processing
- **Azure Functions v4**: Serverless background jobs
  - Payment Processor
  - Payment Collection
  - Notification Scheduler

### Databases & Caching
- **SQL Server 2022**: Primary transactional database
- **MongoDB 7**: Document storage for logs and events
- **Redis 7**: Caching and session management

### Infrastructure
- **Docker & Docker Compose**: Containerization
- **Kubernetes**: Orchestration (production)
- **Azure**: Cloud infrastructure
- **Terraform**: Infrastructure as Code
- **Nginx**: Reverse proxy and load balancing

### Monitoring & Logging
- **Prometheus**: Metrics collection
- **Grafana**: Dashboards and visualization
- **Elasticsearch**: Log storage and search
- **Kibana**: Log analysis
- **Application Insights**: Application monitoring

## Prerequisites

- **.NET 8.0 SDK** or higher
- **Node.js 18+** and npm
- **Docker** and **Docker Compose**
- **SQL Server 2022** (or Docker container)
- **Redis 7** (or Docker container)
- **MongoDB 7** (or Docker container)
- **Git**

## Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/saidulIslam1602/BNPL-Checkout-Debt-Management-Platform.git
cd BNPL-Checkout-Debt-Management-Platform
```

### 2. Environment Setup
```bash
# Copy environment template
cp .env.example .env

# Edit with your configuration
nano .env
```

### 3. Start Infrastructure Services
```bash
# Start databases and supporting services
docker-compose up -d sqlserver redis mongodb elasticsearch kibana grafana prometheus
```

### 4. Build and Run
```bash
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Run specific service (example: Payment API)
cd src/Services/Payment.API
dotnet run
```

### 5. Access Services
- **Payment API**: http://localhost:5000/swagger
- **Risk API**: http://localhost:5001/swagger
- **Settlement API**: http://localhost:5002/swagger
- **Notification API**: http://localhost:5003/swagger
- **Grafana**: http://localhost:3000 (admin/admin)
- **Kibana**: http://localhost:5601

## Project Structure
## Project Structure

```
BNPL-Checkout-Debt-Management-Platform/
├── .github/
│   └── workflows/           # CI/CD pipelines
├── database/
│   └── init/               # Database initialization scripts
├── docs/                   # Documentation
├── infrastructure/
│   └── terraform/          # Infrastructure as Code
├── k8s/                    # Kubernetes manifests
├── monitoring/             # Prometheus & Grafana configs
├── scripts/                # Build and deployment scripts
├── src/
│   ├── Functions/          # Azure Functions
│   │   ├── NotificationScheduler/
│   │   ├── PaymentCollection/
│   │   └── PaymentProcessor/
│   ├── Gateway/
│   │   └── API.Gateway/    # Ocelot API Gateway
│   ├── Services/           # Microservices
│   │   ├── Notification.API/
│   │   ├── Payment.API/
│   │   ├── RealTime.Node.API/
│   │   ├── Risk.API/
│   │   └── Settlement.API/
│   ├── Shared/             # Shared libraries
│   │   ├── Common/
│   │   ├── Events/
│   │   └── Infrastructure/
│   └── Web/                # Frontend applications
│       ├── AdminPortal/    # Vue.js 3
│       ├── ConsumerPortal/ # Angular 17
│       ├── LegacyPortal/   # Knockout.js
│       └── MerchantPortal/ # Angular 17
└── tests/                  # Unit and integration tests
```

## Development

### Building the Solution
```bash
# Build all projects
dotnet build

# Build specific service
cd src/Services/Payment.API
dotnet build

# Run tests
dotnet test
```

### Running Services

#### Backend Services
```bash
# Payment API
cd src/Services/Payment.API
dotnet run

# Risk API
cd src/Services/Risk.API
dotnet run

# Settlement API
cd src/Services/Settlement.API
dotnet run

# Notification API
cd src/Services/Notification.API
dotnet run
```

#### Real-time Service
```bash
cd src/Services/RealTime.Node.API
npm install
npm start
```

#### Frontend Applications
```bash
# Consumer Portal (Angular 17)
cd src/Web/ConsumerPortal
npm install
npm start

# Merchant Portal (Angular 17)
cd src/Web/MerchantPortal
npm install
npm start

# Admin Portal (Vue.js 3)
cd src/Web/AdminPortal
npm install
npm run dev
```

### Docker Deployment
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Configuration

### Database Connection Strings
Configure in `appsettings.json` or environment variables:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourCompanyBNPL_Payment;..."
  }
}
```

### Environment Variables
Key environment variables (see `.env.example` for complete list):
- `ASPNETCORE_ENVIRONMENT`: Development/Staging/Production
- `ConnectionStrings__DefaultConnection`: SQL Server connection
- `Redis__Configuration`: Redis connection string
- `MongoDB__ConnectionString`: MongoDB connection
- `JWT__SecretKey`: JWT signing key
- `JWT__Issuer`: Token issuer
- `JWT__Audience`: Token audience

## API Documentation

All APIs include Swagger/OpenAPI documentation:
- **Payment API**: `http://localhost:5000/swagger`
- **Risk API**: `http://localhost:5001/swagger`
- **Settlement API**: `http://localhost:5002/swagger`
- **Notification API**: `http://localhost:5003/swagger`

## Testing

### Unit Tests
```bash
# Run all unit tests
dotnet test tests/Unit/

# Run specific test project
dotnet test tests/Unit/Payment.API.Tests/
```

### Integration Tests
```bash
# Run integration tests
dotnet test tests/Integration/
```

## Monitoring & Logging

### Accessing Monitoring Tools
- **Grafana**: http://localhost:3000 (default: admin/admin)
- **Prometheus**: http://localhost:9090
- **Kibana**: http://localhost:5601
- **Elasticsearch**: http://localhost:9200

### Application Logs
Logs are written to:
- Console (structured JSON)
- File system (`logs/` directory)
- Elasticsearch (when configured)

## Deployment

### Azure Deployment
```bash
# Deploy infrastructure with Terraform
cd infrastructure/terraform
terraform init
terraform plan
terraform apply

# Deploy Azure Functions
./scripts/deploy-functions.sh
```

### Kubernetes Deployment
```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n bnpl-platform
kubectl get services -n bnpl-platform
```

## Security Considerations

- All API endpoints require authentication (JWT tokens)
- Sensitive data encrypted at rest and in transit
- Role-based access control (RBAC) implemented
- Input validation on all endpoints
- Rate limiting configured
- SQL injection prevention through parameterized queries
- XSS protection enabled
- CORS configured for known origins

## Known Limitations

1. **Authentication Services**: SAML 2.0, OpenID Connect, and Azure AD authentication temporarily disabled pending library API migration
2. **Browser Support**: Optimized for modern browsers (Chrome, Firefox, Edge, Safari - latest 2 versions)
3. **Database**: Currently supports SQL Server only

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Review existing documentation in the `/docs` folder
- Check [AUTH_MIGRATION_REQUIRED.md](AUTH_MIGRATION_REQUIRED.md) for authentication service migration

## Acknowledgments

- Built with .NET 8.0, Angular 17, Vue.js 3, and Node.js
- Uses industry-standard libraries and frameworks
- Follows microservices best practices
- Implements CQRS and event-driven architecture patterns
