#!/bin/bash

echo "========================================="
echo "  Starting BNPL Platform Application"
echo "========================================="
echo ""

# Step 1: Start infrastructure services
echo "Step 1: Starting infrastructure (SQL Server, Redis)..."
docker compose up -d sqlserver redis
echo "Waiting for services to be healthy..."
sleep 15

# Step 2: Check infrastructure health
echo ""
echo "Step 2: Checking infrastructure status..."
docker compose ps

# Step 3: Start backend API services
echo ""
echo "Step 3: Starting backend services..."
echo "Note: This may take a few minutes to build..."
docker compose up -d payment-api risk-api notification-api settlement-api api-gateway

# Wait for services to start
echo "Waiting for services to start..."
sleep 10

# Step 4: Show running services
echo ""
echo "Step 4: All services status..."
docker compose ps

echo ""
echo "========================================="
echo "  Application Started Successfully!"
echo "========================================="
echo ""
echo "Access Points:"
echo "  - API Gateway: http://localhost:5000"
echo "  - Payment API: http://localhost:5001"
echo "  - Risk API: http://localhost:5002"
echo "  - Notification API: http://localhost:5003"
echo "  - Settlement API: http://localhost:5004"
echo "  - SQL Server: localhost:1433"
echo "  - Redis: localhost:6379"
echo "  - Seq (Logs): http://localhost:5341"
echo "  - Prometheus: http://localhost:9090"
echo "  - Grafana: http://localhost:3000"
echo ""
echo "To view logs: docker compose logs -f [service-name]"
echo "To stop: docker compose down"
echo ""