# Quick Start Guide - After Fixes

This guide helps you get started with the YourCompany BNPL platform after the recent fixes.

## Prerequisites

- Docker & Docker Compose
- .NET 8.0 SDK
- Node.js 18+
- Git

## Setup Steps

### 1. Configure Environment Variables

```bash
# Copy the template
cp .env.example .env

# Edit .env and replace placeholder values with your actual credentials
nano .env  # or use your preferred editor
```

**Critical variables to update**:
- `DB_SA_PASSWORD` - SQL Server password
- `REDIS_PASSWORD` - Redis password
- `MONGODB_ROOT_PASSWORD` - MongoDB password
- `JWT_SECRET_KEY` - Generate with: `openssl rand -base64 32`
- Email SMTP credentials
- SMS provider credentials (Twilio)
- Payment gateway credentials

### 2. Start Infrastructure Services

```bash
# Start databases and infrastructure
docker-compose up -d sqlserver redis mongodb elasticsearch kibana prometheus grafana

# Wait 30 seconds for databases to initialize

# Verify services are running
docker-compose ps
```

### 3. Run Database Migrations

```bash
# Option 1: Using the build script
./scripts/build.sh migrate

# Option 2: Manual migration per service
cd src/Services/Payment.API
dotnet ef database update
cd ../Risk.API
dotnet ef database update
cd ../Settlement.API
dotnet ef database update
cd ../Notification.API
dotnet ef database update
cd ../../..
```

### 4. Build the Application

```bash
# Clean previous builds
dotnet clean MerchantBNPL.sln

# Restore and build
dotnet restore MerchantBNPL.sln
dotnet build MerchantBNPL.sln --configuration Debug
```

### 5. Start Microservices

```bash
# Start all services with Docker Compose
docker-compose up -d

# Or start individual services in separate terminals:

# Terminal 1 - API Gateway
cd src/Gateway/API.Gateway
dotnet run

# Terminal 2 - Payment API
cd src/Services/Payment.API
dotnet run

# Terminal 3 - Risk API
cd src/Services/Risk.API
dotnet run

# Terminal 4 - Settlement API
cd src/Services/Settlement.API
dotnet run

# Terminal 5 - Notification API
cd src/Services/Notification.API
dotnet run

# Terminal 6 - RealTime Node API
cd src/Services/RealTime.Node.API
npm install
npm start
```

### 6. Start Frontend Applications

```bash
# Terminal 7 - Admin Portal (Vue.js)
cd src/Web/AdminPortal
npm install
npm run dev

# Terminal 8 - Consumer Portal (Angular)
cd src/Web/ConsumerPortal
npm install
npm start

# Terminal 9 - Merchant Portal (Angular)
cd src/Web/MerchantPortal
npm install
npm start
```

## Access Points

| Service | URL | Purpose |
|---------|-----|---------|
| API Gateway | http://localhost:5000 | Central API endpoint |
| Payment API | http://localhost:5001 | Payment processing |
| Risk API | http://localhost:5002 | Credit risk assessment |
| Settlement API | http://localhost:5003 | Transaction settlement |
| Notification API | http://localhost:5004 | Notifications |
| RealTime API | http://localhost:3000 | WebSocket notifications |
| Admin Portal | http://localhost:4200 | Admin dashboard |
| Consumer Portal | http://localhost:4201 | Consumer interface |
| Merchant Portal | http://localhost:4202 | Merchant interface |
| Grafana | http://localhost:3001 | Metrics (admin/admin) |
| Kibana | http://localhost:5601 | Logs |

## Health Checks

```bash
# Check all services
curl http://localhost:5000/health

# Individual service health
curl http://localhost:5001/health  # Payment API
curl http://localhost:5002/health  # Risk API
curl http://localhost:5003/health  # Settlement API
curl http://localhost:5004/health  # Notification API
```

## Common Issues

### Database Connection Failed
```bash
# Check if SQL Server is running
docker-compose ps sqlserver

# Check logs
docker-compose logs sqlserver

# Verify database names
docker exec -it bnpl-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourPassword' -Q "SELECT name FROM sys.databases"
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean MerchantBNPL.sln
rm -rf **/bin **/obj
dotnet restore MerchantBNPL.sln
dotnet build MerchantBNPL.sln
```

### Port Already in Use
```bash
# Find process using port
lsof -i :5001  # Replace with your port

# Kill process
kill -9 <PID>
```

### Missing Dependencies
```bash
# .NET packages
dotnet restore

# Node.js packages
npm install

# Global tools
dotnet tool restore
```

## Known Compilation Errors

 The following compilation errors exist and need to be fixed separately:

### Payment.API
- Missing `Microsoft.Identity.Web` package
- Missing `Microsoft.Graph` package
- Missing `IUserService` interface

**Fix**:
```bash
cd src/Services/Payment.API
dotnet add package Microsoft.Identity.Web --version 2.15.5
dotnet add package Microsoft.Graph --version 5.36.0
dotnet add package Sustainsys.Saml2.AspNetCore2 --version 2.9.2
```

### Risk.API
- Missing `EmploymentStatus` enum

**Fix**: Define enum in `src/Services/Risk.API/Models/Enums.cs`:
```csharp
public enum EmploymentStatus
{
    FullTime,
    PartTime,
    SelfEmployed,
    Unemployed,
    Retired,
    Student,
    Other
}
```

### PaymentGatewayService
- Syntax errors at lines 1102 and 1146

**Fix**: Review and correct bracket matching in those lines.

For detailed information, see `KNOWN_ISSUES.md`.

## Useful Commands

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f payment-api

# Restart a service
docker-compose restart payment-api

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# Run tests
dotnet test tests/Unit/
dotnet test tests/Integration/

# Build Docker images
docker-compose build

# Check Docker resources
docker system df
docker system prune -f  # Clean up
```

## Development Workflow

1. **Make code changes** in your editor
2. **Rebuild** the affected service: `dotnet build`
3. **Restart** the service: `docker-compose restart <service-name>`
4. **Test** the changes via API or frontend
5. **Check logs** for errors: `docker-compose logs -f <service-name>`

## Testing

```bash
# Unit tests
dotnet test tests/Unit/Payment.API.Tests/
dotnet test tests/Unit/Risk.API.Tests/

# Integration tests (requires services running)
docker-compose up -d
dotnet test tests/Integration/Integration.Tests/

# API testing with curl
curl -X POST http://localhost:5000/api/payment/orders \
  -H "Content-Type: application/json" \
  -d '{"amount": 1000, "currency": "NOK"}'
```

## Next Steps

1.  Services running? → Configure authentication (see docs/API_DOCUMENTATION.md)
2.  APIs working? → Set up payment gateways (Stripe, PayPal, Adyen)
3.  Gateways configured? → Integrate Norwegian credit bureaus
4.  Everything working? → Deploy to Azure (see docs/DEPLOYMENT_GUIDE.md)

## Support

- **Documentation**: See `docs/` folder
- **API Reference**: `docs/API_DOCUMENTATION.md`
- **Deployment**: `docs/DEPLOYMENT_GUIDE.md`
- **Issues**: Check `KNOWN_ISSUES.md`
- **Changes**: Review `FIXES_SUMMARY.md`

---

**Last Updated**: Current Session  
**Platform Version**: 1.0.0  
**Status**: Ready for Development
