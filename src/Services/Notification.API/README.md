# Riverty BNPL Notification API

A comprehensive notification service for the Riverty BNPL platform, supporting multi-channel notifications (Email, SMS, Push) with advanced features like templating, scheduling, and delivery tracking.

## Features

### Core Functionality
- **Multi-Channel Support**: Email (SendGrid), SMS (Twilio), Push (Firebase)
- **Template Management**: Dynamic templates with variable substitution
- **Scheduling & Queuing**: Background job processing with Hangfire
- **Delivery Tracking**: Comprehensive event tracking and analytics
- **User Preferences**: Customizable notification preferences per user
- **Retry Logic**: Automatic retry with exponential backoff
- **Rate Limiting**: Configurable rate limits per channel

### Norwegian Compliance
- **Consumer Protection**: Compliant with Norwegian notification regulations
- **Quiet Hours**: Respects user-defined quiet hours
- **Language Support**: Norwegian (nb-NO) templates by default
- **Time Zone Handling**: Proper handling of Norwegian time zones

## API Endpoints

### Notifications
- `POST /api/v1/notifications` - Send single notification
- `POST /api/v1/notifications/bulk` - Send bulk notifications
- `GET /api/v1/notifications` - List notifications with filtering
- `GET /api/v1/notifications/{id}` - Get notification by ID
- `PUT /api/v1/notifications/{id}/status` - Update notification status
- `POST /api/v1/notifications/{id}/events` - Record delivery event
- `POST /api/v1/notifications/{id}/cancel` - Cancel scheduled notification
- `POST /api/v1/notifications/{id}/retry` - Retry failed notification
- `GET /api/v1/notifications/stats` - Get notification statistics

### Templates
- `GET /api/v1/templates` - List all templates
- `GET /api/v1/templates/{name}` - Get template by name
- `POST /api/v1/templates` - Create new template
- `PUT /api/v1/templates/{name}` - Update template
- `DELETE /api/v1/templates/{name}` - Delete template
- `POST /api/v1/templates/{name}/render` - Test template rendering

### User Preferences
- `GET /api/v1/users/{userId}/preferences` - Get user preferences
- `PUT /api/v1/users/{userId}/preferences` - Update user preferences
- `GET /api/v1/users/{userId}/preferences/check` - Check if notification allowed
- `GET /api/v1/users/{userId}/preferences/optimal-time` - Get optimal send time

### Health & Monitoring
- `GET /api/v1/health` - Detailed health status
- `GET /api/v1/health/status` - System status
- `GET /api/v1/health/queue` - Queue statistics
- `GET /api/v1/health/ready` - Readiness probe
- `GET /api/v1/health/live` - Liveness probe

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=RivertyBNPL_Notification;..."

# JWT Authentication
Jwt__Key="your-jwt-secret-key"
Jwt__Issuer="https://api.riverty.com"
Jwt__Audience="https://api.riverty.com"

# SendGrid (Email)
NotificationProviders__Email__SendGrid__ApiKey="your-sendgrid-api-key"
NotificationProviders__Email__SendGrid__FromEmail="noreply@riverty.com"

# Twilio (SMS)
NotificationProviders__SMS__Twilio__AccountSid="your-twilio-account-sid"
NotificationProviders__SMS__Twilio__AuthToken="your-twilio-auth-token"
NotificationProviders__SMS__Twilio__FromNumber="+4712345678"

# Firebase (Push)
NotificationProviders__Push__Firebase__ProjectId="riverty-bnpl"
NotificationProviders__Push__Firebase__CredentialsPath="/app/secrets/firebase-credentials.json"

# Application Insights
ApplicationInsights__ConnectionString="your-application-insights-connection-string"
```

### Development Mode

In development, the service uses console providers that log notifications to the console instead of sending them through external services.

## Database Schema

The service uses the following main entities:

- **Notifications**: Core notification records
- **NotificationTemplates**: Template definitions with versioning
- **NotificationPreferences**: User notification preferences
- **NotificationQueue**: Background job queue
- **NotificationEvents**: Delivery event tracking
- **NotificationDeliveryAttempts**: Retry attempt tracking

## Background Jobs

The service uses Hangfire for background processing:

- **Queue Processing**: Processes scheduled notifications every 30 seconds
- **Retry Logic**: Automatic retry with exponential backoff for failed notifications
- **Cleanup Jobs**: Periodic cleanup of old records (can be configured)

## Security

- **JWT Authentication**: All endpoints require valid JWT tokens
- **Input Validation**: Comprehensive validation using FluentValidation
- **Rate Limiting**: Built-in rate limiting per notification channel
- **SQL Injection Protection**: Entity Framework Core with parameterized queries

## Monitoring & Health Checks

- **Health Checks**: Database connectivity, provider health, queue status
- **Metrics**: Success rates, queue depths, response times
- **Logging**: Structured logging with Serilog
- **Application Insights**: Integration for telemetry and monitoring

## Docker Support

```bash
# Build image
docker build -t riverty-notification-api .

# Run container
docker run -p 5003:8080 \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  -e NotificationProviders__Email__Provider="Console" \
  riverty-notification-api
```

## Usage Examples

### Send Email Notification

```json
POST /api/v1/notifications
{
  "recipientId": "user123",
  "recipientEmail": "user@example.com",
  "type": "PaymentReminder",
  "channel": "Email",
  "templateName": "payment_reminder",
  "templateData": {
    "CustomerName": "John Doe",
    "Amount": "1500.00",
    "DueDate": "2024-01-15"
  },
  "priority": "Normal"
}
```

### Send Bulk SMS

```json
POST /api/v1/notifications/bulk
{
  "recipients": [
    {
      "recipientId": "user123",
      "recipientEmail": "user1@example.com",
      "recipientPhone": "+4712345678",
      "templateData": { "CustomerName": "John Doe" }
    }
  ],
  "type": "PaymentReminder",
  "channel": "SMS",
  "templateName": "sms_payment_reminder",
  "priority": "High"
}
```

### Create Template

```json
POST /api/v1/templates
{
  "name": "payment_reminder",
  "displayName": "Payment Reminder",
  "type": "PaymentReminder",
  "channel": "Email",
  "subject": "Påminnelse: Betaling forfaller snart",
  "bodyTemplate": "Hei {{CustomerName}},\n\nDin betaling på {{Amount}} NOK forfaller {{DueDate}}.",
  "language": "nb-NO",
  "variables": ["CustomerName", "Amount", "DueDate"]
}
```

## Development

### Prerequisites
- .NET 8 SDK
- SQL Server (or SQL Server Express)
- Redis (optional, for caching)

### Running Locally

```bash
# Restore packages
dotnet restore

# Update database
dotnet ef database update

# Run the application
dotnet run
```

The API will be available at `https://localhost:5003` with Swagger UI at the root path.

### Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration
```

## Contributing

1. Follow the existing code style and patterns
2. Add unit tests for new functionality
3. Update documentation for API changes
4. Ensure all health checks pass
5. Test with both console and real providers

## License

Copyright © 2024 Riverty BNPL. All rights reserved.