# Risk Assessment API

A comprehensive risk assessment and fraud detection API for the YourCompany BNPL (Buy Now, Pay Later) platform. This service provides real-time credit risk assessment, fraud detection, and machine learning-powered risk scoring capabilities specifically designed for the Norwegian market.

## Features

### 🎯 Core Capabilities

- **Credit Risk Assessment**: Comprehensive credit evaluation using multiple data sources
- **Fraud Detection**: Real-time transaction fraud analysis with ML-powered scoring
- **Credit Bureau Integration**: Direct integration with Norwegian credit bureaus (Experian, Bisnode, Lindorff)
- **Machine Learning Models**: Advanced ML models for risk prediction and fraud detection
- **Risk Profiling**: Dynamic customer risk profile management
- **Real-time Analytics**: Risk and fraud analytics with detailed reporting

### 🔒 Security & Compliance

- **JWT Authentication**: Secure API access with role-based authorization
- **Rate Limiting**: Configurable rate limiting to prevent abuse
- **Circuit Breakers**: Fault tolerance for external service dependencies
- **Data Privacy**: GDPR-compliant data handling for Norwegian market
- **Audit Logging**: Comprehensive audit trails for all risk decisions

### 🚀 Performance & Reliability

- **High Availability**: Built for 99.9% uptime with health checks
- **Scalable Architecture**: Microservices design with horizontal scaling
- **Caching**: Intelligent caching for improved response times
- **Monitoring**: Comprehensive health checks and performance metrics
- **Background Processing**: Automated model retraining and rule updates

## API Endpoints

### Risk Assessment
- `POST /api/v1/riskassessment/assess` - Perform credit risk assessment
- `GET /api/v1/riskassessment/{id}` - Get assessment details
- `GET /api/v1/riskassessment/customer/{customerId}` - Get customer assessments
- `POST /api/v1/riskassessment/search` - Search assessments
- `GET /api/v1/riskassessment/profile/{customerId}` - Get risk profile
- `PUT /api/v1/riskassessment/profile/{customerId}/update` - Update risk profile
- `GET /api/v1/riskassessment/analytics` - Get risk analytics

### Fraud Detection
- `POST /api/v1/frauddetection/detect` - Detect fraud in real-time
- `GET /api/v1/frauddetection/{id}` - Get fraud detection details
- `POST /api/v1/frauddetection/search` - Search fraud detections
- `PUT /api/v1/frauddetection/rules/update` - Update fraud rules
- `GET /api/v1/frauddetection/customer/{customerId}/stats` - Get customer fraud stats
- `GET /api/v1/frauddetection/monitoring/dashboard` - Get monitoring dashboard

### Credit Bureau Integration
- `POST /api/v1/creditbureau/credit-report` - Get credit report
- `POST /api/v1/creditbureau/validate-ssn` - Validate Norwegian SSN
- `POST /api/v1/creditbureau/check-bankruptcy` - Check bankruptcy records
- `POST /api/v1/creditbureau/payment-history` - Get payment history
- `GET /api/v1/creditbureau/bureaus` - Get available bureaus
- `POST /api/v1/creditbureau/test-connectivity/{bureauName}` - Test connectivity

### Model Management
- `POST /api/v1/modelmanagement/predict/credit-risk` - Predict credit risk
- `POST /api/v1/modelmanagement/predict/fraud-risk` - Predict fraud risk
- `GET /api/v1/modelmanagement/performance/{modelName}` - Get model performance
- `POST /api/v1/modelmanagement/retrain` - Retrain models
- `GET /api/v1/modelmanagement/models` - Get available models
- `GET /api/v1/modelmanagement/training-status` - Get training status

### Health & Monitoring
- `GET /api/v1/health` - Basic health check
- `GET /api/v1/health/info` - System information
- `GET /api/v1/health/circuit-breakers` - Circuit breaker status
- `POST /api/v1/health/circuit-breakers/{name}/reset` - Reset circuit breaker
- `GET /api/v1/health/metrics` - Performance metrics

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code

### Configuration

1. **Database Connection**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourCompanyBNPL_Risk;Trusted_Connection=true"
     }
   }
   ```

2. **Credit Bureau Configuration**
   ```json
   {
     "CreditBureau": {
       "Bureaus": [
         {
           "Name": "Experian",
           "BaseUrl": "https://api.experian.no/v1",
           "ApiKey": "your-api-key"
         }
       ]
     }
   }
   ```

3. **JWT Configuration**
   ```json
   {
     "Jwt": {
       "Key": "YourSecretKey",
       "Issuer": "YourCompanyBNPL",
       "Audience": "YourCompanyBNPL.APIs"
     }
   }
   ```

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd "BNPL Checkout & Debt Management Platform"
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore src/Services/Risk.API/Risk.API.csproj
   ```

3. **Update database**
   ```bash
   dotnet ef database update --project src/Services/Risk.API
   ```

4. **Run the application**
   ```bash
   dotnet run --project src/Services/Risk.API
   ```

5. **Access Swagger UI**
   Navigate to `https://localhost:5001` to view the API documentation

### Docker Deployment

1. **Build Docker image**
   ```bash
   docker build -t yourcompany-risk-api -f src/Services/Risk.API/Dockerfile .
   ```

2. **Run container**
   ```bash
   docker run -p 8080:80 -e ASPNETCORE_ENVIRONMENT=Production yourcompany-risk-api
   ```

## Architecture

### Risk Assessment Flow
1. **Input Validation**: Comprehensive validation of assessment requests
2. **Credit Bureau Integration**: Fetch credit data from Norwegian bureaus
3. **Rule Engine**: Apply configurable risk rules
4. **ML Scoring**: Machine learning risk prediction
5. **Decision Engine**: Final risk assessment and approval decision
6. **Profile Update**: Update customer risk profile

### Fraud Detection Flow
1. **Real-time Analysis**: Immediate transaction analysis
2. **Rule Evaluation**: Apply fraud detection rules
3. **ML Prediction**: Machine learning fraud scoring
4. **Risk Scoring**: Combined rule and ML-based scoring
5. **Action Decision**: Block/allow transaction decision
6. **Monitoring**: Real-time fraud monitoring and alerting

### Machine Learning Models
- **Credit Risk Model**: FastTree binary classification for credit approval
- **Fraud Detection Model**: FastTree binary classification for fraud detection
- **Automated Retraining**: Weekly model retraining with new data
- **Performance Monitoring**: Continuous model performance tracking

## Norwegian Market Compliance

### Credit Bureau Integration
- **Experian Norway**: Full credit reports and scoring
- **Bisnode (Dun & Bradstreet)**: Business and consumer credit data
- **Lindorff (Intrum)**: Collection and payment history data

### Regulatory Compliance
- **GDPR**: Full compliance with data protection regulations
- **Norwegian Financial Services Act**: Compliance with lending regulations
- **PCI DSS**: Secure handling of payment card data
- **Norwegian SSN Validation**: Proper validation of Norwegian social security numbers

### Data Sources
- **Folkeregisteret**: Norwegian Population Register integration
- **Konkursregisteret**: Bankruptcy register checks
- **Credit History Services**: Payment history and credit behavior

## Monitoring & Observability

### Health Checks
- Database connectivity
- Credit bureau availability
- ML model health
- Circuit breaker status

### Metrics
- Request/response times
- Error rates
- Fraud detection rates
- Model performance metrics
- Resource utilization

### Logging
- Structured logging with Serilog
- Request/response logging
- Error and exception tracking
- Audit trail for risk decisions

## Security

### Authentication & Authorization
- JWT-based authentication
- Role-based access control
- API key management for external integrations

### Data Protection
- Encryption at rest and in transit
- PII data masking in logs
- Secure configuration management
- Regular security audits

### Rate Limiting
- Per-client rate limiting
- Endpoint-specific limits
- Configurable time windows
- Graceful degradation

## Development

### Code Structure
```
src/Services/Risk.API/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Models/              # Data models
├── DTOs/                # Data transfer objects
├── Data/                # Entity Framework context
├── Infrastructure/      # Cross-cutting concerns
├── Validators/          # Request validation
├── Mappings/           # AutoMapper profiles
└── Tests/              # Unit and integration tests
```

### Testing
- Unit tests with xUnit
- Integration tests for API endpoints
- Mock external dependencies
- Test coverage reporting

### Contributing
1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Support

For support and questions, please contact the YourCompany Development Team at dev@yourcompany.com.