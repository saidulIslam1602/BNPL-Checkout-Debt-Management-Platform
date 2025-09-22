namespace RivertyBNPL.Common.Enums;

/// <summary>
/// Payment status enumeration for tracking payment lifecycle
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6,
    Disputed = 7,
    Expired = 8
}

/// <summary>
/// BNPL payment plan types
/// </summary>
public enum BNPLPlanType
{
    PayIn3 = 0,      // 3 installments
    PayIn4 = 1,      // 4 installments
    PayIn6 = 2,      // 6 installments
    PayIn12 = 3,     // 12 installments
    PayIn24 = 4,     // 24 installments
    Custom = 5       // Custom installment plan
}

/// <summary>
/// Payment method types supported by the platform
/// </summary>
public enum PaymentMethod
{
    CreditCard = 0,
    DebitCard = 1,
    BankTransfer = 2,
    DirectDebit = 3,
    DigitalWallet = 4,
    BNPL = 5,
    Cash = 6,
    Cryptocurrency = 7
}

/// <summary>
/// Transaction types for financial operations
/// </summary>
public enum TransactionType
{
    Payment = 0,
    Refund = 1,
    Chargeback = 2,
    Settlement = 3,
    Fee = 4,
    Interest = 5,
    Penalty = 6,
    Adjustment = 7
}

/// <summary>
/// Currency codes (ISO 4217)
/// </summary>
public enum Currency
{
    NOK = 578,  // Norwegian Krone
    EUR = 978,  // Euro
    USD = 840,  // US Dollar
    GBP = 826,  // British Pound
    SEK = 752,  // Swedish Krona
    DKK = 208   // Danish Krone
}

/// <summary>
/// Risk assessment levels
/// </summary>
public enum RiskLevel
{
    VeryLow = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4,
    Blocked = 5
}

/// <summary>
/// Customer credit rating
/// </summary>
public enum CreditRating
{
    Excellent = 0,  // 750+
    Good = 1,       // 700-749
    Fair = 2,       // 650-699
    Poor = 3,       // 600-649
    VeryPoor = 4,   // Below 600
    NoHistory = 5   // No credit history
}

/// <summary>
/// Settlement status for merchant payouts
/// </summary>
public enum SettlementStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    OnHold = 5,
    Disputed = 6
}

/// <summary>
/// Notification types for customer communications
/// </summary>
public enum NotificationType
{
    PaymentReminder = 0,
    PaymentConfirmation = 1,
    PaymentFailed = 2,
    PaymentOverdue = 3,
    AccountSuspended = 4,
    SettlementNotification = 5,
    RiskAlert = 6,
    SystemMaintenance = 7,
    PromotionalOffer = 8
}

/// <summary>
/// Notification channels
/// </summary>
public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Push = 2,
    InApp = 3,
    Phone = 4,
    Mail = 5
}

/// <summary>
/// Debt collection status
/// </summary>
public enum CollectionStatus
{
    Current = 0,        // No overdue payments
    EarlyDelinquency = 1,   // 1-30 days overdue
    Delinquent = 2,     // 31-60 days overdue
    LateDelinquent = 3, // 61-90 days overdue
    ChargeOff = 4,      // 90+ days overdue
    InCollection = 5,   // Sent to collection agency
    Settled = 6,        // Debt settled for less than full amount
    PaidInFull = 7,     // Debt paid in full
    WriteOff = 8        // Debt written off as uncollectable
}