# 📖 API Documentation - BNPL Platform

Complete API documentation for all microservices in the BNPL Platform.

## 📑 Table of Contents

- [Authentication](#authentication)
- [Payment API](#payment-api)
- [Risk Assessment API](#risk-assessment-api)
- [Settlement API](#settlement-api)
- [Notification API](#notification-api)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)
- [Webhooks](#webhooks)

---

## Authentication

All API endpoints (except health checks) require JWT authentication.

### Get JWT Token

**Endpoint:** `POST /api/auth/token`

**Request Body:**
```json
{
  "username": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "refresh_token_here"
}
```

### Using the Token

Include the token in the Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Payment API

Base URL: `https://api.yourcompany.com/payment`

### Create Payment

**Endpoint:** `POST /api/payments`

**Request:**
```json
{
  "amount": 1000.00,
  "currency": "NOK",
  "customerId": "customer-uuid",
  "merchantId": "merchant-uuid",
  "paymentMethod": "CreditCard",
  "description": "Order #12345",
  "metadata": {
    "orderId": "12345",
    "productName": "Product Name"
  }
}
```

**Response:** `201 Created`
```json
{
  "id": "payment-uuid",
  "status": "Pending",
  "amount": 1000.00,
  "currency": "NOK",
  "createdAt": "2024-01-01T10:00:00Z",
  "paymentUrl": "https://payment-gateway.com/pay/xyz"
}
```

### Get Payment

**Endpoint:** `GET /api/payments/{id}`

**Response:** `200 OK`
```json
{
  "id": "payment-uuid",
  "status": "Completed",
  "amount": 1000.00,
  "currency": "NOK",
  "customerId": "customer-uuid",
  "merchantId": "merchant-uuid",
  "paymentMethod": "CreditCard",
  "createdAt": "2024-01-01T10:00:00Z",
  "completedAt": "2024-01-01T10:05:00Z"
}
```

### Create BNPL Application

**Endpoint:** `POST /api/bnpl/applications`

**Request:**
```json
{
  "customerId": "customer-uuid",
  "merchantId": "merchant-uuid",
  "amount": 5000.00,
  "currency": "NOK",
  "installmentPlan": "PayIn12",
  "customerInfo": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+4712345678",
    "dateOfBirth": "1990-01-01",
    "nationalId": "01019012345"
  },
  "items": [
    {
      "name": "Product 1",
      "quantity": 1,
      "unitPrice": 5000.00
    }
  ]
}
```

**Response:** `201 Created`
```json
{
  "applicationId": "application-uuid",
  "status": "UnderReview",
  "amount": 5000.00,
  "installmentPlan": "PayIn12",
  "monthlyPayment": 416.67,
  "interestRate": 0.12,
  "submittedAt": "2024-01-01T10:00:00Z"
}
```

### Get BNPL Application Status

**Endpoint:** `GET /api/bnpl/applications/{id}`

**Response:** `200 OK`
```json
{
  "applicationId": "application-uuid",
  "status": "Approved",
  "amount": 5000.00,
  "approvedAmount": 5000.00,
  "installmentPlan": "PayIn12",
  "monthlyPayment": 416.67,
  "interestRate": 0.12,
  "firstPaymentDate": "2024-02-01",
  "approvedAt": "2024-01-01T10:15:00Z"
}
```

---

## Risk Assessment API

Base URL: `https://api.yourcompany.com/risk`

### Assess Customer Risk

**Endpoint:** `POST /api/risk/assess`

**Request:**
```json
{
  "customerId": "customer-uuid",
  "amount": 5000.00,
  "personalInfo": {
    "nationalId": "01019012345",
    "dateOfBirth": "1990-01-01",
    "income": 500000,
    "employmentStatus": "Employed"
  },
  "requestedCredit": 5000.00
}
```

**Response:** `200 OK`
```json
{
  "riskScore": 750,
  "riskLevel": "Low",
  "creditLimit": 10000.00,
  "recommendedAmount": 5000.00,
  "decision": "Approved",
  "factors": [
    {
      "factor": "CreditHistory",
      "impact": "Positive",
      "weight": 0.3
    },
    {
      "factor": "Income",
      "impact": "Positive",
      "weight": 0.25
    }
  ],
  "assessedAt": "2024-01-01T10:00:00Z"
}
```

### Fraud Detection

**Endpoint:** `POST /api/risk/fraud/check`

**Request:**
```json
{
  "transactionId": "transaction-uuid",
  "customerId": "customer-uuid",
  "amount": 1000.00,
  "ipAddress": "192.168.1.1",
  "deviceFingerprint": "device-fingerprint-hash",
  "location": {
    "country": "Norway",
    "city": "Oslo"
  }
}
```

**Response:** `200 OK`
```json
{
  "fraudScore": 0.15,
  "riskLevel": "Low",
  "decision": "Allow",
  "flags": [],
  "recommendedAction": "Proceed",
  "checkedAt": "2024-01-01T10:00:00Z"
}
```

---

## Settlement API

Base URL: `https://api.yourcompany.com/settlement`

### Create Settlement

**Endpoint:** `POST /api/settlements`

**Request:**
```json
{
  "merchantId": "merchant-uuid",
  "amount": 10000.00,
  "currency": "NOK",
  "description": "Daily settlement",
  "executionDate": "2024-01-02"
}
```

**Response:** `201 Created`
```json
{
  "id": "settlement-uuid",
  "merchantId": "merchant-uuid",
  "amount": 10000.00,
  "currency": "NOK",
  "status": "Pending",
  "reference": "SETT-20240101-ABC12345",
  "createdAt": "2024-01-01T10:00:00Z"
}
```

### Get Settlement Status

**Endpoint:** `GET /api/settlements/{id}`

**Response:** `200 OK`
```json
{
  "id": "settlement-uuid",
  "merchantId": "merchant-uuid",
  "amount": 10000.00,
  "currency": "NOK",
  "status": "Completed",
  "reference": "SETT-20240101-ABC12345",
  "bankTransferId": "bank-transfer-123",
  "createdAt": "2024-01-01T10:00:00Z",
  "processedAt": "2024-01-01T14:00:00Z",
  "completedAt": "2024-01-01T15:00:00Z"
}
```

### Register Merchant Account

**Endpoint:** `POST /api/merchantaccounts`

**Request:**
```json
{
  "merchantId": "merchant-uuid",
  "merchantName": "Merchant Name AS",
  "bankAccountNumber": "12345678901",
  "bankName": "DNB",
  "currency": "NOK"
}
```

**Response:** `201 Created`
```json
{
  "id": "account-uuid",
  "merchantId": "merchant-uuid",
  "merchantName": "Merchant Name AS",
  "bankAccountNumber": "12345678901",
  "bankName": "DNB",
  "currency": "NOK",
  "isActive": true,
  "isVerified": false,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

---

## Notification API

Base URL: `https://api.yourcompany.com/notification`

### Send Notification

**Endpoint:** `POST /api/notifications`

**Request:**
```json
{
  "recipientId": "customer-uuid",
  "channel": "Email",
  "templateId": "payment-confirmation",
  "data": {
    "customerName": "John Doe",
    "amount": "1000.00",
    "currency": "NOK",
    "paymentDate": "2024-01-01"
  },
  "priority": "Normal"
}
```

**Response:** `201 Created`
```json
{
  "id": "notification-uuid",
  "status": "Queued",
  "channel": "Email",
  "recipientId": "customer-uuid",
  "createdAt": "2024-01-01T10:00:00Z",
  "estimatedDelivery": "2024-01-01T10:02:00Z"
}
```

### Get Notification Status

**Endpoint:** `GET /api/notifications/{id}`

**Response:** `200 OK`
```json
{
  "id": "notification-uuid",
  "status": "Delivered",
  "channel": "Email",
  "recipientId": "customer-uuid",
  "createdAt": "2024-01-01T10:00:00Z",
  "sentAt": "2024-01-01T10:01:30Z",
  "deliveredAt": "2024-01-01T10:01:35Z"
}
```

---

## Error Handling

All APIs use standard HTTP status codes and return consistent error responses.

### Error Response Format

```json
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "The request body is invalid",
    "details": [
      {
        "field": "amount",
        "message": "Amount must be greater than 0"
      }
    ],
    "timestamp": "2024-01-01T10:00:00Z",
    "traceId": "trace-uuid"
  }
}
```

### HTTP Status Codes

| Code | Meaning | When to Use |
|------|---------|-------------|
| 200 | OK | Successful GET/PUT/DELETE |
| 201 | Created | Successful POST |
| 400 | Bad Request | Invalid request data |
| 401 | Unauthorized | Missing/invalid token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Duplicate/conflicting request |
| 422 | Unprocessable Entity | Validation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily down |

---

## Rate Limiting

### Limits

- **Default**: 100 requests per minute
- **BNPL Applications**: 50 requests per minute
- **Risk Assessment**: 50 requests per minute
- **Settlements**: 20 requests per minute

### Rate Limit Headers

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

### Rate Limit Exceeded Response

```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests. Please try again later.",
    "retryAfter": 60
  }
}
```

---

## Webhooks

The platform can send webhooks for important events.

### Configure Webhook

**Endpoint:** `POST /api/webhooks`

**Request:**
```json
{
  "url": "https://yourapp.com/webhooks/bnpl",
  "events": [
    "payment.completed",
    "payment.failed",
    "bnpl.approved",
    "bnpl.declined",
    "settlement.completed"
  ],
  "secret": "your-webhook-secret"
}
```

### Webhook Payload

```json
{
  "event": "payment.completed",
  "timestamp": "2024-01-01T10:00:00Z",
  "id": "event-uuid",
  "data": {
    "paymentId": "payment-uuid",
    "amount": 1000.00,
    "currency": "NOK",
    "status": "Completed"
  }
}
```

### Webhook Signature

Verify webhooks using HMAC-SHA256:

```http
X-Webhook-Signature: sha256=5f8d5e...
```

**Verification:**
```csharp
var signature = Request.Headers["X-Webhook-Signature"];
var payload = await Request.Body.ReadAsStringAsync();
var expectedSignature = ComputeHmacSha256(payload, webhookSecret);
if (signature != expectedSignature) {
    // Invalid signature
}
```

---

## Postman Collection

Import our Postman collection for easy API testing:

[Download Postman Collection](../postman/BNPL-Platform.postman_collection.json)

---

## OpenAPI/Swagger

Interactive API documentation available at:

- Payment API: `https://api.yourcompany.com/payment/swagger`
- Risk API: `https://api.yourcompany.com/risk/swagger`
- Settlement API: `https://api.yourcompany.com/settlement/swagger`
- Notification API: `https://api.yourcompany.com/notification/swagger`

---

## SDKs and Client Libraries

Coming soon:
- C# SDK
- JavaScript/TypeScript SDK
- Python SDK
- PHP SDK

---

## Support

For API support:
- Email: api-support@yourcompany.com
- Documentation: https://docs.yourcompany.com
- Status Page: https://status.yourcompany.com