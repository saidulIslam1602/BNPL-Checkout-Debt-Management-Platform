using YourCompanyBNPL.Events.Base;
using YourCompanyBNPL.Common.Enums;
using System.Text.Json.Serialization;

namespace YourCompanyBNPL.Events.Payment;

/// <summary>
/// Event raised when a payment is initiated
/// </summary>
public class PaymentInitiatedEvent : IntegrationEvent
{
    [JsonPropertyName("paymentId")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("merchantId")]
    public Guid MerchantId { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }
    
    [JsonPropertyName("bnplPlanType")]
    public BNPLPlanType? BNPLPlanType { get; set; }
    
    [JsonPropertyName("installmentCount")]
    public int? InstallmentCount { get; set; }
    
    [JsonPropertyName("orderReference")]
    public string OrderReference { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Event raised when a payment is completed successfully
/// </summary>
public class PaymentCompletedEvent : IntegrationEvent
{
    [JsonPropertyName("paymentId")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("merchantId")]
    public Guid MerchantId { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }
    
    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; }
    
    [JsonPropertyName("fees")]
    public decimal Fees { get; set; }
    
    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }
}

/// <summary>
/// Event raised when a payment fails
/// </summary>
public class PaymentFailedEvent : IntegrationEvent
{
    [JsonPropertyName("paymentId")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("merchantId")]
    public Guid MerchantId { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("failureReason")]
    public string FailureReason { get; set; } = string.Empty;
    
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
    
    [JsonPropertyName("isRetryable")]
    public bool IsRetryable { get; set; }
    
    [JsonPropertyName("nextRetryAt")]
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Event raised when a BNPL payment plan is created
/// </summary>
public class BNPLPlanCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("planId")]
    public Guid PlanId { get; set; }
    
    [JsonPropertyName("paymentId")]
    public Guid PaymentId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("merchantId")]
    public Guid MerchantId { get; set; }
    
    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("planType")]
    public BNPLPlanType PlanType { get; set; }
    
    [JsonPropertyName("installmentCount")]
    public int InstallmentCount { get; set; }
    
    [JsonPropertyName("installmentAmount")]
    public decimal InstallmentAmount { get; set; }
    
    [JsonPropertyName("firstPaymentDate")]
    public DateTime FirstPaymentDate { get; set; }
    
    [JsonPropertyName("installments")]
    public List<InstallmentInfo> Installments { get; set; } = new();
    
    [JsonPropertyName("interestRate")]
    public decimal InterestRate { get; set; }
    
    [JsonPropertyName("totalInterest")]
    public decimal TotalInterest { get; set; }
}

/// <summary>
/// Event raised when an installment payment is due
/// </summary>
public class InstallmentDueEvent : IntegrationEvent
{
    [JsonPropertyName("installmentId")]
    public Guid InstallmentId { get; set; }
    
    [JsonPropertyName("planId")]
    public Guid PlanId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }
    
    [JsonPropertyName("installmentNumber")]
    public int InstallmentNumber { get; set; }
    
    [JsonPropertyName("totalInstallments")]
    public int TotalInstallments { get; set; }
    
    [JsonPropertyName("isOverdue")]
    public bool IsOverdue { get; set; }
    
    [JsonPropertyName("daysPastDue")]
    public int DaysPastDue { get; set; }
}

/// <summary>
/// Event raised when an installment payment is processed
/// </summary>
public class InstallmentPaidEvent : IntegrationEvent
{
    [JsonPropertyName("installmentId")]
    public Guid InstallmentId { get; set; }
    
    [JsonPropertyName("planId")]
    public Guid PlanId { get; set; }
    
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }
    
    [JsonPropertyName("paidAt")]
    public DateTime PaidAt { get; set; }
    
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;
    
    [JsonPropertyName("installmentNumber")]
    public int InstallmentNumber { get; set; }
    
    [JsonPropertyName("remainingInstallments")]
    public int RemainingInstallments { get; set; }
    
    [JsonPropertyName("remainingBalance")]
    public decimal RemainingBalance { get; set; }
}

/// <summary>
/// Information about an installment in a BNPL plan
/// </summary>
public class InstallmentInfo
{
    [JsonPropertyName("installmentNumber")]
    public int InstallmentNumber { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }
    
    [JsonPropertyName("principalAmount")]
    public decimal PrincipalAmount { get; set; }
    
    [JsonPropertyName("interestAmount")]
    public decimal InterestAmount { get; set; }
    
    [JsonPropertyName("feeAmount")]
    public decimal FeeAmount { get; set; }
}