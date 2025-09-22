# Riverty BNPL Platform

A comprehensive Buy Now, Pay Later (BNPL) and debt management platform built with .NET Core microservices and Angular frontend, designed to mirror Riverty's core business operations.

## 🏗️ Architecture Overview

This platform implements a microservices architecture with event-driven communication, designed for scalability and maintainability in financial services.

### Backend Services (.NET Core 8)
- **Payment Service**: Handles BNPL payment processing and installment calculations
- **Risk Assessment Service**: Credit checks, fraud detection, and risk scoring
- **Settlement Service**: Merchant payouts and financial reconciliation
- **Notification Service**: Payment reminders and customer communications

### Frontend Applications (Angular 17)
- **Merchant Portal**: Transaction analytics, settlement reports, payment tracking
- **Consumer Portal**: BNPL checkout, payment management, debt self-service

### Infrastructure (Azure Cloud)
- **Azure Kubernetes Service (AKS)**: Container orchestration
- **Azure Service Bus**: Event-driven messaging
- **Azure Functions**: Scheduled payment processing
- **Azure Key Vault**: Secrets management
- **Application Insights**: Monitoring and telemetry
- **SQL Server**: Financial transaction database

## 🚀 Key Features

### Financial Operations
- ✅ Real-time BNPL payment processing
- ✅ Automated installment calculations
- ✅ Risk-based credit assessments
- ✅ Merchant settlement automation
- ✅ Payment collection workflows

### Compliance & Security
- ✅ Strong Customer Authentication (SCA)
- ✅ PCI DSS compliance patterns
- ✅ Idempotent payment operations
- ✅ Comprehensive audit logging
- ✅ GDPR data protection

### User Experience
- ✅ Mobile-responsive design
- ✅ Multi-language support (EN/NO)
- ✅ Real-time payment notifications
- ✅ Self-service debt management
- ✅ Payment plan simulation

## 🛠️ Technology Stack

### Backend
- .NET Core 8.0
- Entity Framework Core
- Azure Service Bus
- Azure Functions
- SQL Server
- AutoMapper
- FluentValidation
- Serilog

### Frontend
- Angular 17
- TypeScript 5.0
- Angular Material
- NgRx (State Management)
- Chart.js (Analytics)
- Angular PWA

### Infrastructure
- Azure Kubernetes Service
- Terraform (IaC)
- Docker
- Azure DevOps
- SonarQube

## 📁 Project Structure

```
riverty-bnpl-platform/
├── src/
│   ├── Services/
│   │   ├── Payment.API/           # Payment processing service
│   │   ├── Risk.API/              # Risk assessment service
│   │   ├── Settlement.API/        # Merchant settlement service
│   │   └── Notification.API/      # Notification service
│   ├── Functions/
│   │   ├── PaymentCollection/     # Scheduled payment collection
│   │   └── NotificationScheduler/ # Payment reminders
│   ├── Web/
│   │   ├── MerchantPortal/        # Angular merchant dashboard
│   │   └── ConsumerPortal/        # Angular consumer portal
│   ├── Shared/
│   │   ├── Common/                # Shared libraries
│   │   ├── Events/                # Event contracts
│   │   └── Infrastructure/        # Cross-cutting concerns
│   └── Gateway/
│       └── API.Gateway/           # API Gateway with Ocelot
├── infrastructure/
│   └── terraform/                 # Infrastructure as Code
├── tests/
│   ├── Unit/                      # Unit tests
│   ├── Integration/               # Integration tests
│   └── E2E/                       # End-to-end tests
└── docs/
    ├── api/                       # API documentation
    └── architecture/              # Architecture decisions
```

## 🚦 Getting Started

### Prerequisites
- .NET Core 8.0 SDK
- Node.js 18+ and npm
- SQL Server (LocalDB or full instance)
- Azure CLI (for cloud deployment)
- Docker Desktop

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/riverty-bnpl-platform.git
   cd riverty-bnpl-platform
   ```

2. **Backend Setup**
   ```bash
   cd src
   dotnet restore
   dotnet build
   ```

3. **Database Setup**
   ```bash
   cd src/Services/Payment.API
   dotnet ef database update
   ```

4. **Frontend Setup**
   ```bash
   cd src/Web/MerchantPortal
   npm install
   ng serve
   ```

### Running the Platform

1. **Start Backend Services**
   ```bash
   docker-compose up -d
   ```

2. **Start Frontend Applications**
   ```bash
   # Merchant Portal (http://localhost:4200)
   cd src/Web/MerchantPortal && ng serve
   
   # Consumer Portal (http://localhost:4201)
   cd src/Web/ConsumerPortal && ng serve --port 4201
   ```

## 📊 Business Logic

### BNPL Payment Flow
1. Customer selects BNPL option at checkout
2. Risk assessment evaluates creditworthiness
3. Payment plan is generated with installments
4. First payment is processed immediately
5. Subsequent payments are scheduled automatically
6. Merchant receives settlement according to terms

### Debt Management
1. Automated payment reminders via multiple channels
2. Self-service payment plan modifications
3. Hardship assistance workflows
4. Collection process automation
5. Settlement negotiations

## 🔒 Security Features

- **Authentication**: OAuth 2.0 / OpenID Connect
- **Authorization**: Role-based access control (RBAC)
- **Data Protection**: Encryption at rest and in transit
- **API Security**: Rate limiting, input validation
- **Compliance**: PCI DSS, GDPR, SCA requirements

## 📈 Monitoring & Observability

- Application Insights for telemetry
- Structured logging with Serilog
- Health checks for all services
- Performance monitoring
- Business metrics dashboards

## 🧪 Testing Strategy

- **Unit Tests**: 90%+ code coverage
- **Integration Tests**: API and database testing
- **Contract Tests**: Service boundary validation
- **E2E Tests**: Critical user journey validation
- **Performance Tests**: Load and stress testing

## 📚 Documentation

- [API Documentation](docs/api/README.md)
- [Architecture Decisions](docs/architecture/README.md)
- [Deployment Guide](docs/deployment/README.md)
- [Contributing Guidelines](CONTRIBUTING.md)

## 🤝 Contributing

Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ for modern financial services**