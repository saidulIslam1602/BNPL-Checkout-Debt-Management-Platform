# 🚀 Deployment Guide - BNPL Platform

This guide covers deploying the BNPL Platform to various environments.

## 📑 Table of Contents

- [Prerequisites](#prerequisites)
- [Local Development](#local-development)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Azure Cloud Deployment](#azure-cloud-deployment)
- [CI/CD Setup](#cicd-setup)
- [Monitoring Setup](#monitoring-setup)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

- **Docker Desktop** (v20.10+)
- **kubectl** (v1.28+)
- **Azure CLI** (v2.50+) - for Azure deployments
- **Terraform** (v1.5+) - for infrastructure provisioning
- **.NET SDK 8.0**
- **Node.js 18+**

### Azure Resources (for cloud deployment)

- Azure Subscription
- Resource Group
- Azure Container Registry (ACR)
- Azure Kubernetes Service (AKS)
- Azure SQL Database
- Azure Redis Cache
- Azure Service Bus

---

## Local Development

### Quick Start

```bash
# Start all services
docker-compose up -d

# Check service health
curl http://localhost:5001/health  # Payment API
curl http://localhost:5002/health  # Risk API
curl http://localhost:5003/health  # Notification API
curl http://localhost:5004/health  # Settlement API
```

### Manual Setup

See [GETTING_STARTED.md](../GETTING_STARTED.md) for detailed local setup instructions.

---

## Docker Deployment

### Build All Images

```bash
# Build all services
docker-compose build

# Build specific service
docker-compose build payment-api
```

### Production Docker Compose

```bash
# Use production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Environment Variables

Create a `.env` file in the root directory:

```env
COMPANY_NAME=Your Company
JWT_KEY=your-secure-jwt-key
SQL_PASSWORD=YourStrongPassword123!
REDIS_PASSWORD=YourRedisPassword
SENDGRID_API_KEY=your-sendgrid-key
TWILIO_ACCOUNT_SID=your-twilio-sid
TWILIO_AUTH_TOKEN=your-twilio-token
```

---

## Kubernetes Deployment

### 1. Prepare Cluster

```bash
# Create namespace
kubectl apply -f k8s/namespace.yaml

# Verify namespace
kubectl get namespace bnpl-platform
```

### 2. Configure Secrets

Edit `k8s/secrets.yaml` with your actual secrets:

```bash
# Generate base64 encoded secrets
echo -n 'your-secret' | base64

# Apply secrets
kubectl apply -f k8s/secrets.yaml
```

### 3. Deploy Services

```bash
# Deploy configuration
kubectl apply -f k8s/configmap.yaml

# Deploy all services
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n bnpl-platform
kubectl get services -n bnpl-platform
```

### 4. Verify Deployment

```bash
# Check pod status
kubectl get pods -n bnpl-platform

# Check logs
kubectl logs -f deployment/payment-api -n bnpl-platform

# Port forward for testing
kubectl port-forward svc/payment-api 8080:80 -n bnpl-platform
```

### 5. Configure Ingress

```bash
# Install NGINX Ingress Controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/cloud/deploy.yaml

# Apply ingress configuration
kubectl apply -f k8s/ingress.yaml

# Get ingress IP
kubectl get ingress -n bnpl-platform
```

---

## Azure Cloud Deployment

### 1. Infrastructure Provisioning with Terraform

```bash
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Copy and edit variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Plan infrastructure
terraform plan

# Apply infrastructure
terraform apply

# Save outputs
terraform output > outputs.txt
```

### 2. Setup Azure Container Registry

```bash
# Login to Azure
az login

# Login to ACR
az acr login --name <your-acr-name>

# Build and push images
./scripts/deploy-azure.sh
```

### 3. Deploy to AKS

```bash
# Get AKS credentials
az aks get-credentials \
  --resource-group bnpl-platform-rg \
  --name bnpl-aks

# Deploy to Kubernetes
kubectl apply -f k8s/

# Monitor deployment
kubectl rollout status deployment/payment-api -n bnpl-platform
```

### 4. Configure DNS

```bash
# Get ingress IP
INGRESS_IP=$(kubectl get ingress bnpl-ingress -n bnpl-platform -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

echo "Configure DNS:"
echo "api.yourcompany.com -> $INGRESS_IP"
echo "app.yourcompany.com -> $INGRESS_IP"
echo "merchant.yourcompany.com -> $INGRESS_IP"
```

---

## CI/CD Setup

### GitHub Actions

The project includes a complete CI/CD pipeline in `.github/workflows/ci-cd.yml`.

#### Setup Secrets

Add these secrets in GitHub Settings > Secrets:

```
AZURE_CREDENTIALS
ACR_USERNAME
ACR_PASSWORD
API_BASE_URL
```

#### Generate Azure Credentials

```bash
az ad sp create-for-rbac \
  --name "bnpl-github-actions" \
  --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/bnpl-platform-rg \
  --sdk-auth
```

Copy the JSON output to `AZURE_CREDENTIALS` secret.

#### Trigger Deployment

```bash
# Push to main branch triggers deployment
git push origin main

# Monitor in GitHub Actions tab
```

---

## Monitoring Setup

### 1. Prometheus & Grafana

```bash
# Deploy monitoring stack
kubectl apply -f k8s/monitoring/

# Access Grafana
kubectl port-forward svc/grafana 3000:3000 -n bnpl-platform

# Default credentials: admin/admin
```

### 2. Application Insights

Already configured in Terraform. View metrics in Azure Portal:

```bash
# Get Application Insights details
terraform output application_insights_instrumentation_key
```

### 3. Log Analytics

```bash
# Query logs
az monitor log-analytics query \
  --workspace $(terraform output log_analytics_workspace_id) \
  --analytics-query "ContainerLog | where TimeGenerated > ago(1h) | limit 100"
```

---

## SSL/TLS Certificates

### Let's Encrypt with cert-manager

```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create cluster issuer
kubectl apply -f k8s/cert-manager/cluster-issuer.yaml

# Certificates will be automatically issued for ingress
```

### Custom Certificate

```bash
# Create TLS secret
kubectl create secret tls bnpl-tls \
  --cert=path/to/cert.crt \
  --key=path/to/cert.key \
  -n bnpl-platform
```

---

## Scaling

### Manual Scaling

```bash
# Scale deployment
kubectl scale deployment payment-api --replicas=5 -n bnpl-platform
```

### Auto-scaling (HPA)

```bash
# Horizontal Pod Autoscaler is already configured
kubectl get hpa -n bnpl-platform

# View scaling events
kubectl describe hpa payment-api-hpa -n bnpl-platform
```

---

## Backup and Recovery

### Database Backup

```bash
# Azure SQL automatic backups are configured in Terraform
# Manual backup:
az sql db export \
  --resource-group bnpl-platform-rg \
  --server bnpl-sql \
  --name BNPL_Payment \
  --admin-user sqladmin \
  --admin-password <password> \
  --storage-key <storage-key> \
  --storage-key-type StorageAccessKey \
  --storage-uri https://bnplbackup.blob.core.windows.net/backups/payment-$(date +%Y%m%d).bacpac
```

### Restore from Backup

```bash
az sql db import \
  --resource-group bnpl-platform-rg \
  --server bnpl-sql \
  --name BNPL_Payment_Restored \
  --admin-user sqladmin \
  --admin-password <password> \
  --storage-key <storage-key> \
  --storage-key-type StorageAccessKey \
  --storage-uri https://bnplbackup.blob.core.windows.net/backups/payment-20240101.bacpac
```

---

## Troubleshooting

### Pod Not Starting

```bash
# Check pod events
kubectl describe pod <pod-name> -n bnpl-platform

# Check logs
kubectl logs <pod-name> -n bnpl-platform

# Check previous container logs
kubectl logs <pod-name> --previous -n bnpl-platform
```

### Database Connection Issues

```bash
# Test database connectivity from pod
kubectl exec -it <pod-name> -n bnpl-platform -- /bin/bash
curl -v telnet://sql-server:1433
```

### Ingress Not Working

```bash
# Check ingress status
kubectl describe ingress bnpl-ingress -n bnpl-platform

# Check ingress controller logs
kubectl logs -f -n ingress-nginx deployment/ingress-nginx-controller
```

### Performance Issues

```bash
# Check resource usage
kubectl top pods -n bnpl-platform
kubectl top nodes

# Check HPA status
kubectl get hpa -n bnpl-platform
```

---

## Rollback

### Kubernetes Deployment Rollback

```bash
# View deployment history
kubectl rollout history deployment/payment-api -n bnpl-platform

# Rollback to previous version
kubectl rollout undo deployment/payment-api -n bnpl-platform

# Rollback to specific revision
kubectl rollout undo deployment/payment-api --to-revision=2 -n bnpl-platform
```

---

## Security Best Practices

1. **Use Azure Key Vault** for sensitive secrets
2. **Enable Pod Security Policies**
3. **Use Network Policies** to restrict traffic
4. **Regular security scans** with Trivy/Snyk
5. **Rotate credentials** regularly
6. **Enable Azure AD authentication** for AKS
7. **Use managed identities** instead of service principals

---

## Performance Optimization

1. **Enable Redis caching** for frequent queries
2. **Use CDN** for static content
3. **Configure connection pooling** for databases
4. **Enable response compression**
5. **Use async/await** patterns consistently
6. **Implement request batching** where possible

---

## Next Steps

After deployment:

1. ✅ Verify all health checks are passing
2. ✅ Configure monitoring alerts
3. ✅ Setup backup schedules
4. ✅ Test disaster recovery procedures
5. ✅ Configure autoscaling rules
6. ✅ Implement log retention policies
7. ✅ Schedule regular security audits

---

**Need Help?** Check the [troubleshooting section](#troubleshooting) or create an issue on GitHub.