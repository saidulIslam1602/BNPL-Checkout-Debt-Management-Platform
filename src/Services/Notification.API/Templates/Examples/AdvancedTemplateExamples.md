# Advanced Template Examples

This document showcases the advanced template features available in the Notification API.

## 1. Simple Variables

```html
<h1>Hello {{customer.name}}!</h1>
<p>Your order #{{order.id}} for {{order.amount | currency}} has been confirmed.</p>
```

## 2. Conditionals

### Basic If Statement
```html
{% if customer.isPremium %}
<div class="premium-badge">Premium Customer</div>
{% endif %}

<p>Dear {{customer.name}},</p>
{% if order.amount > 1000 %}
<p>Thank you for your large order! You qualify for free shipping.</p>
{% else %}
<p>Your order total is {{order.amount | currency}}.</p>
{% endif %}
```

### Complex Conditions
```html
{% if customer.age >= 18 and customer.verified %}
<p>You are eligible for our premium services.</p>
{% endif %}

{% if order.status == 'shipped' or order.status == 'delivered' %}
<p>Your order is on its way!</p>
{% endif %}

{% if not customer.hasOptedOut %}
<p>You will receive updates about this order.</p>
{% endif %}
```

## 3. Loops

### Simple Loop
```html
<h3>Your Order Items:</h3>
<ul>
{% for item in order.items %}
<li>{{item.name}} - {{item.price | currency}} (Qty: {{item.quantity}})</li>
{% endfor %}
</ul>
```

### Loop with Index and Conditionals
```html
<h3>Payment Schedule:</h3>
{% for payment in installments %}
<div class="payment-item">
    <h4>Payment {{payment_index + 1}}{% if payment_first %} (First Payment){% endif %}</h4>
    <p>Amount: {{payment.amount | currency}}</p>
    <p>Due Date: {{payment.dueDate | date:'yyyy-MM-dd'}}</p>
    {% if payment.isPaid %}
    <span class="paid">✓ Paid</span>
    {% else %}
    <span class="pending">⏳ Pending</span>
    {% endif %}
</div>
{% endfor %}
```

## 4. Filters

### Text Filters
```html
<h1>{{title | upper}}</h1>
<p>{{description | truncate:100}}</p>
<p>{{customer.name | title}} - {{customer.email | lower}}</p>
```

### Date and Currency Filters
```html
<p>Order Date: {{order.createdAt | date:'MMMM dd, yyyy'}}</p>
<p>Total Amount: {{order.total | currency}}</p>
<p>Due Date: {{payment.dueDate | date:'yyyy-MM-dd'}}</p>
```

### Default Values
```html
<p>Phone: {{customer.phone | default:'Not provided'}}</p>
<p>Notes: {{order.notes | default:'No special instructions'}}</p>
```

### HTML Escaping
```html
<p>Customer Message: {{customer.message | escape}}</p>
<p>Description: {{product.description | nl2br}}</p>
```

## 5. Functions

### Built-in Functions
```html
<p>Generated on: {{now()}}</p>
<p>Report Date: {{today()}}</p>
<p>Total Items: {{count(order.items)}}</p>
<p>Random Order ID: #{{random()}}</p>
```

## 6. Complex Example: Payment Reminder

```html
<!DOCTYPE html>
<html>
<head>
    <title>Payment Reminder - {{customer.name | title}}</title>
</head>
<body>
    <div class="header">
        <h1>Payment Reminder</h1>
        {% if customer.isPremium %}
        <div class="premium-badge">Premium Customer</div>
        {% endif %}
    </div>
    
    <div class="content">
        <p>Dear {{customer.name | title}},</p>
        
        {% if overdueDays > 0 %}
        <div class="alert alert-danger">
            <p><strong>Your payment is {{overdueDays}} days overdue.</strong></p>
        </div>
        {% else %}
        <p>This is a friendly reminder that your payment is due soon.</p>
        {% endif %}
        
        <div class="payment-details">
            <h3>Payment Information</h3>
            <table>
                <tr>
                    <td>Amount Due:</td>
                    <td><strong>{{payment.amount | currency}}</strong></td>
                </tr>
                <tr>
                    <td>Due Date:</td>
                    <td>{{payment.dueDate | date:'MMMM dd, yyyy'}}</td>
                </tr>
                <tr>
                    <td>Order ID:</td>
                    <td>#{{order.id}}</td>
                </tr>
            </table>
        </div>
        
        {% if installments %}
        <div class="installment-schedule">
            <h3>Remaining Installments</h3>
            {% for installment in installments %}
            <div class="installment {% if installment.isOverdue %}overdue{% endif %}">
                <span class="amount">{{installment.amount | currency}}</span>
                <span class="date">Due: {{installment.dueDate | date:'MMM dd'}}</span>
                {% if installment.isOverdue %}
                <span class="status overdue">Overdue</span>
                {% else %}
                <span class="status pending">Pending</span>
                {% endif %}
            </div>
            {% endfor %}
        </div>
        {% endif %}
        
        <div class="actions">
            <a href="{{paymentLink}}" class="btn btn-primary">Pay Now</a>
            {% if customer.canRequestExtension %}
            <a href="{{extensionLink}}" class="btn btn-secondary">Request Extension</a>
            {% endif %}
        </div>
        
        <div class="footer">
            <p>If you have any questions, please contact us at {{supportEmail | default:'support@yourcompany.com'}}</p>
            <p><small>Generated on {{now()}} for {{customer.email}}</small></p>
        </div>
    </div>
</body>
</html>
```

## 7. SMS Template Example

```
{% if customer.firstName %}Hi {{customer.firstName}}{% else %}Hello{% endif %}! 

Your payment of {{payment.amount | currency}} is due on {{payment.dueDate | date:'MMM dd'}}. 

{% if overdueDays > 0 %}⚠️ OVERDUE by {{overdueDays}} days. {% endif %}Pay now: {{shortPaymentLink}}

{% if customer.canRequestExtension %}Need more time? Reply EXTEND{% endif %}

- YourCompany BNPL
```

## 8. Email Subject Examples

```
{% if overdueDays > 0 %}URGENT: Payment Overdue{% else %}Payment Reminder{% endif %} - {{payment.amount | currency}} Due {{payment.dueDate | date:'MMM dd'}}
```

```
{{customer.name | title}}, your {{order.type | title}} order #{{order.id}} {% if order.status == 'shipped' %}has shipped{% else %}is being processed{% endif %}
```

## 9. Push Notification Examples

```json
{
  "title": "{% if isUrgent %}URGENT: {% endif %}Payment Due",
  "body": "{{payment.amount | currency}} payment due {% if overdueDays > 0 %}{{overdueDays}} days ago{% else %}on {{payment.dueDate | date:'MMM dd'}}{% endif %}",
  "data": {
    "paymentId": "{{payment.id}}",
    "amount": "{{payment.amount}}",
    "dueDate": "{{payment.dueDate}}"
  }
}
```

## Template Variables Reference

### Customer Variables
- `customer.id` - Customer ID
- `customer.name` - Full name
- `customer.firstName` - First name
- `customer.lastName` - Last name
- `customer.email` - Email address
- `customer.phone` - Phone number
- `customer.isPremium` - Premium status (boolean)
- `customer.verified` - Verification status (boolean)
- `customer.age` - Customer age
- `customer.hasOptedOut` - Opt-out status (boolean)

### Order Variables
- `order.id` - Order ID
- `order.amount` - Order amount
- `order.total` - Total amount
- `order.status` - Order status
- `order.createdAt` - Creation date
- `order.items` - Array of order items
- `order.type` - Order type
- `order.notes` - Order notes

### Payment Variables
- `payment.id` - Payment ID
- `payment.amount` - Payment amount
- `payment.dueDate` - Due date
- `payment.isPaid` - Payment status (boolean)
- `payment.isOverdue` - Overdue status (boolean)

### System Variables
- `overdueDays` - Number of overdue days
- `paymentLink` - Payment URL
- `shortPaymentLink` - Shortened payment URL
- `extensionLink` - Extension request URL
- `supportEmail` - Support email address
- `isUrgent` - Urgency flag (boolean)

### Available Filters
- `upper` - Convert to uppercase
- `lower` - Convert to lowercase
- `title` - Convert to title case
- `truncate:length` - Truncate to specified length
- `default:'value'` - Use default if empty
- `currency` - Format as currency
- `date:'format'` - Format date
- `escape` - HTML escape
- `nl2br` - Convert newlines to <br> tags

### Available Functions
- `now()` - Current date and time
- `today()` - Current date
- `random()` - Random number
- `count(array)` - Count array items