# 🚀 Quick Start Guide - BNPL Platform

Get the BNPL Checkout & Debt Management Platform (Riverty) up and running in less than 10 minutes!

## 📋 Prerequisites

Ensure you have these tools installed:

| Tool | Version | Download Link | Purpose |
|------|---------|---------------|---------|
| **Docker Desktop** | 24.0+ | [docker.com](https://www.docker.com/products/docker-desktop) | Container orchestration |
| **.NET SDK** | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) | Backend services |
| **Node.js** | 18.0+ LTS | [nodejs.org](https://nodejs.org/) | Frontend apps & real-time API |
| **Git** | Latest | [git-scm.com](https://git-scm.com/) | Version control |

**System Requirements:**
- **RAM**: 8GB minimum (16GB recommended)
- **Disk**: 10GB free space
- **OS**: Windows 10/11, macOS 12+, or Linux (Ubuntu 20.04+)

## ⚡ Quick Setup (5 Minutes)

### Step 1: Clone Repository

```bash
git clone https://github.com/saidulIslam1602/BNPL-Checkout-Debt-Management-Platform.git
cd BNPL-Checkout-Debt-Management-Platform
```

### Step 2: Start Infrastructure

```bash
# Start databases and monitoring stack
docker-compose up -d sqlserver redis mongodb elasticsearch prometheus grafana

# Wait for databases to initialize (30 seconds)
echo "Waiting for databases to initialize..."
sleep 30

# Verify infrastructure is running
docker-compose ps
```

**Expected Services Running:**
- ✅ SQL Server (Port 1433)
- ✅ Redis (Port 6379)
- ✅ MongoDB (Port 27017)
- ✅ Elasticsearch (Port 9200)
- ✅ Prometheus (Port 9090)
- ✅ Grafana (Port 3001)

### Step 3: Build Solution

```bash
# Restore NuGet packages and build all 14 projects
dotnet restore MerchantBNPL.sln
dotnet build MerchantBNPL.sln --configuration Release
```

**Build Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 4: Start All Services

```bash
# Option A: Start everything with Docker Compose (Recommended)
docker-compose up -d

# Option B: Start services individually (for development)
# See "Development Mode" section below
```

### Step 5: Verify Services

```bash
# Check health of all services
curl http://localhost:7000/health  # API Gateway
curl http://localhost:5001/health  # Payment API
curl http://localhost:5002/health  # Risk API
curl http://localhost:5003/health  # Settlement API
curl http://localhost:5004/health  # Notification API
curl http://localhost:3000/health  # Real-time API
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "mongodb": "Healthy"
  }
}
```

## 🌐 Access the Platform

### Backend Services & APIs

| Service | URL | Swagger Docs | Description |
|---------|-----|--------------|-------------|
| **API Gateway** | http://localhost:7000 | - | Central routing & load balancing |
| **Payment API** | http://localhost:5001 | http://localhost:5001/swagger | Payment processing & tokenization |
| **Risk API** | http://localhost:5002 | http://localhost:5002/swagger | Credit risk & fraud detection |
| **Settlement API** | http://localhost:5003 | http://localhost:5003/swagger | Merchant settlements & reporting |
| **Notification API** | http://localhost:5004 | http://localhost:5004/swagger | Email, SMS, Push notifications |
| **Real-time API** | http://localhost:3000 | - | WebSocket connections |

### Frontend Portals

| Portal | URL | Technology | Purpose |
|--------|-----|-----------|----------|
| **Admin Portal** | http://localhost:4200 | Vue.js 3 + Vite | System administration dashboard |
| **Consumer Portal** | http://localhost:4201 | Angular 17 | Customer checkout & account management |
| **Merchant Portal** | http://localhost:4202 | Angular 17 | Business analytics & transaction management |
| **Legacy Portal** | http://localhost:4203 | Knockout.js | Backward compatibility support |

### Monitoring & Observability

| Tool | URL | Credentials | Purpose |
|------|-----|-------------|---------|
| **Grafana** | http://localhost:3001 | admin / admin | Real-time metrics & dashboards |
| **Prometheus** | http://localhost:9090 | - | Metrics collection |
| **Kibana** | http://localhost:5601 | - | Log analysis & search |
| **Elasticsearch** | http://localhost:9200 | - | Centralized log storage |

## 🔧 Development Mode (Hot Reload)

For active development with automatic reloading:

```bash
# Terminal 1: API Gateway
cd src/Gateway/API.Gateway && dotnet watch run

# Terminal 2: Payment API  
cd src/Services/Payment.API && dotnet watch run

# Terminal 3: Risk API
cd src/Services/Risk.API && dotnet watch run

# Terminal 4: Settlement API
cd src/Services/Settlement.API && dotnet watch run

# Terminal 5: Notification API
cd src/Services/Notification.API && dotnet watch run

# Terminal 6: Real-time API (Node.js)
cd src/Services/RealTime.Node.API && npm install && npm run dev

# Terminal 7: Admin Portal (Vue.js)
cd src/Web/AdminPortal && npm install && npm run dev

# Terminal 8: Merchant Portal (Angular)
cd src/Web/MerchantPortal && npm install && npm start

# Terminal 9: Consumer Portal (Angular)
cd src/Web/ConsumerPortal && npm install && npm start
```

## 🧪 Testing Your Setup

### 1. Verify All Health Checks

```bash
# Check all backend services
echo "Testing API Gateway..." && curl -s http://localhost:7000/health | grep -q "Healthy" && echo "✅ API Gateway" || echo "❌ API Gateway"
echo "Testing Payment API..." && curl -s http://localhost:5001/health | grep -q "Healthy" && echo "✅ Payment API" || echo "❌ Payment API"
echo "Testing Risk API..." && curl -s http://localhost:5002/health | grep -q "Healthy" && echo "✅ Risk API" || echo "❌ Risk API"
echo "Testing Settlement API..." && curl -s http://localhost:5003/health | grep -q "Healthy" && echo "✅ Settlement API" || echo "❌ Settlement API"
echo "Testing Notification API..." && curl -s http://localhost:5004/health | grep -q "Healthy" && echo "✅ Notification API" || echo "❌ Notification API"
```

### 2. Explore API Documentation

Visit Swagger UI for interactive API testing:
- **Payment API**: http://localhost:5001/swagger
- **Risk API**: http://localhost:5002/swagger
- **Settlement API**: http://localhost:5003/swagger
- **Notification API**: http://localhost:5004/swagger

### 3. Test API via Gateway

```bash
# Test routing through API Gateway
curl http://localhost:7000/payment/api/v1/health
curl http://localhost:7000/risk/api/v1/health
curl http://localhost:7000/settlement/api/v1/health
curl http://localhost:7000/notification/api/v1/health
```

### 4. View Monitoring Dashboards

1. **Open Grafana**: http://localhost:3001
   - Login: `admin` / `admin`
   - Navigate to "BNPL Overview" dashboard
   - View real-time metrics

2. **Open Kibana**: http://localhost:5601
   - Create index pattern: `logs-*`
   - Search application logs
   - Create custom visualizations

## ⚙️ Environment Configuration (Optional)

For production-like setup, configure environment variables:

```bash
# Copy template
cp appsettings.template.json src/Services/Payment.API/appsettings.Development.json

# Edit configuration
# Update connection strings, API keys, and secrets
```

**Key Variables to Configure:**
- **JWT Secret**: Generate with `openssl rand -base64 32`
- **Database Password**: Strong password for SQL Server
- **Redis Password**: Secure password for Redis
- **SendGrid API Key**: For email notifications
- **Twilio Credentials**: For SMS notifications
- **Payment Gateway Keys**: Stripe, Adyen, Nets, Vipps credentials
- **Credit Bureau Keys**: Norwegian credit bureau API credentials

## 🐛 Troubleshooting

## 🐛 Troubleshooting

### Common Issues and Solutions

#### 1. Database Connection Failed

```bash
# Check if SQL Server container is running
docker-compose ps sqlserver

# View SQL Server logs
docker-compose logs sqlserver

# Verify databases were created
docker exec -it bnpl-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Password123' \
  -Q "SELECT name FROM sys.databases"

# Restart SQL Server
docker-compose restart sqlserver
```

#### 2. Port Already in Use

```bash
# Find process using the port (Linux/macOS)
lsof -i :5001
netstat -an | grep 5001

# Find process using the port (Windows)
netstat -ano | findstr :5001

# Kill the process
kill -9 <PID>          # Linux/macOS
taskkill /PID <PID> /F # Windows

# Or change the port in launchSettings.json
```

#### 3. Build Errors

```bash
# Clean solution and remove all build artifacts
dotnet clean MerchantBNPL.sln
find . -type d -name 'bin' -o -name 'obj' | xargs rm -rf

# Restore packages
dotnet restore MerchantBNPL.sln

# Rebuild
dotnet build MerchantBNPL.sln --configuration Release
```

#### 4. Docker Container Issues

```bash
# Check all container status
docker-compose ps

# View logs for specific service
docker-compose logs -f payment-api
docker-compose logs -f risk-api

# Restart all services
docker-compose restart

# Complete cleanup and restart
docker-compose down -v
docker-compose up -d --build
```

#### 5. Missing Node Modules

```bash
# Install dependencies for all frontend apps
cd src/Web/AdminPortal && npm install && cd ../../..
cd src/Web/MerchantPortal && npm install && cd ../../..
cd src/Web/ConsumerPortal && npm install && cd ../../..
cd src/Services/RealTime.Node.API && npm install && cd ../../..
```

#### 6. Redis Connection Issues

```bash
# Check Redis is running
docker-compose logs redis

# Test Redis connection
docker exec -it bnpl-redis redis-cli ping
# Should return: PONG

# Flush Redis cache if needed
docker exec -it bnpl-redis redis-cli FLUSHALL
```

#### 7. Service Not Responding

```bash
# Check if service process is running
ps aux | grep dotnet

# Check if port is listening
netstat -tlnp | grep :5001

# Restart specific service
docker-compose restart payment-api

# Check service logs
docker-compose logs --tail=100 -f payment-api
```

## 📝 Useful Development Commands

```bash
# View all container logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f payment-api risk-api

# Restart a specific service
docker-compose restart payment-api

# Stop all services
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v

# Rebuild specific service
docker-compose up -d --no-deps --build payment-api

# Run unit tests
dotnet test tests/Unit/ --logger "console;verbosity=normal"

# Run integration tests (requires services running)
docker-compose up -d
dotnet test tests/Integration/ --logger "console;verbosity=detailed"

# Check Docker resource usage
docker stats

# Clean Docker system
docker system df
docker system prune -f --volumes
```

## 🔄 Development Workflow

**Standard Development Cycle:**

1. **Make Code Changes** in your IDE
2. **Build Service** (if not using `dotnet watch`):
   ```bash
   cd src/Services/Payment.API
   dotnet build
   ```
3. **Restart Service**:
   ```bash
   docker-compose restart payment-api
   ```
4. **Test via Swagger**: http://localhost:5001/swagger
5. **Check Logs**:
   ```bash
   docker-compose logs -f payment-api
   ```
6. **Verify Health**:
   ```bash
   curl http://localhost:5001/health
   ```

## ✅ Platform Status & Next Steps

### ✅ What's Working

- ✅ **Build**: All 14 projects compile with 0 errors
- ✅ **Services**: All APIs start and respond to health checks
- ✅ **Database**: 4 SQL Server databases initialized
- ✅ **Caching**: Redis configured and operational
- ✅ **Logging**: Elasticsearch + Kibana setup
- ✅ **Monitoring**: Prometheus + Grafana dashboards
- ✅ **Gateway**: Ocelot API Gateway routing all services
- ✅ **Real-time**: Socket.IO WebSocket server running
- ✅ **Frontend**: All 4 portals build and run

### 🎯 Next Steps

1. **Configure External Services** (For Production):
   - Set up SendGrid for email notifications
   - Configure Twilio for SMS notifications
   - Add Stripe/Adyen/Vipps payment gateway credentials
   - Configure Norwegian credit bureau API keys

2. **Test Core Flows**:
   - Create test payment through Swagger
   - Perform risk assessment
   - Send test notification
   - Process settlement batch

3. **Customize & Extend**:
   - Add custom business rules
   - Configure merchant-specific settings
   - Customize notification templates
   - Set up additional monitoring alerts

4. **Production Deployment**:
   - Review [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)
   - Set up Azure infrastructure with Terraform
   - Configure CI/CD pipelines
   - Enable production monitoring

## 📚 Additional Resources

- **Full Documentation**: [README.md](README.md)
- **API Reference**: [docs/API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md)
- **Deployment Guide**: [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)
- **Payment API Features**: [src/Services/Payment.API/PAYMENT_API_ENHANCEMENTS.md](src/Services/Payment.API/PAYMENT_API_ENHANCEMENTS.md)
- **Risk API Guide**: [src/Services/Risk.API/README.md](src/Services/Risk.API/README.md)
- **Notification API Guide**: [src/Services/Notification.API/README.md](src/Services/Notification.API/README.md)
- **Merchant Portal Guide**: [src/Web/MerchantPortal/README.md](src/Web/MerchantPortal/README.md)

## 🆘 Getting Help

- **GitHub Issues**: [Report a bug](https://github.com/saidulIslam1602/BNPL-Checkout-Debt-Management-Platform/issues)
- **Documentation**: Check the `/docs` folder
- **Logs**: Always check Docker logs for detailed error messages

---

**🎉 Congratulations!** You now have a fully functional BNPL platform running locally.

**Last Updated**: January 2026  
**Version**: 1.0.0  
**Status**: ✅ Production Ready
