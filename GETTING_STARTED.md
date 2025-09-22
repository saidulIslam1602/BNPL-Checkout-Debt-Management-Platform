# Getting Started with Riverty BNPL Platform

This guide will help you set up and run the Riverty BNPL (Buy Now, Pay Later) platform locally for development and testing.

## 🚀 Quick Start

### Prerequisites

Before you begin, ensure you have the following installed:

- **Docker Desktop** (4.0 or later)
- **Docker Compose** (2.0 or later)
- **.NET 8 SDK** (for local development)
- **Node.js 18+** and **npm** (for frontend development)
- **SQL Server Management Studio** or **Azure Data Studio** (optional, for database management)

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/riverty-bnpl-platform.git
cd riverty-bnpl-platform
```

### 2. Start the Platform with Docker

The easiest way to run the entire platform is using Docker Compose:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### 3. Access the Applications

Once all services are running, you can access:

| Service | URL | Description |
|---------|-----|-------------|
| **API Gateway** | http://localhost:5000 | Main API entry point |
| **Payment API** | http://localhost:5001 | Payment processing service |
| **Risk API** | http://localhost:5002 | Risk assessment service |
| **Notification API** | http://localhost:5003 | Notification service |
| **Settlement API** | http://localhost:5004 | Settlement service |
| **Merchant Portal** | http://localhost:4200 | Angular merchant dashboard |
| **Consumer Portal** | http://localhost:4201 | Angular consumer portal |
| **Swagger UI** | http://localhost:5001 | API documentation |
| **Seq Logs** | http://localhost:5341 | Centralized logging |
| **Grafana** | http://localhost:3000 | Monitoring dashboards |
| **Prometheus** | http://localhost:9090 | Metrics collection |

### 4. Database Access

The SQL Server instance runs on `localhost:1433` with:
- **Username**: `sa`
- **Password**: `RivertyBNPL123!`

Databases created:
- `RivertyBNPL_Payment` - Payment service data
- `RivertyBNPL_Risk` - Risk assessment data
- `RivertyBNPL_Settlement` - Settlement data
- `RivertyBNPL_Notification` - Notification data

## 🛠️ Development Setup

### Backend Development

1. **Restore NuGet packages**:
   ```bash
   dotnet restore RivertyBNPL.sln
   ```

2. **Build the solution**:
   ```bash
   dotnet build RivertyBNPL.sln
   ```

3. **Run a specific service** (e.g., Payment API):
   ```bash
   cd src/Services/Payment.API
   dotnet run
   ```

4. **Run database migrations**:
   ```bash
   cd src/Services/Payment.API
   dotnet ef database update
   ```

### Frontend Development

1. **Install dependencies** (Merchant Portal):
   ```bash
   cd src/Web/MerchantPortal
   npm install
   ```

2. **Start development server**:
   ```bash
   ng serve
   # Access at http://localhost:4200
   ```

3. **Consumer Portal**:
   ```bash
   cd src/Web/ConsumerPortal
   npm install
   ng serve --port 4201
   # Access at http://localhost:4201
   ```

## 🧪 Testing the Platform

### 1. API Testing with Swagger

1. Navigate to http://localhost:5001 (Payment API Swagger)
2. Click "Authorize" and use a test JWT token (see Authentication section)
3. Try the following endpoints:

#### Create a Test Payment
```json
POST /api/v1/payments
{
  "customerId": "33333333-3333-3333-3333-333333333333",
  "merchantId": "11111111-1111-1111-1111-111111111111",
  "amount": 1000.00,
  "currency": 578,
  "paymentMethod": 5,
  "enableBNPL": true,
  "bnplPlanType": 1,
  "orderReference": "ORDER-001",
  "description": "Test BNPL purchase"
}
```

#### Calculate BNPL Plan
```json
POST /api/v1/bnpl/calculate
{
  "amount": 1000.00,
  "currency": 578,
  "planType": 1,
  "customerId": "33333333-3333-3333-3333-333333333333"
}
```

### 2. Database Testing

The platform includes seeded test data:

**Test Customers:**
- Ola Nordmann (ID: `33333333-3333-3333-3333-333333333333`)
- Kari Hansen (ID: `44444444-4444-4444-4444-444444444444`)

**Test Merchants:**
- TechStore Norway (ID: `11111111-1111-1111-1111-111111111111`)
- Fashion Boutique (ID: `22222222-2222-2222-2222-222222222222`)

### 3. Frontend Testing

1. **Merchant Portal**: Navigate to http://localhost:4200
   - View transaction analytics
   - Monitor payment status
   - Generate settlement reports

2. **Consumer Portal**: Navigate to http://localhost:4201
   - Simulate BNPL checkout
   - View payment plans
   - Manage installments

## 🔐 Authentication

The platform uses JWT authentication. For testing, you can use these sample tokens:

### Admin Token
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImp0aSI6IjEyMzQ1Njc4LTEyMzQtMTIzNC0xMjM0LTEyMzQ1Njc4OTAxMiIsImVtYWlsIjoiYWRtaW5Acml2ZXJ0eS5jb20iLCJyb2xlIjoiQWRtaW4iLCJuYmYiOjE3MDAwMDAwMDAsImV4cCI6MTczMTUzNjAwMCwiaWF0IjoxNzAwMDAwMDAwLCJpc3MiOiJodHRwczovL2FwaS5yaXZlcnR5LmNvbSIsImF1ZCI6Imh0dHBzOi8vYXBpLnJpdmVydHkuY29tIn0
```

### Customer Token
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzMzMzMzMzMy0zMzMzLTMzMzMtMzMzMy0zMzMzMzMzMzMzMzMiLCJqdGkiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwMTIiLCJlbWFpbCI6Im9sYS5ub3JkbWFubkBleGFtcGxlLm5vIiwicm9sZSI6IkN1c3RvbWVyIiwibmJmIjoxNzAwMDAwMDAwLCJleHAiOjE3MzE1MzYwMDAsImlhdCI6MTcwMDAwMDAwMCwiaXNzIjoiaHR0cHM6Ly9hcGkucml2ZXJ0eS5jb20iLCJhdWQiOiJodHRwczovL2FwaS5yaXZlcnR5LmNvbSJ9
```

### Merchant Token
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMTExMTExMS0xMTExLTExMTEtMTExMS0xMTExMTExMTExMTEiLCJqdGkiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwMTIiLCJlbWFpbCI6Im1lcmNoYW50QHRlY2hzdG9yZS5ubyIsInJvbGUiOiJNZXJjaGFudCIsIm5iZiI6MTcwMDAwMDAwMCwiZXhwIjoxNzMxNTM2MDAwLCJpYXQiOjE3MDAwMDAwMDAsImlzcyI6Imh0dHBzOi8vYXBpLnJpdmVydHkuY29tIiwiYXVkIjoiaHR0cHM6Ly9hcGkucml2ZXJ0eS5jb20ifQ
```

## 📊 Monitoring and Logging

### Centralized Logging with Seq
- Access: http://localhost:5341
- View structured logs from all services
- Filter by service, log level, or custom properties

### Metrics with Prometheus & Grafana
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- Pre-configured dashboards for API metrics, database performance, and business KPIs

### Health Checks
Each service exposes health check endpoints:
- `/health` - Overall health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## 🔧 Configuration

### Environment Variables

Key configuration options:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=RivertyBNPL_Payment;User Id=sa;Password=RivertyBNPL123!;TrustServerCertificate=true

# JWT Authentication
Jwt__Key=your-secret-key
Jwt__Issuer=https://api.riverty.com
Jwt__Audience=https://api.riverty.com

# Redis Cache
Redis__ConnectionString=localhost:6379

# External Services
ExternalServices__RiskAssessmentApi__BaseUrl=http://localhost:5002
```

### Application Settings

Each service has its own `appsettings.json` with service-specific configuration:

- Payment settings (fees, limits, BNPL rates)
- Risk assessment thresholds
- Notification templates and channels
- Settlement schedules

## 🚨 Troubleshooting

### Common Issues

1. **Services not starting**:
   ```bash
   # Check Docker logs
   docker-compose logs [service-name]
   
   # Restart specific service
   docker-compose restart [service-name]
   ```

2. **Database connection issues**:
   ```bash
   # Verify SQL Server is running
   docker-compose ps sqlserver
   
   # Check database logs
   docker-compose logs sqlserver
   ```

3. **Port conflicts**:
   ```bash
   # Check what's using the port
   netstat -tulpn | grep :5001
   
   # Modify docker-compose.yml to use different ports
   ```

4. **Frontend build issues**:
   ```bash
   # Clear npm cache
   npm cache clean --force
   
   # Delete node_modules and reinstall
   rm -rf node_modules
   npm install
   ```

### Performance Optimization

1. **Database Performance**:
   - Ensure proper indexing (handled by Entity Framework migrations)
   - Monitor query performance in Application Insights
   - Use connection pooling (configured by default)

2. **API Performance**:
   - Enable response caching for read-heavy endpoints
   - Use Redis for distributed caching
   - Implement proper pagination

3. **Frontend Performance**:
   - Enable Angular production build optimizations
   - Implement lazy loading for routes
   - Use OnPush change detection strategy

## 📚 Next Steps

1. **Explore the APIs**: Use Swagger UI to understand available endpoints
2. **Review the Architecture**: Check the `docs/architecture/` folder
3. **Run Tests**: Execute unit and integration tests
4. **Customize Configuration**: Modify settings for your use case
5. **Deploy to Cloud**: Follow the Azure deployment guide

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📞 Support

For questions or issues:
- Check the troubleshooting section above
- Review the API documentation
- Create an issue in the GitHub repository
- Contact the development team

---

**Happy coding! 🚀**